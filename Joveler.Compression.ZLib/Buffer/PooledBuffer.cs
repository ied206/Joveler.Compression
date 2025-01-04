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

namespace Joveler.Compression.ZLib.Buffer
{
    internal sealed class PooledBuffer : PooledBufferBase
    {
        public PooledBuffer(ArrayPool<byte> pool, int size) : base(pool, size)
        {
        }

        public PooledBuffer(ArrayPool<byte> pool) : base(pool)
        {
        }

        public override string ToString()
        {
            return $"PBUF[{_dataStartIdx,7} - {_dataEndIdx,7}/{_size,7}] (data: {_dataEndIdx - _dataStartIdx})]";
        }
    }
}
