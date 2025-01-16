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
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.Compression.LZ4.XXHash
{
    public abstract class XXHashStreamBase<T> : Stream where T : unmanaged
    {
        protected readonly T _defaultSeed;
        public T DefaultSeed => _defaultSeed;

        private readonly int _hashValueSize = Marshal.SizeOf(typeof(T));
        public int HashValueSize => _hashValueSize;

        protected T? _hashFinal;
        /// <summary>
        /// Return result of xxhash.
        /// After getting the hash value, you cannot put more data until you call Reset().
        /// </summary>
        public T HashValue => Digest();

        /// <summary>
        /// Returns result of xxhash in bytes (Big Endian).
        /// After getting the hash value, you cannot put more data until you call Reset().
        /// </summary>
        public byte[] HashBytesLE => ConvertValueToBytesLE(HashValue);
        public byte[] HashBytesBE => ConvertValueToBytesBE(HashValue);
        public void GetHashBytesLE(Span<byte> destSpan)
        {
            if (destSpan.Length < _hashValueSize)
                throw new ArgumentOutOfRangeException(nameof(destSpan));
            ConvertValueToBytesLE(destSpan, HashValue);
        }
        public void GetHashBytesBE(Span<byte> destSpan)
        {
            if (destSpan.Length < _hashValueSize)
                throw new ArgumentOutOfRangeException(nameof(destSpan));
            ConvertValueToBytesBE(destSpan, HashValue);
        }


        public bool IsDigested => _hashFinal.HasValue;

        protected XXHashStreamBase(T defaultSeed)
        {
            _defaultSeed = defaultSeed;
        }

        public abstract byte[] ConvertValueToBytesLE(T val);
        public abstract byte[] ConvertValueToBytesBE(T val);
        public abstract void ConvertValueToBytesLE(Span<byte> dest, T val);
        public abstract void ConvertValueToBytesBE(Span<byte> dest, T val);
        public abstract T ConvertValueFromBytesLE(ReadOnlySpan<byte> span);
        public abstract T ConvertValueFromBytesBE(ReadOnlySpan<byte> span);


        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(buffer.AsSpan(offset, count));
        }

#if NETCOREAPP
        public override unsafe void Write(ReadOnlySpan<byte> span)
#else
        public unsafe void Write(ReadOnlySpan<byte> span)
#endif
        {
            if (_hashFinal.HasValue)
                throw new InvalidOperationException("Hash value had been already calculated. Reset this instance.");

            // xxhash does not treat zero-length buffer as a valid input.
            if (span.Length == 0)
                return;

            UpdateCore(span);
        }

        public T Digest()
        {
            if (_hashFinal.HasValue)
                return _hashFinal.Value;

            _hashFinal = DigestCore();
            return _hashFinal.Value;
        }

        public void Reset()
        {
            ResetCore(_defaultSeed);
            _hashFinal = null;
        }

        public void Reset(T seed)
        {
            ResetCore(seed);
            _hashFinal = null;
        }

        protected abstract void ResetCore(T seed);
        protected abstract void UpdateCore(ReadOnlySpan<byte> span);
        protected abstract T DigestCore();


        public override void Flush()
        {
            // Do nothing
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => !_hashFinal.HasValue;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
