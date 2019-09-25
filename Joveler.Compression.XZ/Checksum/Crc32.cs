using System;
using System.IO;
using System.Security.Cryptography;

namespace Joveler.Compression.XZ.Checksum
{
    #region Crc32Checksum
    public sealed class Crc32Checksum : BaseChecksum<uint>
    {
        #region Const
        public const uint InitCrc32 = 0;
        #endregion

        #region Constructors
        public Crc32Checksum() : base(InitCrc32)
        {
            NativeMethods.EnsureLoaded();
        }

        public Crc32Checksum(int bufferSize) : base(InitCrc32, bufferSize)
        {
            NativeMethods.EnsureLoaded();
        }
        #endregion

        #region Compute
        /// <inheritdoc/>
        public override unsafe uint Compute(uint checksum, byte[] buffer, int offset, int count)
        {
            ValidateReadWriteArgs(buffer, offset, count);
            if (count == 0)
                return checksum;

            fixed (byte* bufPtr = buffer.AsSpan(offset))
            {
                return NativeMethods.LzmaCrc32(bufPtr, new UIntPtr((uint)count), checksum);
            }
        }

        /// <inheritdoc/>
        public override unsafe uint Compute(uint checksum, ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
                return checksum;

            fixed (byte* bufPtr = span)
            {
                return NativeMethods.LzmaCrc32(bufPtr, new UIntPtr((uint)span.Length), checksum);
            }
        }

        /// <inheritdoc/>
        public override unsafe uint Compute(uint checksum, Stream stream)
        {
            byte[] buffer = new byte[NativeMethods.BufferSize];
            int bytesRead;
            do
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                fixed (byte* bufPtr = buffer)
                {
                    checksum = NativeMethods.LzmaCrc32(bufPtr, new UIntPtr((uint)bytesRead), checksum);
                }
            }
            while (0 < bytesRead);

            return checksum;
        }
        #endregion
    }
    #endregion

    #region Crc32Stream
    public sealed class Crc32Stream : BaseChecksumStream<uint>
    {
        #region Constructor
        public Crc32Stream(Stream stream) : base(new Crc32Checksum(), stream)
        {
            NativeMethods.EnsureLoaded();
        }

        public Crc32Stream(Stream stream, int bufferSize) : base(new Crc32Checksum(bufferSize), stream)
        {
            NativeMethods.EnsureLoaded();
        }
        #endregion
    }
    #endregion

    #region Crc32Algorithm
    public sealed class Crc32Algorithm : HashAlgorithm
    {
        private Crc64Checksum _crc32;

        public override void Initialize()
        {
            NativeMethods.EnsureLoaded();
            _crc32 = new Crc64Checksum();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _crc32.Append(array, ibStart, cbSize);
        }

#pragma warning disable CS0628 // For .Net Standard build
        protected void HashCore(ReadOnlySpan<byte> source)
#pragma warning restore CS0628
        {
            _crc32.Append(source);
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(_crc32.Checksum);
        }
    }
    #endregion
}
