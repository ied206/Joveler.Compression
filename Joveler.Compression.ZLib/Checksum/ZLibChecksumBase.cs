/*
    Written by Hajin Jang
    Copyright (C) 2019-present Hajin Jang

    zlib license

    This software is provided 'as-is', without any express or implied
    warranty.  In no event will the authors be held liable for any damages
    arising from the use of this software.

    Permission is granted to anyone to use this software for any purpose,
    including commercial applications, and to alter it and redistribute it
    freely, subject to the following restrictions:

    1. The origin of this software must not be misrepresented; you must not
       claim that you wrote the original software. If you use this software
       in a product, an acknowledgment in the product documentation would be
       appreciated but is not required.
    2. Altered source versions must be plainly marked as such, and must not be
       misrepresented as being the original software.
    3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.IO;

namespace Joveler.Compression.ZLib.Checksum
{
    #region ZLibChecksumBase
    public abstract class ZLibChecksumBase<T> where T : unmanaged
    {
        #region Fields and Properties
        protected int DefaultBufferSize { get; } = 1024 * 1024;
        public T InitChecksum { get; }
        public T Checksum { get; protected set; }
        #endregion

        #region Constructor
        protected ZLibChecksumBase(T initChecksum)
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

        #region Combine()
        public T Combine(T nextChecksum, int nextInputSize)
        {
            if (nextInputSize < 0)
                throw new ArgumentOutOfRangeException(nameof(nextInputSize));
            Checksum = CombineCore(Checksum, nextChecksum, nextInputSize);
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

        #region CombineCore
        /// <summary>
        /// Please override this method to implement actual checksum combination.
        /// Must not affect internal Checksum property, make it works like a static method.
        /// Arguments are prefiltered by Combine methods, so it does not need to be checked here.
        /// </summary>
        protected abstract T CombineCore(T priorChecksum, T nextChecksum, int nextInputSize);
        #endregion
    }
    #endregion
}
