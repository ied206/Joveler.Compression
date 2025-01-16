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
using System.Buffers.Binary;

namespace Joveler.Compression.LZ4.XXHash
{
    public sealed class XXH64Stream : XXHashStreamBase<ulong>
    {
        #region Fields and Properties
        private bool _disposed = false;
        private IntPtr _xxh64State = IntPtr.Zero;
        #endregion

        #region Const
        public const ulong XXH64Init = 0;
        #endregion

        #region Constructors
        public XXH64Stream() : this(XXH64Init) { }

        public XXH64Stream(ulong defaultSeed) : base(defaultSeed)
        {
            LZ4Init.Manager.EnsureLoaded();
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            _xxh64State = LZ4Init.Lib.XXH64CreateState!();
            if (_xxh64State == IntPtr.Zero)
                throw new OutOfMemoryException(nameof(LZ4Init.Lib.XXH64CreateState));

            XXHashErrorCode errCode = LZ4Init.Lib.XXH64Reset!(_xxh64State, defaultSeed);
            XXHashException.CheckReturnValue(errCode);
        }
        #endregion

        #region Disposable
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                { // Dispose managed state

                }

                // Calculate last digest it hasn't been calculated
                Digest();

                // Dispose unmanaged state
                if (_xxh64State != IntPtr.Zero)
                {
                    if (LZ4Init.Lib == null)
                        throw new ObjectDisposedException(nameof(LZ4Init));

                    // Calls C free() internally, does not need to check return code
                    LZ4Init.Lib.XXH64FreeState!(_xxh64State);
                    _xxh64State = IntPtr.Zero;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
        #endregion

        #region ValueType to Bytes
        public override byte[] ConvertValueToBytesLE(ulong checksum)
        {
            byte[] buf = new byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buf, checksum);
            return buf;
        }

        public override byte[] ConvertValueToBytesBE(ulong checksum)
        {
            byte[] buf = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(buf, checksum);
            return buf;
        }

        public override void ConvertValueToBytesLE(Span<byte> dest, ulong val)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(dest, val);
        }

        public override void ConvertValueToBytesBE(Span<byte> dest, ulong val)
        {
            BinaryPrimitives.WriteUInt64BigEndian(dest, val);
        }

        public override ulong ConvertValueFromBytesLE(ReadOnlySpan<byte> span)
        {
            return BinaryPrimitives.ReadUInt64LittleEndian(span);
        }

        public override ulong ConvertValueFromBytesBE(ReadOnlySpan<byte> span)
        {
            return BinaryPrimitives.ReadUInt64BigEndian(span);
        }
        #endregion

        #region Native Wrappers
        protected override void ResetCore(ulong seed)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            XXHashErrorCode errCode = LZ4Init.Lib.XXH64Reset!(_xxh64State, seed);
            XXHashException.CheckReturnValue(errCode);
        }

        protected override unsafe void UpdateCore(ReadOnlySpan<byte> span)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            fixed (byte* bufPtr = span)
            {
                XXHashErrorCode ret = LZ4Init.Lib.XXH64Update!(_xxh64State, bufPtr, (nuint)span.Length);
                XXHashException.CheckReturnValue(ret);
            }
        }

        protected override ulong DigestCore()
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            ulong digest = LZ4Init.Lib.XXH64Digest!(_xxh64State);
            return digest;
        }
        #endregion

        #region Static
        /// <inheritdoc/>
        public static unsafe ulong XXHash64(ulong seed, byte[] buffer, int offset, int count)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            fixed (byte* bufPtr = buffer.AsSpan(offset, count))
            {
                return LZ4Init.Lib.XXH64!(bufPtr, (nuint)count, seed);
            }
        }

        /// <inheritdoc/>
        public static unsafe ulong XXHash64(ulong seed, ReadOnlySpan<byte> span)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            fixed (byte* bufPtr = span)
            {
                return LZ4Init.Lib.XXH64!(bufPtr, (nuint)span.Length, seed);
            }
        }
        #endregion
    }
}
