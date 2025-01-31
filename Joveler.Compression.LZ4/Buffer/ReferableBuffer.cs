/*
    Written by Hajin Jang (BSD 2-Clause)
    Copyright (C) 2025-present Hajin Jang

    Redistribution and use in source and binary forms, with or without modification,
    are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this
      list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice, this
      list of conditions and the following disclaimer in the documentation and/or
      other materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
    WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
    ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
    (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
    LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
    ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
    (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
    SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System.Buffers;
using System.Threading;

namespace Joveler.Compression.LZ4.Buffer
{
    /// <summary>
    /// Sharable pooled smart buffer.
    /// </summary>
    /// <remarks>
    /// USE WITH EXTREME CAUTION! Using this incorrectly leads to memory corruption!
    /// </remarks>
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
            return $"RBUF[{_dataStartIdx,7} - {_dataEndIdx,7}/{_capacity,7}] (data: {_dataEndIdx - _dataStartIdx}) (ref: {_refCount})]";
        }
    }
}
