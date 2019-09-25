using System;
using System.IO;
using System.Security.Cryptography;

namespace Joveler.Compression.XZ.Checksum
{
    #region Crc64Checksum
    public sealed class Crc64Checksum : BaseChecksum<ulong>
    {
        #region Const
        public const ulong InitCrc64 = 0;
        #endregion

        #region Constructors
        public Crc64Checksum() : base(InitCrc64)
        {
            NativeMethods.EnsureLoaded();
        }

        public Crc64Checksum(int bufferSize) : base(InitCrc64, bufferSize)
        {
            NativeMethods.EnsureLoaded();
        }
        #endregion

        #region Compute
        /// <inheritdoc/>
        public override unsafe ulong Compute(ulong checksum, byte[] buffer, int offset, int count)
        {
            ValidateReadWriteArgs(buffer, offset, count);
            if (count == 0)
                return checksum;

            fixed (byte* bufPtr = buffer.AsSpan(offset))
            {
                return NativeMethods.LzmaCrc64(bufPtr, new UIntPtr((uint)count), checksum);
            }
        }
        
        /// <inheritdoc/>
        public override unsafe ulong Compute(ulong checksum, ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
                return checksum;

            fixed (byte* bufPtr = span)
            {
                return NativeMethods.LzmaCrc64(bufPtr, new UIntPtr((uint)span.Length), checksum);
            }
        }

        /// <inheritdoc/>
        public override unsafe ulong Compute(ulong checksum, Stream stream)
        {
            byte[] buffer = new byte[_bufferSize];
            int bytesRead;
            do
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                fixed (byte* bufPtr = buffer)
                {
                    checksum = NativeMethods.LzmaCrc64(bufPtr, new UIntPtr((uint)bytesRead), checksum);
                }
            }
            while (0 < bytesRead);

            return checksum;
        }
        #endregion
    }
    #endregion

    #region Crc64Stream
    public sealed class Crc64Stream : BaseChecksumStream<ulong>
    {
        #region Constructor
        public Crc64Stream(Stream stream) : base(new Crc64Checksum(), stream)
        {
            NativeMethods.EnsureLoaded();
        }

        public Crc64Stream(Stream stream, int bufferSize) : base(new Crc64Checksum(bufferSize), stream)
        {
            NativeMethods.EnsureLoaded();
        }
        #endregion
    }
    #endregion

    #region Crc64Algorithm
    public sealed class Crc64Algorithm : HashAlgorithm
    {
        private Crc64Checksum _crc64;

        public override void Initialize()
        {
            NativeMethods.EnsureLoaded();
            _crc64 = new Crc64Checksum();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _crc64.Append(array, ibStart, cbSize);
        }

#pragma warning disable CS0628 // For .Net Standard build
        protected void HashCore(ReadOnlySpan<byte> source)
#pragma warning restore CS0628
        {
            _crc64.Append(source);
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(_crc64.Checksum);
        }
    }
    #endregion
}
