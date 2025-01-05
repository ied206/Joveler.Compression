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
                if (_singleThreadStream != null)
                    return _singleThreadStream.BaseStream;
                if (_parallelCompressStream != null)
                    return _parallelCompressStream.BaseStream;
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

                if (_singleThreadStream != null)
                    _totalIn = _singleThreadStream.TotalIn;
                if (_parallelCompressStream != null)
                    _totalIn = _parallelCompressStream.TotalIn;
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

                if (_singleThreadStream != null)
                    _totalOut = _singleThreadStream.TotalOut;
                if (_parallelCompressStream != null)
                    _totalOut = _parallelCompressStream.TotalOut;
                return _totalOut;
            }
        }

        // Singlethread Compress/Decompress
        private DeflateSerialStream? _singleThreadStream = null;
        // Multithread Parallel Compress
        private DeflateThreadedStream? _parallelCompressStream = null;

        /// <summary>
        /// Default buffer size for internal buffer, to be used in single-threaded operation.
        /// </summary>
        internal const int DefaultBufferSize = DeflateSerialStream.DefaultBufferSize;
        /// <summary>
        /// Default block size for parallel compress operation.
        /// </summary>
        internal const int DefaultBlockSize = DeflateThreadedStream.DefaultBlockSize;
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing DeflateStream.
        /// </summary>
        public DeflateStreamBase(Stream baseStream, ZLibCompressOptions compOpts, ZLibOperateFormat format)
        {
            _singleThreadStream = new DeflateSerialStream(baseStream, compOpts, format);
        }

        public DeflateStreamBase(Stream baseStream, ZLibThreadedCompressOptions pcompOpts, ZLibOperateFormat format)
        {
            _parallelCompressStream = new DeflateThreadedStream(baseStream, pcompOpts, format);
        }

        public DeflateStreamBase(Stream baseStream, ZLibDecompressOptions decompOpts, ZLibOperateFormat format)
        {
            _singleThreadStream = new DeflateSerialStream(baseStream, decompOpts, format);
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
                if (_singleThreadStream != null)
                {
                    _singleThreadStream.Dispose();

                    _totalIn = _singleThreadStream.TotalIn;
                    _totalOut = _singleThreadStream.TotalOut;

                    _singleThreadStream = null;
                }

                if (_parallelCompressStream != null)
                {
                    _parallelCompressStream.Dispose();

                    _totalIn = _parallelCompressStream.TotalIn;
                    _totalOut = _parallelCompressStream.TotalOut;

                    _parallelCompressStream = null;
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
            if (_singleThreadStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");
            return _singleThreadStream.Read(buffer, offset, count);
        }

        /// <inheritdoc />
#if NETCOREAPP3_1
        public override unsafe int Read(Span<byte> span)
#else
        public unsafe int Read(Span<byte> span)
#endif
        { // Parallel decompression is not yet supported.
            if (_singleThreadStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");
            return _singleThreadStream.Read(span);
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_parallelCompressStream != null)
            {
                _parallelCompressStream.Write(buffer, offset, count);
                return;
            }

            if (_singleThreadStream != null)
            {
                _singleThreadStream.Write(buffer, offset, count);
                return;
            }

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        /// <inheritdoc />
#if NETCOREAPP
        public override unsafe void Write(ReadOnlySpan<byte> span)
#else
        public unsafe void Write(ReadOnlySpan<byte> span)
#endif
        {
            if (_parallelCompressStream != null)
            {
                _parallelCompressStream.Write(span);
                return;
            }

            if (_singleThreadStream != null)
            {
                _singleThreadStream.Write(span);
                return;
            }

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        /// <inheritdoc />
        public override unsafe void Flush()
        {
            if (_parallelCompressStream != null)
            {
                _parallelCompressStream.Flush();
                return;
            }

            if (_singleThreadStream != null)
            {
                _singleThreadStream.Flush();
                return;
            }

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        /// <inheritdoc />
        public override bool CanRead
        {
            get
            {
                if (_parallelCompressStream != null)
                    return _parallelCompressStream.CanRead;
                else if (_singleThreadStream != null)
                    return _singleThreadStream.CanRead;
                throw new ObjectDisposedException("This stream had been disposed.");
            }
        }
        /// <inheritdoc />
        public override bool CanWrite
        {
            get
            {
                if (_parallelCompressStream != null)
                    return _parallelCompressStream.CanWrite;
                else if (_singleThreadStream != null)
                    return _singleThreadStream.CanWrite;
                throw new ObjectDisposedException("This stream had been disposed.");
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
                if (_parallelCompressStream != null)
                    return _parallelCompressStream.CompressionRatio;
                if (_singleThreadStream != null)
                    return _singleThreadStream.CompressionRatio;
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
        public DeflateStream(Stream baseStream, ZLibThreadedCompressOptions pcompOpts)
            : base(baseStream, pcompOpts, ZLibOperateFormat.Deflate) { }

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
        public ZLibStream(Stream baseStream, ZLibThreadedCompressOptions pcompOpts)
            : base(baseStream, pcompOpts, ZLibOperateFormat.ZLib) { }

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
        public GZipStream(Stream baseStream, ZLibThreadedCompressOptions pcompOpts)
            : base(baseStream, pcompOpts, ZLibOperateFormat.GZip) { }

        /// <summary>
        /// Create decompressing GZipStream.
        /// </summary>
        public GZipStream(Stream baseStream, ZLibDecompressOptions decompOpts)
            : base(baseStream, decompOpts, ZLibOperateFormat.GZip) { }
    }
    #endregion
}
