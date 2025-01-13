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
using System.Security.Cryptography;

namespace Joveler.Compression.ZLib.Checksum
{
    #region Adler32Algorithm
    [Obsolete($"Result of [{nameof(Adler32Algorithm)}] depends on processor endianness. Use [{nameof(Adler32Checksum)}] instead.")]
    public sealed class Adler32Algorithm : HashAlgorithm
    {
        private Adler32Checksum _adler32;

        public Adler32Algorithm()
        {
            Initialize();

            if (_adler32 == null)
                throw new InvalidOperationException($"Failed to initialize [{nameof(Adler32Checksum)}]");
        }

        public override void Initialize()
        {
            ZLibInit.Manager.EnsureLoaded();

            _adler32 = new Adler32Checksum();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _adler32.Append(array, ibStart, cbSize);
        }

#if NETCOREAPP
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            _adler32.Append(source);
        }
#endif

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(_adler32.Checksum);
        }

        public override int HashSize => 32;
    }
    #endregion
}