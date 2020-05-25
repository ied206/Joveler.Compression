/*
   Derived from zlib header files (zlib license)
   Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler
   Copyright (C) 2017-2020 Hajin Jang

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
    #region BaseChecksum
    public abstract class ChecksumBase<T>
    {
        #region Fields and Properties
        protected readonly int _bufferSize = 64 * 1024;

        public virtual T InitChecksum { get; private set; }
        public virtual T Checksum { get; protected set; }
        #endregion

        #region Constructor
        protected ChecksumBase(T initChecksum)
        {
            InitChecksum = initChecksum;
            Reset();
        }

        protected ChecksumBase(T initChecksum, int bufferSize)
        {
            InitChecksum = initChecksum;
            _bufferSize = bufferSize;

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
            byte[] buffer = new byte[_bufferSize];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, _bufferSize);
                if (bytesRead == 0)
                    break;

                // Some Checksum functions reset checksum to the init value when the buffer is empty. (Ex: zlib)
                Checksum = AppendCore(Checksum, buffer, 0, bytesRead);
            }
            return Checksum;
        }
        #endregion

        #region Reset
        public abstract void Reset();
        public abstract void Reset(T reset);
        #endregion

        #region AppendCore
        /// <summary>
        /// Please override this method to implement actual checksum calculation.
        /// Must not affect internal Checksum property, make it works like a static method.
        /// Arguments are prefilted by Append methods, so do not need to check them here.
        /// </summary>
        protected abstract T AppendCore(T checksum, byte[] buffer, int offset, int count);

        /// <summary>
        /// /// Please override this method to implement actual checksum calculation.
        /// Must not affect internal Checksum property, make it works like a static method.
        /// Arguments are prefilted by Append methods, so do not need to check them here.
        /// </summary>
        protected abstract T AppendCore(T checksum, ReadOnlySpan<byte> span);
        #endregion
    }
    #endregion
}
