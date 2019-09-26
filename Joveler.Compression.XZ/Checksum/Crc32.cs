using System;
using System.IO;
using System.Security.Cryptography;

namespace Joveler.Compression.XZ.Checksum
{
    #region Crc32Checksum
    public sealed class Crc32Checksum : BaseChecksum<uint>
    {
        #region Const
        public const uint Crc32Init = 0;
        #endregion

        #region Constructors
        public Crc32Checksum() : base(Crc32Init)
        {
            NativeMethods.EnsureLoaded();
        }

        public Crc32Checksum(int bufferSize) : base(Crc32Init, bufferSize)
        {
            NativeMethods.EnsureLoaded();
        }
        #endregion

        #region Reset
        public override void Reset()
        {
            Checksum = Crc32Init;
        }

        public override void Reset(uint reset)
        {
            Checksum = Crc32Init;
        }
        #endregion

        #region AppendCore
        /// <inheritdoc/>
        protected override unsafe uint AppendCore(uint checksum, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufPtr = buffer.AsSpan(offset, count))
            {
                return NativeMethods.LzmaCrc32(bufPtr, new UIntPtr((uint)count), checksum);
            }
        }

        /// <inheritdoc/>
        protected override unsafe uint AppendCore(uint checksum, ReadOnlySpan<byte> span)
        {
            fixed (byte* bufPtr = span)
            {
                return NativeMethods.LzmaCrc32(bufPtr, new UIntPtr((uint)span.Length), checksum);
            }
        }
        #endregion
    }
    #endregion

    #region Crc32Algorithm
    public sealed class Crc32Algorithm : HashAlgorithm
    {
        private Crc32Checksum _crc32;

        public Crc32Algorithm()
        {
            Initialize();
        }

        public override void Initialize()
        {
            NativeMethods.EnsureLoaded();

            _crc32 = new Crc32Checksum();
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
