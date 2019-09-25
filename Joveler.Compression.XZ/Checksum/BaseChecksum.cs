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
        public virtual T Checksum { get; private set; }
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

        #region ValidateReadWriteArgs
        public void ValidateReadWriteArgs(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - offset < count)
                throw new ArgumentOutOfRangeException(nameof(count));
        }
        #endregion

        #region Append, Reset
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

            Checksum = Compute(Checksum, buffer, offset, count);
            return Checksum;
        }

        public T Append(ReadOnlySpan<byte> span)
        {
            Checksum = Compute(Checksum, span);
            return Checksum;
        }

        public T Append(Stream stream)
        {
            int bytesRead;
            byte[] buffer = new byte[_bufferSize];
            do
            {
                bytesRead = stream.Read(buffer, 0, _bufferSize);
                Checksum = Compute(Checksum, buffer, 0, bytesRead);
            }
            while (0 < bytesRead);

            return Checksum;
        }

        public void Reset()
        {
            Checksum = InitChecksum;
        }
        #endregion

        #region Compute methods
        /// <summary>
        /// Does not affect internal Checksum property, working just like a static method.
        /// </summary>
        public abstract T Compute(T checksum, byte[] buffer, int offset, int count);

        /// <summary>
        /// Does not affect internal Checksum property, working just like a static method.
        /// </summary>
        public abstract T Compute(T checksum, ReadOnlySpan<byte> span);

        /// <summary>
        /// Does not affect internal Checksum property, working just like a static method.
        /// </summary>
        public abstract T Compute(T checksum, Stream stream);
        #endregion
    }
    #endregion

    #region BaseChecksumStream
    public abstract class BaseChecksumStream<T> : Stream
    {
        #region Fields and Properties
        private readonly BaseChecksum<T> _check;
        public T Checksum => _check.Checksum;
        public Stream BaseStream { get; }
        #endregion

        #region Constructor
        protected BaseChecksumStream(BaseChecksum<T> check, Stream stream)
        {
            NativeMethods.EnsureLoaded();
            BaseStream = stream;
            _check = check;
        }
        #endregion

        #region Stream Methods
        public override int Read(byte[] buffer, int offset, int count)
        {
            NativeMethods.EnsureLoaded();

            int bytesRead = BaseStream.Read(buffer, offset, count);
            _check.Append(buffer, offset, bytesRead);
            return bytesRead;
        }

        public override unsafe void Write(byte[] buffer, int offset, int count)
        {
            NativeMethods.EnsureLoaded();

            BaseStream.Write(buffer, offset, count);
            _check.Append(buffer, offset, count);
        }

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override bool CanRead => BaseStream.CanRead;
        public override bool CanWrite => BaseStream.CanWrite;
        public override bool CanSeek => BaseStream.CanSeek;

        public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);
        public override void SetLength(long value) => BaseStream.SetLength(value);
        public override long Length => BaseStream.Length;

        public override long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }
        #endregion
    }
    #endregion
}
