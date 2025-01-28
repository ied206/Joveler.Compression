/*
   Derived from zlib header files (zlib license)
   Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

   Written by Hajin Jang
   Copyright (C) 2017-present Hajin Jang

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

namespace Joveler.Compression.ZLib.Checksum
{
    #region Crc32Algorithm
    public sealed class Crc32Algorithm : ZLibHashAlgorithmBase
    {
        [Obsolete($"Result of this constructor depends on processor endianness. Use constructor with explicit endianness instead.")]
        public Crc32Algorithm() 
            : base(BitConverter.IsLittleEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian, new Crc32Checksum())
        {
        }

        public Crc32Algorithm(ByteOrder endian)
           : base(endian, new Crc32Checksum())
        {
        }

        public static new Crc32Algorithm Create()
        {
            return new Crc32Algorithm(BitConverter.IsLittleEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian);
        }
    }
    #endregion
}
