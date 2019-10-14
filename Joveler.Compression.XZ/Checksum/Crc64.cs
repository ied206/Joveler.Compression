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
    #region Crc64Checksum
    public sealed class Crc64Checksum : BaseChecksum<ulong>
    {
        #region Const
        public const ulong InitCrc64 = 0;
        #endregion

        #region Constructors
        public Crc64Checksum() : base(InitCrc64)
        {
            XZInit.EnsureLoaded();
        }

        public Crc64Checksum(int bufferSize) : base(InitCrc64, bufferSize)
        {
            XZInit.EnsureLoaded();
        }
        #endregion

        #region Reset
        public override void Reset()
        {
            Checksum = InitCrc64;
        }

        public override void Reset(ulong reset)
        {
            Checksum = reset;
        }
        #endregion

        #region AppendCore
        /// <inheritdoc/>
        protected override unsafe ulong AppendCore(ulong checksum, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufPtr = buffer.AsSpan(offset))
            {
                return XZInit.Lib.LzmaCrc64(bufPtr, new UIntPtr((uint)count), checksum);
            }
        }

        /// <inheritdoc/>
        protected override unsafe ulong AppendCore(ulong checksum, ReadOnlySpan<byte> span)
        {
            fixed (byte* bufPtr = span)
            {
                return XZInit.Lib.LzmaCrc64(bufPtr, new UIntPtr((uint)span.Length), checksum);
            }
        }
        #endregion
    }
    #endregion

    #region Crc64Algorithm
    public sealed class Crc64Algorithm : HashAlgorithm
    {
        private Crc64Checksum _crc64;

        public Crc64Algorithm()
        {
            Initialize();
        }

        public override void Initialize()
        {
            XZInit.EnsureLoaded();

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
