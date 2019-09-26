using System;
using System.IO;
using System.Security.Cryptography;

namespace Joveler.Compression.XZ.Checksum
{
    #region Crc64Checksum
    public sealed class Crc64Checksum : BaseChecksum<ulong>
    {
        #region Const
        public const ulong ResetCrc64 = 0;
        #endregion

        #region Constructors
        public Crc64Checksum()
            : base(ResetCrc64, ResetCrc64)
        {
            NativeMethods.EnsureLoaded();
        }

        public Crc64Checksum(ulong initCrc64)
            : base(initCrc64, ResetCrc64)
        {
            NativeMethods.EnsureLoaded();
        }

        public Crc64Checksum(int bufferSize)
            : base(ResetCrc64, ResetCrc64, bufferSize)
        {
            NativeMethods.EnsureLoaded();
        }

        public Crc64Checksum(ulong initCrc64, int bufferSize)
            : base(initCrc64, ResetCrc64, bufferSize)
        {
            NativeMethods.EnsureLoaded();
        }
        #endregion

        #region AppendCore
        /// <inheritdoc/>
        protected override unsafe ulong AppendCore(ulong checksum, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufPtr = buffer.AsSpan(offset))
            {
                return NativeMethods.LzmaCrc64(bufPtr, new UIntPtr((uint)count), checksum);
            }
        }

        /// <inheritdoc/>
        protected override unsafe ulong AppendCore(ulong checksum, ReadOnlySpan<byte> span)
        {
            fixed (byte* bufPtr = span)
            {
                return NativeMethods.LzmaCrc64(bufPtr, new UIntPtr((uint)span.Length), checksum);
            }
        }
        #endregion
    }
    #endregion

    #region Crc64Stream
    public sealed class Crc64Stream : BaseChecksumStream<ulong>
    {
        #region Constructor
        public Crc64Stream(Stream stream)
            : base(new Crc64Checksum(), stream)
        {
            NativeMethods.EnsureLoaded();
        }

        public Crc64Stream(Stream stream, ulong initCrc64)
            : base(new Crc64Checksum(initCrc64), stream)
        {
            NativeMethods.EnsureLoaded();
        }

        public Crc64Stream(Stream stream, int bufferSize)
            : base(new Crc64Checksum(bufferSize), stream)
        {
            NativeMethods.EnsureLoaded();
        }

        public Crc64Stream(Stream stream, ulong initCrc64, int bufferSize)
            : base(new Crc64Checksum(initCrc64, bufferSize), stream)
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
