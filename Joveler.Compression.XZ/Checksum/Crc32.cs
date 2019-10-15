/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2019 Hajin Jang

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
            XZInit.Manager.EnsureLoaded();
        }

        public Crc32Checksum(int bufferSize) : base(Crc32Init, bufferSize)
        {
            XZInit.Manager.EnsureLoaded();
        }
        #endregion

        #region Reset
        public override void Reset()
        {
            Checksum = Crc32Init;
        }

        public override void Reset(uint reset)
        {
            Checksum = reset;
        }
        #endregion

        #region AppendCore
        /// <inheritdoc/>
        protected override unsafe uint AppendCore(uint checksum, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufPtr = buffer.AsSpan(offset, count))
            {
                return XZInit.Lib.LzmaCrc32(bufPtr, new UIntPtr((uint)count), checksum);
            }
        }

        /// <inheritdoc/>
        protected override unsafe uint AppendCore(uint checksum, ReadOnlySpan<byte> span)
        {
            fixed (byte* bufPtr = span)
            {
                return XZInit.Lib.LzmaCrc32(bufPtr, new UIntPtr((uint)span.Length), checksum);
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
            XZInit.Manager.EnsureLoaded();

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
