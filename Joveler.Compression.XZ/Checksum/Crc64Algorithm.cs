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
using System.Buffers.Binary;

namespace Joveler.Compression.XZ.Checksum
{
    #region Crc64Algorithm
    public sealed class Crc64Algorithm : XZHashAlgorithmBase<ulong>
    {
        public Crc64Algorithm(ByteOrder endian) : base(endian, new Crc64Checksum())
        {
        }

        public static new Crc32Algorithm Create()
        {
            return new Crc32Algorithm(BitConverter.IsLittleEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian);
        }

        public override byte[] ConvertValueToBytesBE(ulong val)
        {
            byte[] buf = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(buf, val);
            return buf;
        }

        public override byte[] ConvertValueToBytesLE(ulong val)
        {
            byte[] buf = new byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buf, val);
            return buf;
        }

        public override void ConvertValueToBytesBE(Span<byte> dest, ulong val)
        {
            BinaryPrimitives.WriteUInt64BigEndian(dest, val);
        }

        public override void ConvertValueToBytesLE(Span<byte> dest, ulong val)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(dest, val);
        }
    }
    #endregion
}
