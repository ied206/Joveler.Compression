/*
    Written by Hajin Jang
    Copyright (C) 2018-present Hajin Jang

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

namespace Joveler.Compression.XZ.Checksum
{
    #region Crc32Checksum
    public sealed class Crc32Checksum : XZChecksumBase<uint>
    {
        #region Const
        public const uint Crc32Init = 0;
        #endregion

        #region Constructors
        public Crc32Checksum() : base(Crc32Init)
        {
            XZInit.Manager.EnsureLoaded();
        }

        [Obsolete($"Instance-level bufferSize is deprecated, use default constructor instead.")]
        public Crc32Checksum(int bufferSize) : base(Crc32Init)
        {
            XZInit.Manager.EnsureLoaded();
        }
        #endregion

        #region AppendCore
        /// <inheritdoc/>
        protected override unsafe uint AppendCore(uint checksum, byte[] buffer, int offset, int count)
        {
            return AppendCore(checksum, buffer.AsSpan(offset, count));
        }

        /// <inheritdoc/>
        protected override unsafe uint AppendCore(uint checksum, ReadOnlySpan<byte> span)
        {
            if (XZInit.Lib == null)
                throw new ObjectDisposedException(nameof(XZInit));

            fixed (byte* bufPtr = span)
            {
                return XZInit.Lib.LzmaCrc32?.Invoke(bufPtr, (nuint)span.Length, checksum) ??
                    throw new EntryPointNotFoundException(nameof(XZInit.Lib.LzmaCrc32));
            }
        }
        #endregion
    }
    #endregion
}
