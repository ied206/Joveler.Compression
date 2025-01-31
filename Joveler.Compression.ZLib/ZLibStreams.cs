/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

    Written by Hajin Jang
    Copyright (C) 2017-present Hajin Jang

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
using System.Runtime.CompilerServices;

namespace Joveler.Compression.ZLib
{
    #region DeflateStreamBase
    /// <summary>
    /// The stream which compresses or decompresses zlib-related stream format.
    /// <para>This symbol can be changed anytime, consider this as not a part of public ABI!</para>
    /// </summary>
    public abstract class DeflateStreamBase : Stream
    {
        #region Fields and Properties
        private bool _disposed = false;

        public Stream? BaseStream
        {
            get
            {
                if (_serialStream != null)
                    return _serialStream.BaseStream;
                if (_parallelStream != null)
                    return _parallelStream.BaseStream;
                throw new ObjectDisposedException("This stream had been disposed.");
            }
        }
        private long _totalIn = 0;
        public long TotalIn
        {
            get
            {
                if (_disposed)
                    return _totalIn;

                if (_serialStream != null)
                    _totalIn = _serialStream.TotalIn;
                if (_parallelStream != null)
                    _totalIn = _parallelStream.TotalIn;
                return _totalIn;
            }
        }
        private long _totalOut = 0;
        public long TotalOut
        {
            get
            {
                if (_disposed)
                    return _totalOut;

                if (_serialStream != null)
                    _totalOut = _serialStream.TotalOut;
                if (_parallelStream != null)
                    _totalOut = _parallelStream.TotalOut;
                return _totalOut;
            }
        }

        private bool _isAborted = false;
        public bool IsAborted
        {
            get
            {
                if (_serialStream != null)
                    _isAborted = _serialStream.IsAborted;
                if (_parallelStream != null)
                    _isAborted = _parallelStream.IsAborted;
                return _isAborted;
            }
        }

        // Singlethread Compress/Decompress
        private DeflateSerialStream? _serialStream = null;
        // Multithread Parallel Compress
        private DeflateParallelStream? _parallelStream = null;

        /// <summary>
        /// Default buffer size for internal buffer, to be used in single-threaded operation.
        /// </summary>
        internal const int DefaultBufferSize = DeflateSerialStream.DefaultBufferSize;
        /// <summary>
        /// Default block size for parallel compress operation.
        /// </summary>
        internal const int DefaultChunkSize = DeflateParallelStream.DefaultChunkSize;
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing DeflateStream.
        /// </summary>
        public DeflateStreamBase(Stream baseStream, ZLibCompressOptions compOpts, ZLibOperateFormat format)
        {
            _serialStream = new DeflateSerialStream(baseStream, compOpts, format);
        }

        public DeflateStreamBase(Stream baseStream, ZLibCompressOptions compOpts, ZLibParallelCompressOptions pcompOpts, ZLibOperateFormat format)
        {
            _parallelStream = new DeflateParallelStream(baseStream, compOpts, pcompOpts, format);
        }

        public DeflateStreamBase(Stream baseStream, ZLibDecompressOptions decompOpts, ZLibOperateFormat format)
        {
            _serialStream = new DeflateSerialStream(baseStream, decompOpts, format);
        }
        #endregion

        #region Disposable Pattern
        ~DeflateStreamBase()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                { // Dispose managed state.

                }

                // Dispose unmanaged resources, and set large fields to null.
                if (_serialStream != null)
                {
                    _serialStream.Dispose();

                    _totalIn = _serialStream.TotalIn;
                    _totalOut = _serialStream.TotalOut;
                    _isAborted = _serialStream.IsAborted;

                    _serialStream = null;
                }

                if (_parallelStream != null)
                {
                    _parallelStream.Dispose();

                    _totalIn = _parallelStream.TotalIn;
                    _totalOut = _parallelStream.TotalOut;
                    _isAborted = _parallelStream.IsAborted;

                    _parallelStream = null;
                }

                _disposed = true;
            }

            // Dispose the base class
            base.Dispose(disposing);
        }
        #endregion

        #region Stream Methods and Properties
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        { // Parallel decompression is not yet supported.
            if (_serialStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");
            return _serialStream.Read(buffer, offset, count);
        }

        /// <inheritdoc />
#if NETCOREAPP
        public override int Read(Span<byte> span)
#else
        public int Read(Span<byte> span)
#endif
        { // Parallel decompression is not yet supported.
            if (_serialStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");
            return _serialStream.Read(span);
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_parallelStream != null)
            {
                _parallelStream.Write(buffer, offset, count);
                return;
            }

            if (_serialStream != null)
            {
                _serialStream.Write(buffer, offset, count);
                return;
            }

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        /// <inheritdoc />
#if NETCOREAPP
        public override void Write(ReadOnlySpan<byte> span)
#else
        public void Write(ReadOnlySpan<byte> span)
#endif
        {
            if (_parallelStream != null)
            {
                _parallelStream.Write(span);
                return;
            }

            if (_serialStream != null)
            {
                _serialStream.Write(span);
                return;
            }

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        /// <inheritdoc />
        public override void Flush()
        {
            if (_parallelStream != null)
            {
                _parallelStream.Flush();
                return;
            }

            if (_serialStream != null)
            {
                _serialStream.Flush();
                return;
            }

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        public void Abort()
        {
            if (_parallelStream != null)
            {
                _parallelStream.Abort();
                return;
            }

            if (_serialStream != null)
            {
                _serialStream.Abort();
                return;
            }

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        /// <inheritdoc />
        public override bool CanRead
        {
            get
            {
                if (_parallelStream != null)
                    return _parallelStream.CanRead;
                else if (_serialStream != null)
                    return _serialStream.CanRead;
                else
                    return false;
            }
        }
        /// <inheritdoc />
        public override bool CanWrite
        {
            get
            {
                if (_parallelStream != null)
                    return _parallelStream.CanWrite;
                else if (_serialStream != null)
                    return _serialStream.CanWrite;
                else
                    return false;
            }
        }
        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException($"{nameof(Seek)}() not supported.");
        }
        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException($"{nameof(SetLength)} not supported.");
        }
        /// <inheritdoc />
        public override long Length => throw new NotSupportedException($"{nameof(Length)} not supported.");
        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException($"{nameof(Position)} not supported.");
            set => throw new NotSupportedException($"{nameof(Position)} not supported.");
        }

        public double CompressionRatio
        {
            get
            {
                if (_parallelStream != null)
                    return _parallelStream.CompressionRatio;
                if (_serialStream != null)
                    return _serialStream.CompressionRatio;
                throw new ObjectDisposedException("This stream had been disposed.");
            }
        }
        #endregion

        #region (internal, private) Check Arguments
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CheckReadWriteArgs(byte[] buffer, int offset, int count)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CheckBufferSize(int bufferSize)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            return Math.Max(bufferSize, 4096);
        }

        internal static int ProcessFormatWindowBits(ZLibWindowBits windowBits, ZLibStreamOperateMode mode, ZLibOperateFormat format)
        {
            if (!Enum.IsDefined(typeof(ZLibWindowBits), windowBits))
                throw new ArgumentOutOfRangeException(nameof(windowBits));

            int bits = (int)windowBits;
            switch (format)
            {
                case ZLibOperateFormat.Deflate:
                    // -1 ~ -15 process raw deflate data
                    return bits * -1;
                case ZLibOperateFormat.GZip:
                    // 16 ~ 31, i.e. 16 added to 0..15: process gzip-wrapped deflate data (RFC 1952)
                    return bits += 16;
                case ZLibOperateFormat.ZLib:
                    // 0 ~ 15: zlib format
                    return bits;
                case ZLibOperateFormat.BothZLibGZip:
                    // 32 ~ 47 (32 added to 0..15): automatically detect either a gzip or zlib header (but not raw deflate data), and decompress accordingly.
                    if (mode == ZLibStreamOperateMode.Decompress)
                        return bits += 32;
                    else
                        throw new ArgumentException(nameof(format));
                default:
                    throw new ArgumentException(nameof(format));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckMemLevel(ZLibMemLevel memLevel)
        {
            if (!Enum.IsDefined(typeof(ZLibMemLevel), memLevel))
                throw new ArgumentOutOfRangeException(nameof(memLevel));
        }
        #endregion
    }
    #endregion

    #region DeflateStream
    /// <inheritdoc />
    /// <summary>
    /// The stream which compress or decompress deflate stream format.
    /// </summary>
    public sealed class DeflateStream : DeflateStreamBase
    {
        /// <summary>
        /// Create compressing DeflateStream.
        /// </summary>
        public DeflateStream(Stream baseStream, ZLibCompressOptions compOpts)
            : base(baseStream, compOpts, ZLibOperateFormat.Deflate) { }

        /// <summary>
        /// (EXPERIMENTAL) Create parallel-compressing DeflateStream.
        /// </summary>
        public DeflateStream(Stream baseStream, ZLibCompressOptions compOpts, ZLibParallelCompressOptions pcompOpts)
            : base(baseStream, compOpts, pcompOpts, ZLibOperateFormat.Deflate) { }

        /// <summary>
        /// Create decompressing DeflateStream.
        /// </summary>
        public DeflateStream(Stream baseStream, ZLibDecompressOptions decompOpts)
            : base(baseStream, decompOpts, ZLibOperateFormat.Deflate) { }
    }
    #endregion 

    #region ZLibStream
    /// <inheritdoc />
    /// <summary>
    /// The stream which compress or decompress zlib stream format.
    /// </summary>
    public sealed class ZLibStream : DeflateStreamBase
    {
        /// <summary>
        /// Create compressing ZLibStream.
        /// </summary>
        public ZLibStream(Stream baseStream, ZLibCompressOptions compOpts)
            : base(baseStream, compOpts, ZLibOperateFormat.ZLib) { }

        /// <summary>
        /// (EXPERIMENTAL) Create parallel-compressing ZLibStream.
        /// </summary>
        public ZLibStream(Stream baseStream, ZLibCompressOptions compOpts, ZLibParallelCompressOptions pcompOpts)
            : base(baseStream, compOpts, pcompOpts, ZLibOperateFormat.ZLib) { }

        /// <summary>
        /// Create decompressing ZLibStream.
        /// </summary>
        public ZLibStream(Stream baseStream, ZLibDecompressOptions decompOpts)
            : base(baseStream, decompOpts, ZLibOperateFormat.ZLib) { }
    }
    #endregion

    #region GZipStream
    /// <inheritdoc />
    /// /// <summary>
    /// The stream which compress or decompress gzip stream format.
    /// </summary>
    public sealed class GZipStream : DeflateStreamBase
    {
        /// <summary>
        /// Create compressing GZipStream.
        /// </summary>
        public GZipStream(Stream baseStream, ZLibCompressOptions compOpts)
            : base(baseStream, compOpts, ZLibOperateFormat.GZip) { }

        /// <summary>
        /// (EXPERIMENTAL) Create parallel-compressing GZipStream.
        /// </summary>
        public GZipStream(Stream baseStream, ZLibCompressOptions compOpts, ZLibParallelCompressOptions pcompOpts)
            : base(baseStream, compOpts, pcompOpts, ZLibOperateFormat.GZip) { }

        /// <summary>
        /// Create decompressing GZipStream.
        /// </summary>
        public GZipStream(Stream baseStream, ZLibDecompressOptions decompOpts)
            : base(baseStream, decompOpts, ZLibOperateFormat.GZip) { }
    }
    #endregion
}
