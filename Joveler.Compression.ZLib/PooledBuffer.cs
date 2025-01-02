#nullable enable

/*   
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

namespace Joveler.Compression.ZLib
{
    internal sealed class PooledBuffer : IDisposable
    {
        private readonly ArrayPool<byte> _pool;
        private byte[] _buf;
        // First writable index of the buffer. Have a range of [0..Size]. 
        // If _pos == _size, the buffer is full.
        private int _pos = 0;
        private int _size = 0;

        private bool _disposed = false;
        public bool Disposed => _disposed;

        public byte[] Buf => _buf;
        /// <summary>
        /// Position of the buffer.
        /// Have a range of [0..Size].
        /// </summary>
        /// <remarks>
        /// Pos is settable, for scenario of manipulating the buffer with a pointer.
        /// </remarks>
        public int Pos 
        { 
            get => _pos;
            set
            {
                if (value < 0 || _size < value)
                    throw new ArgumentOutOfRangeException(nameof(Pos));
                _pos = value;
            }
        }
        public int Size => _size;
        
        public Span<byte> Span => _buf.AsSpan(0, _size);
        public ReadOnlySpan<byte> ReadablePortionSpan => _buf.AsSpan(0, _pos);
        public Span<byte> WritablePortionSpan => _buf.AsSpan(_pos, _size - _pos);

        public bool IsEmpty => _pos == 0;
        public bool IsFull => _size == _pos;

        public PooledBuffer(ArrayPool<byte> pool, int size)
        {
            _pool = pool;
            _buf = _pool.Rent(size);
            _pos = 0;
            _size = size;
        }

        /// <summary>
        /// Create an empty buffer.
        /// </summary>
        /// <param name="pool"></param>
        public PooledBuffer(ArrayPool<byte> pool)
        {
            _pool = pool;
            _buf = Array.Empty<byte>();
            _pos = 0;
            _size = 0;
        }

        ~PooledBuffer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                { // Dispose managed state.

                }

                // Dispose unmanaged resources, and set large fields to null.
                if (0 < _size)
                {
                    // Return the buffer to the pool
                    _pool.Return(_buf);

                    // Reset to the empty buffer
                    _pos = 0;
                    _size = 0;
                    _buf = Array.Empty<byte>();
                }

                _disposed = true;
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public int Read(Span<byte> span)
        {
            if (span.Length == 0)
                return 0;

            int readLength = Math.Min(span.Length, _pos);

            ReadablePortionSpan.Slice(0, readLength).CopyTo(span);
            _pos -= readLength;

            if (0 < _pos)
                Buffer.BlockCopy(_buf, readLength, _buf, 0, _pos);

            return readLength;
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
            if (_size <= _pos + span.Length)
            {
                if (autoExpand)
                { // Expand buffer
                    if (!Expand(_pos + span.Length))
                        throw new InvalidOperationException("Failed to expand buffer.");
                }
                else
                { // Write as much as possible
                    inputSpan = span.Slice(0, _size - _pos);
                }
            }

            // Copy inputSpan to a writable portion of the buffer
            inputSpan.CopyTo(WritablePortionSpan);

            _pos += inputSpan.Length;

            Debug.Assert(_pos <= _size);
            return inputSpan.Length;
        }

        public void Reset()
        {
            _pos = 0;
        }

        public bool Expand(int newSize)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PooledBuffer));
            if (newSize < _size)
                return false;
            if (newSize == _size)
                return true;

            byte[] oldBuffer = _buf;
            byte[] newBuffer = _pool.Rent(newSize);

            if (0 < _pos)
                Buffer.BlockCopy(oldBuffer, 0, newBuffer, 0, _pos);

            if (0 < _size) // Buffer of zero size does not belong to the pool.
                _pool.Return(oldBuffer);

            _buf = newBuffer;
            _size = newSize;

            return true;
        }

        public bool TrimStart(int len)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PooledBuffer));
            if (len == 0)
                return true;
            if (len < 0 || _pos < len)
                return false;

            byte[] oldBuffer = _buf;
            byte[] newBuffer = _pool.Rent(_size);

            Buffer.BlockCopy(oldBuffer, len, newBuffer, 0, _pos - len);

            if (0 < _size) // Buffer of zero size does not belong to the pool.
                _pool.Return(oldBuffer);

            _buf = newBuffer;
            _pos -= len;
            Debug.Assert(0 <= _pos && _pos <= _size);

            return true;
        }

        public override string ToString()
        {
            return $"BUF [{_pos,7}/{_size,7}] (real: {_buf.Length})";
        }
    }
}
