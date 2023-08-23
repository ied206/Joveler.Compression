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
    #region Adler32Checksum
    public sealed class Adler32Checksum : ChecksumBase<uint>
    {
        #region Const
        public const uint Adler32Init = 1;
        #endregion

        #region Constructors
        public Adler32Checksum() : base(Adler32Init)
        {
            ZLibInit.Manager.EnsureLoaded();
        }

        public Adler32Checksum(int bufferSize) : base(Adler32Init, bufferSize)
        {
            ZLibInit.Manager.EnsureLoaded();
        }
        #endregion

        #region Reset
        public override void Reset()
        {
            Checksum = Adler32Init;
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
                if (ZLibInit.Lib.UseStdcall)
                    return ZLibInit.Lib.Stdcall.Adler32(checksum, bufPtr, (uint)count);
                else
                    return ZLibInit.Lib.Cdecl.Adler32(checksum, bufPtr, (uint)count);
            }
        }

        /// <inheritdoc/>
        protected override unsafe uint AppendCore(uint checksum, ReadOnlySpan<byte> span)
        {
            fixed (byte* bufPtr = span)
            {
                if (ZLibInit.Lib.UseStdcall)
                    return ZLibInit.Lib.Stdcall.Adler32(checksum, bufPtr, (uint)span.Length);
                else
                    return ZLibInit.Lib.Cdecl.Adler32(checksum, bufPtr, (uint)span.Length);
            }
        }
        #endregion
    }
    #endregion

    #region Adler32Algorithm
    public sealed class Adler32Algorithm : HashAlgorithm
    {
        private Adler32Checksum _adler32;

        public Adler32Algorithm()
        {
            Initialize();
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
    }
    #endregion
}