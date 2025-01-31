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
    #region Crc32Checksum
    public sealed class Crc32Checksum : ZLibChecksumBase<uint>
    {
        #region Const
        /// <summary>
        /// Equivalent to zlib's crc32(0, NULL, 0);
        /// </summary>
        public const uint Crc32Init = 0;
        #endregion

        #region Constructors
        public Crc32Checksum() : base(Crc32Init)
        {
            ZLibInit.Manager.EnsureLoaded();
        }

        [Obsolete($"Instance-level bufferSize is deprecated, use default constructor instead.")]
        public Crc32Checksum(int bufferSize) : base(Crc32Init)
        {
            ZLibInit.Manager.EnsureLoaded();
        }
        #endregion

        #region AppendCore
        /// <inheritdoc/>
        protected override unsafe uint AppendCore(uint checksum, byte[] buffer, int offset, int count)
        {
            return AppendCore(checksum, buffer.AsSpan(offset, count));
        }

        /// <inheritdoc/>
        protected override unsafe uint AppendCore(uint checksum, ReadOnlySpan<byte> span)
        {
            if (ZLibInit.Lib == null)
                throw new ObjectDisposedException(nameof(ZLibInit));

            fixed (byte* bufPtr = span)
            {
                return ZLibInit.Lib.NativeAbi.Crc32(checksum, bufPtr, (uint)span.Length);
            }
        }
        #endregion

        #region CombineCore
        protected override uint CombineCore(uint priorChecksum, uint nextChecksum, int nextInputSize)
        {
            if (ZLibInit.Lib == null)
                throw new ObjectDisposedException(nameof(ZLibInit));

            return ZLibInit.Lib.NativeAbi.Crc32Combine(priorChecksum, nextChecksum, nextInputSize);
        }
        #endregion
    }
    #endregion
}
