/*
   Derived from zlib header files (zlib license)
   Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler
   Copyright (C) 2017-2020 Hajin Jang

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
using System.Security.Cryptography;

namespace Joveler.Compression.ZLib.Checksum
{
    #region Crc32Checksum
    public sealed class Crc32Checksum : ChecksumBase<uint>
    {
        #region Const
        public const uint Crc32Init = 0;
        #endregion

        #region Constructors
        public Crc32Checksum() : base(Crc32Init)
        {
            ZLibInit.Manager.EnsureLoaded();
        }

        public Crc32Checksum(int bufferSize) : base(Crc32Init, bufferSize)
        {
            ZLibInit.Manager.EnsureLoaded();
        }
        #endregion

        #region Reset
        public override void Reset()
        {
            Checksum = Crc32Init;
        }

        public override void Reset(uint reset)
        {
            Checksum = reset;
        }
        #endregion

        #region AppendCore
        /// <inheritdoc/>
        protected override unsafe uint AppendCore(uint checksum, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufPtr = buffer.AsSpan(offset, count))
            {
                return ZLibInit.Lib.Crc32(checksum, bufPtr, (uint)count);
            }
        }

        /// <inheritdoc/>
        protected override unsafe uint AppendCore(uint checksum, ReadOnlySpan<byte> span)
        {
            fixed (byte* bufPtr = span)
            {
                return ZLibInit.Lib.Crc32(checksum, bufPtr, (uint)span.Length);
            }
        }
        #endregion
    }
    #endregion

    #region Crc32Algorithm
    public sealed class Crc32Algorithm : HashAlgorithm
    {
        private Crc32Checksum _crc32;

        public Crc32Algorithm()
        {
            Initialize();
        }

        public override void Initialize()
        {
            ZLibInit.Manager.EnsureLoaded();

            _crc32 = new Crc32Checksum();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _crc32.Append(array, ibStart, cbSize);
        }

#if NETCOREAPP
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            _crc32.Append(source);
        }
#endif

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(_crc32.Checksum);
        }
    }
    #endregion
}
