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
    public sealed class XXH32Stream : XXHashStreamBase<uint>
    {
        #region Fields and Properties
        private bool _disposed = false;
        private IntPtr _xxh32State = IntPtr.Zero;
        #endregion

        #region Const
        public const uint XXH32Init = 0;
        #endregion

        #region Constructors
        public XXH32Stream() : this(XXH32Init) { }

        public XXH32Stream(uint defaultSeed) : base(defaultSeed)
        {
            LZ4Init.Manager.EnsureLoaded();
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            _xxh32State = LZ4Init.Lib.XXH32CreateState!();
            if (_xxh32State == IntPtr.Zero)
                throw new OutOfMemoryException(nameof(LZ4Init.Lib.XXH32CreateState));

            XXHashErrorCode errCode = LZ4Init.Lib.XXH32Reset!(_xxh32State, defaultSeed);
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

                // Calculate last digest if it hasn't been calculated
                Digest();

                // Dispose unmanaged state
                if (_xxh32State != IntPtr.Zero)
                {
                    if (LZ4Init.Lib == null)
                        throw new ObjectDisposedException(nameof(LZ4Init));

                    // Calls C free() internally, does not need to check return code
                    LZ4Init.Lib.XXH32FreeState!(_xxh32State);
                    _xxh32State = IntPtr.Zero;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
        #endregion

        #region ValueType to Bytes
        public override byte[] ConvertValueToBytesLE(uint checksum)
        {
            byte[] buf = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buf, checksum);
            return buf;
        }

        public override byte[] ConvertValueToBytesBE(uint checksum)
        {
            byte[] buf = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buf, checksum);
            return buf;
        }

        public override void ConvertValueToBytesLE(Span<byte> dest, uint val)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(dest, val);
        }

        public override void ConvertValueToBytesBE(Span<byte> dest, uint val)
        {
            BinaryPrimitives.WriteUInt32BigEndian(dest, val);
        }

        public override uint ConvertValueFromBytesLE(ReadOnlySpan<byte> span)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(span);
        }

        public override uint ConvertValueFromBytesBE(ReadOnlySpan<byte> span)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(span);
        }
        #endregion

        #region Native Wrappers
        protected override void ResetCore(uint seed)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            XXHashErrorCode errCode = LZ4Init.Lib.XXH32Reset!(_xxh32State, seed);
            XXHashException.CheckReturnValue(errCode);
        }

        protected override unsafe void UpdateCore(ReadOnlySpan<byte> span)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            fixed (byte* bufPtr = span)
            {
                XXHashErrorCode ret = LZ4Init.Lib.XXH32Update!(_xxh32State, bufPtr, (nuint)span.Length);
                XXHashException.CheckReturnValue(ret);
            }
        }

        protected override uint DigestCore()
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            uint digest = LZ4Init.Lib.XXH32Digest!(_xxh32State);
            return digest;
        }
        #endregion

        #region Static
        /// <summary>
        /// One-time XXH32 calculation, which would be faster than stream instance in small data.
        /// </summary>
        public static unsafe uint XXH32(byte[] buffer, int offset, int count)
        {
            return XXH32(XXH32Init, buffer, offset, count);
        }

        /// <summary>
        /// One-time XXH32 calculation, which would be faster than stream instance in small data.
        /// </summary>
        public static unsafe uint XXH32(uint seed, byte[] buffer, int offset, int count)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            fixed (byte* bufPtr = buffer.AsSpan(offset, count))
            {
                return LZ4Init.Lib.XXH32!(bufPtr, (nuint)count, seed);
            }
        }

        /// <summary>
        /// One-time XXH32 calculation, which would be faster than stream instance in small data.
        /// </summary>
        public static unsafe uint XXH32(ReadOnlySpan<byte> span)
        {
            return XXH32(XXH32Init, span);
        }

        /// <summary>
        /// One-time XXH32 calculation, which would be faster than stream instance in small data.
        /// </summary>
        public static unsafe uint XXH32(uint seed, ReadOnlySpan<byte> span)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            fixed (byte* bufPtr = span)
            {
                return LZ4Init.Lib.XXH32!(bufPtr, (nuint)span.Length, seed);
            }
        }
        #endregion
    }
}
