/*
    Written by Hajin Jang
    Copyright (C) 2018-present Hajin Jang

    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Joveler.Compression.XZ.Checksum
{
    public abstract class XZHashAlgorithmBase<T> : HashAlgorithm where T : unmanaged
    {
        private bool _disposed = false;
        private readonly XZChecksumBase<T> _check;
        private readonly ByteOrder _endian;

        public ByteOrder Endian => _endian;
        public T Checksum => _check.Checksum;

        protected XZHashAlgorithmBase(ByteOrder endian, XZChecksumBase<T> check)
        {
            _endian = endian;
            _check = check;
        }

        public override void Initialize()
        {
            XZInit.Manager.EnsureLoaded();

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

            T checksum = _check.Checksum;
            return _endian switch
            {
                ByteOrder.LittleEndian => ConvertValueToBytesLE(checksum),
                ByteOrder.BigEndian => ConvertValueToBytesBE(checksum),
                _ => throw new InvalidOperationException($"Invalid {nameof(ByteOrder)} [{_endian}]"),
            };
        }

#if NETCOREAPP
        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            if (_check == null)
                throw new ObjectDisposedException(nameof(_check));

            if (destination.Length < _hashSize)
            {
                bytesWritten = 0;
                return false;
            }

            T checksum = _check.Checksum;
            switch (Endian)
            {
                case ByteOrder.LittleEndian:
                    ConvertValueToBytesLE(destination, checksum);
                    break;
                case ByteOrder.BigEndian:
                    ConvertValueToBytesBE(destination, checksum);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid {nameof(ByteOrder)} [{Endian}]");
            }
            bytesWritten = _hashSize;
            return true;
        }
#endif

        public void GetHashBytesLE(Span<byte> destSpan)
        {
            if (destSpan.Length < _hashSize)
                throw new ArgumentOutOfRangeException(nameof(destSpan));
            ConvertValueToBytesLE(destSpan, _check.Checksum);
        }
        public void GetHashBytesBE(Span<byte> destSpan)
        {
            if (destSpan.Length < _hashSize)
                throw new ArgumentOutOfRangeException(nameof(destSpan));
            ConvertValueToBytesBE(destSpan, _check.Checksum);
        }

        public abstract byte[] ConvertValueToBytesLE(T val);
        public abstract byte[] ConvertValueToBytesBE(T val);
        public abstract void ConvertValueToBytesLE(Span<byte> dest, T val);
        public abstract void ConvertValueToBytesBE(Span<byte> dest, T val);

        private readonly int _hashSize = Marshal.SizeOf<T>();
        public override int HashSize => _hashSize;

        public override bool CanReuseTransform => true;

        public override bool CanTransformMultipleBlocks => true;
    }
}