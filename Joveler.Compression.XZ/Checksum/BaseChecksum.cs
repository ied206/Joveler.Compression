using System;
using System.IO;

namespace Joveler.Compression.XZ.Checksum
{
    #region BaseChecksum
    public abstract class BaseChecksum<T>
    {
        #region Fields and Properties
        protected readonly int _bufferSize = 64 * 1024;

        public virtual T InitChecksum { get; private set; }
        public virtual T Checksum { get; protected set; }
        #endregion

        #region Constructor
        protected BaseChecksum(T initChecksum)
        {
            InitChecksum = initChecksum;
            Reset();
        }

        protected BaseChecksum(T initChecksum, int bufferSize)
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
            int bytesRead;
            byte[] buffer = new byte[_bufferSize];
            do
            {
                bytesRead = stream.Read(buffer, 0, _bufferSize);
                Checksum = AppendCore(Checksum, buffer, 0, bytesRead);
            }
            while (0 < bytesRead);

            return Checksum;
        }
        #endregion

        #region Reset
        public abstract void Reset();
        public abstract void Reset(T reset);
        #endregion

        #region Compute methods
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
