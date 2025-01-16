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

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Joveler.Compression.LZ4.XXHash
{
    public enum XXHashBytesEndian
    {
        LittleEndian = 0,
        BigEndian = 1,
    }

    public abstract class XXHashAlgorithmBase<T> : HashAlgorithm where T : unmanaged
    {
        private bool _disposed = false;
        private readonly int _hashValSize = Marshal.SizeOf(typeof(T));
        private XXHashStreamBase<T>? _stream;

        public XXHashBytesEndian Endian { get; set; }

        protected XXHashAlgorithmBase(XXHashBytesEndian endian, XXHashStreamBase<T> xxhStream)
        {
            Endian = endian;
            _stream = xxhStream;
        }

        public override void Initialize()
        {
            if (_stream == null)
                throw new ObjectDisposedException(nameof(_stream));

            _stream.Reset();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                { // Disposed managed state

                }

                // DIspose unmanaged state
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (_stream == null)
                throw new ObjectDisposedException(nameof(_stream));

            _stream.Write(array, ibStart, cbSize);
        }

#if NETCOREAPP
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            if (_stream == null)
                throw new ObjectDisposedException(nameof(_stream));

            _stream.Write(source);
        }
#endif

        protected override byte[] HashFinal()
        {
            if (_stream == null)
                throw new ObjectDisposedException(nameof(_stream));

            return Endian switch
            {
                XXHashBytesEndian.LittleEndian => _stream.HashBytesLE,
                XXHashBytesEndian.BigEndian => _stream.HashBytesBE,
                _ => throw new InvalidOperationException($"Invalid XXHashBytesEndian [{Endian}]"),
            };
        }

#if NETCOREAPP
        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            if (_stream == null)
                throw new ObjectDisposedException(nameof(_stream));

            if (destination.Length < _hashValSize)
            {
                bytesWritten = 0;
                return false;
            }

            T hashVal = _stream.HashValue;
            switch (Endian)
            {
                case XXHashBytesEndian.LittleEndian:
                    _stream.ConvertValueToBytesLE(destination, hashVal);
                    break;
                case XXHashBytesEndian.BigEndian:
                    _stream.ConvertValueToBytesBE(destination, hashVal);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid XXHashBytesEndian [{Endian}]");
            }
            bytesWritten = _hashValSize;
            return true;
        }
#endif

        public override int HashSize => _hashValSize * 8;

        public override bool CanReuseTransform => false;

        public override bool CanTransformMultipleBlocks => true;
    }
}