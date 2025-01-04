using System;
using System.Buffers;
using System.Diagnostics;

namespace Joveler.Compression.ZLib.Buffer
{
    internal abstract class PooledBufferBase : IDisposable
    {
        protected readonly ArrayPool<byte> _pool;
        protected byte[] _buf;
        protected int _size = 0;
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
        public int Size => _size;

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
                if (value < 0 || _size < value || _dataEndIdx < value)
                    throw new ArgumentOutOfRangeException(nameof(DataStartIdx));
                _dataStartIdx = value;
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
                if (value < 0 || _size < value || value < _dataStartIdx)
                    throw new ArgumentOutOfRangeException(nameof(DataEndIdx));
                _dataEndIdx = value;
            }
        }
        public int ReadableSize => _dataEndIdx - _dataStartIdx;
        public int WritableSize => _size - _dataEndIdx;

        public Span<byte> Span => _buf.AsSpan(0, _size);
        public ReadOnlySpan<byte> ReadablePortionSpan => _buf.AsSpan(_dataStartIdx, _dataEndIdx);
        public Span<byte> WritablePortionSpan => _buf.AsSpan(_dataEndIdx, WritableSize);

        public Memory<byte> Memory => _buf.AsMemory(0, _size);
        public ReadOnlyMemory<byte> ReadablePortionMemory => _buf.AsMemory(_dataStartIdx, _dataEndIdx);
        public Memory<byte> WritablePortionMemory => _buf.AsMemory(_dataEndIdx, WritableSize);

        public bool IsEmpty => _dataEndIdx == _dataStartIdx;
        public bool IsFull => _size == _dataEndIdx;

        public PooledBufferBase(ArrayPool<byte> pool, int size)
        {
            _pool = pool;
            _buf = _pool.Rent(size);
            _dataStartIdx = 0;
            _dataEndIdx = 0;
            _size = size;
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
            _size = 0;
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
                    _dataStartIdx = 0;
                    _dataEndIdx = 0;
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

            ReadOnlySpan<byte> readSpan = _buf.AsSpan(_dataStartIdx, _dataEndIdx);
            int readLength = Math.Min(span.Length, readSpan.Length);

            readSpan.Slice(0, readLength).CopyTo(span);
            _dataStartIdx += readLength;

            Debug.Assert(_dataStartIdx <= _dataEndIdx);
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
            if (_size <= _dataEndIdx + span.Length)
            {
                if (autoExpand)
                { // Expand buffer
                    if (!Expand(_dataEndIdx + span.Length))
                        throw new InvalidOperationException("Failed to expand buffer.");
                }
                else
                { // Write as much as possible
                    inputSpan = span.Slice(0, _size - _dataEndIdx);
                }
            }

            // Copy inputSpan to a writable portion of the buffer
            inputSpan.CopyTo(_buf.AsSpan(_dataEndIdx, _size - _dataEndIdx));

            _dataEndIdx += inputSpan.Length;

            Debug.Assert(_dataStartIdx <= _dataEndIdx);
            Debug.Assert(_dataEndIdx <= _size);
            return inputSpan.Length;
        }

        /// <summary>
        /// Reset the position of the buffer to zero.
        /// </summary>
        /// <remarks>
        /// The data of the buffer itself is not cleared.
        /// </remarks>
        public void Clear()
        {
            _dataStartIdx = 0;
            _dataEndIdx = 0;
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

            if (0 < _dataEndIdx - _dataStartIdx)
                System.Buffer.BlockCopy(oldBuffer, _dataStartIdx, newBuffer, _dataStartIdx, _dataEndIdx - _dataStartIdx);

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
            if (len < 0 || _dataEndIdx < _dataStartIdx + len)
                return false;

            byte[] oldBuffer = _buf;
            byte[] newBuffer = _pool.Rent(_size);

            System.Buffer.BlockCopy(oldBuffer, _dataStartIdx + len, newBuffer, 0, _dataEndIdx - _dataStartIdx - len);

            if (0 < _size) // Buffer of zero size does not belong to the pool.
                _pool.Return(oldBuffer);

            _buf = newBuffer;
            _dataEndIdx -= len + _dataStartIdx;
            _dataStartIdx = 0;
            Debug.Assert(0 <= _dataEndIdx && _dataEndIdx <= _size);

            return true;
        }
    }
}
