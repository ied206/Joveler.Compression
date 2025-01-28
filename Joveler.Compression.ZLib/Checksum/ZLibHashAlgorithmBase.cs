/*
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
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Joveler.Compression.ZLib.Checksum
{
    public abstract class ZLibHashAlgorithmBase : HashAlgorithm
    {
        private bool _disposed = false;
        private readonly ZLibChecksumBase<uint> _check;
        private readonly ByteOrder _endian;

        public ByteOrder Endian => _endian;
        public uint Checksum => _check.Checksum;

        protected ZLibHashAlgorithmBase(ByteOrder endian, ZLibChecksumBase<uint> check)
        {
            _endian = endian;
            _check = check;
        }

        public override void Initialize()
        {
            ZLibInit.Manager.EnsureLoaded();

            _check.Reset();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                { // Disposed managed state

                }

                // Dispose unmanaged state
                _disposed = true;
            }

            base.Dispose(disposing);
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _check.Append(array, ibStart, cbSize);
        }

#if NETCOREAPP
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            _check.Append(source);
        }
#endif

        protected override byte[] HashFinal()
        {
            if (_check == null)
                throw new ObjectDisposedException(nameof(_check));

            byte[] buf = new byte[4];
            uint checksum = _check.Checksum;
            switch (_endian)
            {
                case ByteOrder.LittleEndian:
                    BinaryPrimitives.WriteUInt32LittleEndian(buf, checksum);
                    break;
                case ByteOrder.BigEndian:
                    BinaryPrimitives.WriteUInt32BigEndian(buf, checksum);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid {nameof(ByteOrder)} [{_endian}]");
            }
            return buf;
        }

#if NETCOREAPP
        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            if (_check == null)
                throw new ObjectDisposedException(nameof(_check));

            if (destination.Length < 4)
            {
                bytesWritten = 0;
                return false;
            }

            uint checksum = _check.Checksum;
            switch (Endian)
            {
                case ByteOrder.LittleEndian:
                    BinaryPrimitives.WriteUInt32LittleEndian(destination, checksum);
                    break;
                case ByteOrder.BigEndian:
                    BinaryPrimitives.WriteUInt32BigEndian(destination, checksum);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid XXHashBytesEndian [{Endian}]");
            }
            bytesWritten = 4;
            return true;
        }
#endif

        public override int HashSize => 4 * 8;

        public override bool CanReuseTransform => true;

        public override bool CanTransformMultipleBlocks => true;
    }
}