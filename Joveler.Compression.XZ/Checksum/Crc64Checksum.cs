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
    #region Crc64Checksum
    public sealed class Crc64Checksum : XZChecksumBase<ulong>
    {
        #region Const
        public const ulong InitCrc64 = 0;
        #endregion

        #region Constructors
        public Crc64Checksum() : base(InitCrc64)
        {
            XZInit.Manager.EnsureLoaded();
        }

        [Obsolete($"Instance-level bufferSize is deprecated, use default constructor instead.")]
        public Crc64Checksum(int bufferSize) : base(InitCrc64)
        {
            XZInit.Manager.EnsureLoaded();
        }
        #endregion

        #region AppendCore
        /// <inheritdoc/>
        protected override unsafe ulong AppendCore(ulong checksum, byte[] buffer, int offset, int count)
        {
            return AppendCore(checksum, buffer.AsSpan(offset, count));
        }

        /// <inheritdoc/>
        protected override unsafe ulong AppendCore(ulong checksum, ReadOnlySpan<byte> span)
        {
            if (XZInit.Lib == null)
                throw new ObjectDisposedException(nameof(XZInit));

            fixed (byte* bufPtr = span)
            {
                return XZInit.Lib.LzmaCrc64?.Invoke(bufPtr, (nuint)span.Length, checksum) ??
                    throw new EntryPointNotFoundException(nameof(XZInit.Lib.LzmaCrc64));
            }
        }
        #endregion
    }
    #endregion
}
