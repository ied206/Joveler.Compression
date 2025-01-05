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

using System.Buffers;
using System.Threading;

namespace Joveler.Compression.ZLib.Buffer
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
            return $"RBUF[{_dataStartIdx,7} - {_dataEndIdx,7}/{_size,7}] (data: {_dataEndIdx - _dataStartIdx}) (ref: {_refCount})]";
        }
    }
}
