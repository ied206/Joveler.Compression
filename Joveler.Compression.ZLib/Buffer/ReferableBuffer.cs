using System.Buffers;
using System.Threading;

namespace Joveler.Compression.ZLib.Buffer
{
    internal sealed class ReferableBuffer : PooledBufferBase
    {
        private int _refCount = 0;

        public ReferableBuffer(ArrayPool<byte> pool, int size) : base(pool, size)
        {
        }

        /// <summary>
        /// Create an empty buffer.
        /// </summary>
        /// <param name="pool"></param>
        public ReferableBuffer(ArrayPool<byte> pool) : base(pool)
        {
        }

        public ReferableBuffer AcquireRef()
        {
            Interlocked.Increment(ref _refCount);
            return this;
        }

        public void ReleaseRef()
        {
            if (Interlocked.Decrement(ref _refCount) <= 0)
                Dispose();
        }

        public override string ToString()
        {
            return $"RBUF[{_dataStartIdx,7} - {_dataEndIdx,7}/{_size,7}] (data: {_dataEndIdx - _dataStartIdx}) (ref: {_refCount})]";
        }
    }
}
