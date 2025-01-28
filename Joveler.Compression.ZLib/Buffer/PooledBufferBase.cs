﻿/*   
    Written by Hajin Jang
    Copyright (C) 2024-present Hajin Jang

    zlib license

    This software is provided 'as-is', without any express or implied
    warranty.  In no event will the authors be held liable for any damages
    arising from the use of this software.

    Permission is granted to anyone to use this software for any purpose,
    including commercial applications, and to alter it and redistribute it
    freely, subject to the following restrictions:

    1. The origin of this software must not be misrepresented; you must not
       claim that you wrote the original software. If you use this software
       in a product, an acknowledgment in the product documentation would be
       appreciated but is not required.
    2. Altered source versions must be plainly marked as such, and must not be
       misrepresented as being the original software.
    3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Buffers;
using System.Diagnostics;

namespace Joveler.Compression.ZLib.Buffer
{
    internal abstract class PooledBufferBase : IDisposable
    {
        protected readonly ArrayPool<byte> _pool;
        protected readonly object _bufLock = new object();

        protected byte[] _buf;
        protected int _capacity = 0;
        /// <summary>
        /// Index of first valid data position of the buffer.
        /// Have a range of [0.._dataEndIdx].
        /// </summary>
        protected int _dataStartIdx = 0;
        /// <summary>
        /// Index of last valid data, and first writable position of the buffer.
        /// Have a range of [_dataStartIdx.._size].
        /// If _dataEndIdx == _size, the buffer is full.
        /// </summary>
        protected int _dataEndIdx = 0;


        protected bool _disposed = false;
        public bool Disposed => _disposed;

        public byte[] Buf => _buf;
        public int Capacity => _capacity;

        /// <summary>
        /// Index of first valid data position of the buffer.
        /// Have a range of [0..DataEndIdx].
        /// </summary>
        /// <remarks>
        /// DataStartIdx is settable for scenario of manipulating the buffer with a pointer.
        /// </remarks>
        public int DataStartIdx
        {
            get => _dataStartIdx;
            set
            {
                lock (_bufLock)
                {
                    if (value < 0 || _capacity < value || _dataEndIdx < value)
                        throw new ArgumentOutOfRangeException(nameof(DataStartIdx));
                    _dataStartIdx = value;
                }
            }
        }
        /// <summary>
        /// Index of last valid data, and first writable position of the buffer.
        /// Have a range of [DataStartIdx..Size].
        /// </summary>
        /// <remarks>
        /// DataEndIdx is settable for scenario of manipulating the buffer with a pointer.
        /// </remarks>
        public int DataEndIdx
        {
            get => _dataEndIdx;
            set
            {
                lock (_bufLock)
                {
                    if (value < 0 || _capacity < value || value < _dataStartIdx)
                        throw new ArgumentOutOfRangeException(nameof(DataEndIdx));
                    _dataEndIdx = value;
                }
            }
        }
        public int ReadableSize => _dataEndIdx - _dataStartIdx;
        public int WritableSize => _capacity - _dataEndIdx;

        // Buf/Span/Memory properties are not thread-safe, use with caution!
        public Span<byte> Span => _buf.AsSpan(0, _capacity);
        public ReadOnlySpan<byte> ReadablePortionSpan => _buf.AsSpan(_dataStartIdx, _dataEndIdx - _dataStartIdx);
        public Span<byte> WritablePortionSpan => _buf.AsSpan(_dataEndIdx, _capacity - _dataEndIdx);

        public Memory<byte> Memory => _buf.AsMemory(0, _capacity);
        public ReadOnlyMemory<byte> ReadablePortionMemory => _buf.AsMemory(_dataStartIdx, _dataEndIdx - _dataStartIdx);
        public Memory<byte> WritablePortionMemory => _buf.AsMemory(_dataEndIdx, _capacity - _dataEndIdx);

        public bool IsEmpty => _dataEndIdx == _dataStartIdx;
        public bool IsFull => _capacity == _dataEndIdx;

        public PooledBufferBase(ArrayPool<byte> pool, int size)
        {
            _pool = pool;
            _buf = _pool.Rent(size);
            _dataStartIdx = 0;
            _dataEndIdx = 0;
            _capacity = size;
        }

        /// <summary>
        /// Create an empty buffer.
        /// </summary>
        /// <param name="pool"></param>
        public PooledBufferBase(ArrayPool<byte> pool)
        {
            _pool = pool;
            _buf = Array.Empty<byte>();
            _dataStartIdx = 0;
            _dataEndIdx = 0;
            _capacity = 0;
        }

        ~PooledBufferBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            lock (_bufLock)
            {
                if (!_disposed)
                {
                    if (disposing)
                    { // Dispose managed state.

                    }

                    // Dispose unmanaged resources, and set large fields to null.
                    if (0 < _capacity)
                    {
                        // Return the buffer to the pool
                        _pool.Return(_buf);

                        // Reset to the empty buffer
                        _dataStartIdx = 0;
                        _dataEndIdx = 0;
                        _capacity = 0;
                        _buf = Array.Empty<byte>();
                    }

                    _disposed = true;
                }
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        /// <summary>
        /// Not thread safe.
        /// Use Buf directly when the buffer is shared among multiple threads.
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public int Read(Span<byte> span)
        {
            if (span.Length == 0)
                return 0;

            lock (_bufLock)
            {
                ReadOnlySpan<byte> readSpan = _buf.AsSpan(_dataStartIdx, _dataEndIdx);
                int readLength = Math.Min(span.Length, readSpan.Length);

                readSpan.Slice(0, readLength).CopyTo(span);
                _dataStartIdx += readLength;

                Debug.Assert(_dataStartIdx <= _dataEndIdx);
                return readLength;
            }    
        }

        public int Write(byte[] buffer, int offset, int count)
        {
            return Write(buffer.AsSpan(offset, count), false);
        }

        public int Write(byte[] buffer, int offset, int count, bool autoExpand)
        {
            return Write(buffer.AsSpan(offset, count), autoExpand);
        }

        public int Write(ReadOnlySpan<byte> span)
        {
            return Write(span, false);
        }

        public int Write(ReadOnlySpan<byte> span, bool autoExpand)
        {
            if (span.Length == 0)
                return 0;

            ReadOnlySpan<byte> inputSpan = span;
            lock (_bufLock)
            {
                if (_capacity <= _dataEndIdx + span.Length)
                {
                    if (autoExpand)
                    { // Expand buffer
                        if (!Expand(_dataEndIdx + span.Length))
                            throw new InvalidOperationException("Failed to expand buffer.");
                    }
                    else
                    { // Write as much as possible
                        inputSpan = span.Slice(0, _capacity - _dataEndIdx);
                    }
                }

                // Copy inputSpan to a writable portion of the buffer
                inputSpan.CopyTo(_buf.AsSpan(_dataEndIdx, _capacity - _dataEndIdx));

                _dataEndIdx += inputSpan.Length;

                Debug.Assert(_dataStartIdx <= _dataEndIdx);
                Debug.Assert(_dataEndIdx <= _capacity);
                return inputSpan.Length;
            }
        }

        /// <summary>
        /// Reset the position of the buffer to zero.
        /// </summary>
        /// <remarks>
        /// The data of the buffer itself is not cleared.
        /// </remarks>
        public void Clear()
        {
            lock (_bufLock)
            {
                _dataStartIdx = 0;
                _dataEndIdx = 0;
            }
        }

        public bool Expand(int newSize)
        {
            lock (_bufLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(PooledBuffer));
                if (newSize < _capacity)
                    return false;
                if (newSize == _capacity)
                    return true;

                byte[] oldBuffer = _buf;
                byte[] newBuffer = _pool.Rent(newSize);

                if (0 < _dataEndIdx - _dataStartIdx)
                    System.Buffer.BlockCopy(oldBuffer, _dataStartIdx, newBuffer, _dataStartIdx, _dataEndIdx - _dataStartIdx);

                if (0 < _capacity) // Buffer of zero size does not belong to the pool.
                    _pool.Return(oldBuffer);

                _buf = newBuffer;
                _capacity = newSize;

                return true;
            }
        }

        public bool TrimStart(int len)
        {
            lock (_bufLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(PooledBuffer));
                if (len == 0)
                    return true;
                if (len < 0 || _dataEndIdx < _dataStartIdx + len)
                    return false;

                byte[] oldBuffer = _buf;
                byte[] newBuffer = _pool.Rent(_capacity);

                System.Buffer.BlockCopy(oldBuffer, _dataStartIdx + len, newBuffer, 0, _dataEndIdx - _dataStartIdx - len);

                if (0 < _capacity) // Buffer of zero size does not belong to the pool.
                    _pool.Return(oldBuffer);

                _buf = newBuffer;
                _dataEndIdx -= len + _dataStartIdx;
                _dataStartIdx = 0;
                Debug.Assert(0 <= _dataEndIdx && _dataEndIdx <= _capacity);

                return true;
            }
        }
    }
}
