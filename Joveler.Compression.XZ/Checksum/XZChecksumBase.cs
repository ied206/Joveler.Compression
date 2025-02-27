﻿/*
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
using System.IO;

namespace Joveler.Compression.XZ.Checksum
{
    #region BaseChecksum
    public abstract class XZChecksumBase<T> where T : unmanaged
    {
        #region Fields and Properties
        protected int DefaultBufferSize { get; } = 1024 * 1024;
        public T InitChecksum { get; }
        public T Checksum { get; protected set; }
        #endregion

        #region Constructor
        protected XZChecksumBase(T initChecksum)
        {
            InitChecksum = initChecksum;
            Reset();
        }
        #endregion

        #region Append
        public T Append(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - offset < count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return Checksum;

            Checksum = AppendCore(Checksum, buffer, offset, count);
            return Checksum;
        }

        public T Append(ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
                return Checksum;

            Checksum = AppendCore(Checksum, span);
            return Checksum;
        }

        public T Append(Stream stream)
        {
            return Append(stream, DefaultBufferSize);
        }

        public T Append(Stream stream, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, bufferSize);
                if (bytesRead == 0)
                    break;

                // Some Checksum functions reset checksum to the init value when the buffer is empty. (Ex: zlib)
                Checksum = AppendCore(Checksum, buffer, 0, bytesRead);
            }
            return Checksum;
        }
        #endregion

        #region Reset
        public void Reset()
        {
            Checksum = InitChecksum;
        }

        public void Reset(T reset)
        {
            Checksum = reset;
        }
        #endregion

        #region AppendCore
        /// <summary>
        /// Please override this method to implement actual checksum calculation.
        /// Must not affect internal Checksum property, make it works like a static method.
        /// Arguments are prefiltered by Append methods, so it does not need to be checked here.
        /// </summary>
        protected abstract T AppendCore(T checksum, byte[] buffer, int offset, int count);

        /// <summary>
        /// Please override this method to implement actual checksum calculation.
        /// Must not affect internal Checksum property, make it works like a static method.
        /// Arguments are prefiltered by Append methods, so it does not need to be checked here.
        /// </summary>
        protected abstract T AppendCore(T checksum, ReadOnlySpan<byte> span);
        #endregion
    }
    #endregion
}
