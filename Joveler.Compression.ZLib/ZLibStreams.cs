﻿#nullable enable

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
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Joveler.Compression.ZLib
{
    #region StreamOptions
    public sealed class ZLibCompressOptions
    {
        /// <summary>
        /// Compression level. The Default is `ZLibCompLevel.Default`.
        /// </summary>
        public ZLibCompLevel Level { get; set; } = ZLibCompLevel.Default;
        /// <summary>
        /// The base two logarithm of the window size (the size of the history buffer).  
        /// It should be in the range from 9 to 15. The default value is 15.
        /// Larger values of this parameter result in better compression at the expense of memory usage.
        /// </summary>
        /// <remarks>
        /// C library allows value of 8 but it have been prohibitted in here due to multiple issues.
        /// </remarks>
        public ZLibWindowBits WindowBits { get; set; } = ZLibWindowBits.Default;
        /// <summary>
        /// Specifies how much memory should be allocated for the internal compression state.
        /// 1 uses minimum memory but is slow and reduces compression ratio; 9 uses maximum memory for optimal speed.
        /// The default value is 8.
        /// </summary>
        public ZLibMemLevel MemLevel { get; set; } = ZLibMemLevel.Default;
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = DeflateStreamBase.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the zlib stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
        /// <summary>
        /// Buffer pool to use for internal buffer.
        /// </summary>
        public ArrayPool<byte>? BufferPool { get; set; } = ArrayPool<byte>.Shared;
    }

    public sealed class ZLibParallelCompressOptions
    {
        /// <summary>
        /// Compression level. The Default is `ZLibCompLevel.Default`.
        /// </summary>
        public ZLibCompLevel Level { get; set; } = ZLibCompLevel.Default;
        /// <summary>
        /// The base two logarithm of the window size (the size of the history buffer).  
        /// It should be in the range from 9 to 15. The default value is 15.
        /// Larger values of this parameter result in better compression at the expense of memory usage.
        /// </summary>
        /// <remarks>
        /// C library allows value of 8 but it have been prohibitted in here due to multiple issues.
        /// </remarks>
        public ZLibWindowBits WindowBits { get; set; } = ZLibWindowBits.Default;
        /// <summary>
        /// Specifies how much memory should be allocated for the internal compression state.
        /// 1 uses minimum memory but is slow and reduces compression ratio; 9 uses maximum memory for optimal speed.
        /// The default value is 8.
        /// </summary>
        public ZLibMemLevel MemLevel { get; set; } = ZLibMemLevel.Default;
        /// <summary>
        /// The number of threads to use for parallel compression.
        /// </summary>
        public int Threads { get; set; } = 1;
        /// <summary>
        /// Size of the compress block, which would be a unit of data to be compressed.
        /// </summary>
        public int BlockSize { get; set; } = DeflateParallelCompressStream.DefaultBlockSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the zlib stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
        /// <summary>
        /// Buffer pool to use for internal buffers.
        /// </summary>
        public ArrayPool<byte>? BufferPool { get; set; } = ArrayPool<byte>.Shared;
    }

    public sealed class ZLibDecompressOptions
    {
        /// <summary>
        /// The base two logarithm of the window size (the size of the history buffer).  
        /// It should be in the range from 9 to 15. The default value is 15.
        /// WindowBits must be greater than or equal to the value provided when the stream was compressed, or the decompress will fail.
        /// </summary>
        /// <remarks>
        /// For maximum compatibility, using ZLibWindowBits.Default (15) is recommended.
        /// </remarks>
        public ZLibWindowBits WindowBits { get; set; } = ZLibWindowBits.Default;
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = DeflateStreamBase.DefaultBufferSize;
         /// <summary>
        /// Whether to leave the base stream object open after disposing the zlib stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
        /// <summary>
        /// Buffer pool to use for internal buffer.
        /// </summary>
        public ArrayPool<byte>? BufferPool { get; set; } = ArrayPool<byte>.Shared;
    }
    #endregion

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
        public long TotalIn
        {
            get
            {
                if (_singleThreadStream != null)
                    return _singleThreadStream.TotalIn;
                if (_parallelCompressStream != null)
                    return _parallelCompressStream.TotalIn;
                throw new ObjectDisposedException("This stream had been disposed.");
            }
        }
        public long TotalOut
        {
            get
            {
                if (_singleThreadStream != null)
                    return _singleThreadStream.TotalOut;
                if (_parallelCompressStream != null)
                    return _parallelCompressStream.TotalOut;
                throw new ObjectDisposedException("This stream had been disposed.");
            }
        }

        // Singlethread Compress/Decompress
        private DeflateSingleThreadStream? _singleThreadStream = null;
        // Multithread Parallel Compress
        private DeflateParallelCompressStream? _parallelCompressStream = null;
        private Stream? _activeStream = null;

        /// <summary>
        /// Default buffer size for internal buffer, to be used in single-threaded operation.
        /// </summary>
        internal const int DefaultBufferSize = DeflateSingleThreadStream.DefaultBufferSize;
        /// <summary>
        /// Default block size for parallel compress operation.
        /// </summary>
        internal const int DefaultBlockSize = DeflateParallelCompressStream.DefaultBlockSize;
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing DeflateStream.
        /// </summary>
        public DeflateStreamBase(Stream baseStream, ZLibCompressOptions compOpts, ZLibOperateFormat format)
        {
            _singleThreadStream = new DeflateSingleThreadStream(baseStream, compOpts, format);
            _activeStream = _singleThreadStream;
        }

        public DeflateStreamBase(Stream baseStream, ZLibParallelCompressOptions pcompOpts, ZLibOperateFormat format)
        {
            _parallelCompressStream = new DeflateParallelCompressStream(baseStream, pcompOpts, format);
            _activeStream = _parallelCompressStream;
        }

        public DeflateStreamBase(Stream baseStream, ZLibDecompressOptions decompOpts, ZLibOperateFormat format)
        {
            _singleThreadStream = new DeflateSingleThreadStream(baseStream, decompOpts, format);
            _activeStream = _singleThreadStream;
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
                _activeStream = null;

                if (_singleThreadStream != null)
                {
                    _singleThreadStream.Dispose();
                    _singleThreadStream = null;
                }

                if (_parallelCompressStream != null)
                {
                    _parallelCompressStream.Dispose();
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
        {
            if (_activeStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");
            return _activeStream.Read(buffer, offset, count);
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
            if (_activeStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");
            _activeStream.Write(buffer, offset, count);
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
            if (_activeStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");
            _activeStream.Flush();
        }

        /// <inheritdoc />
        public override bool CanRead => _activeStream != null && _activeStream.CanRead;
        /// <inheritdoc />
        public override bool CanWrite => _activeStream != null && _activeStream.CanWrite;
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
        /// Create parallel-compressing DeflateStream.
        /// </summary>
        public DeflateStream(Stream baseStream, ZLibParallelCompressOptions pcompOpts)
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
        /// Create parallel-compressing ZLibStream.
        /// </summary>
        public ZLibStream(Stream baseStream, ZLibParallelCompressOptions pcompOpts)
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
        /// Create parallel-compressing GZipStream.
        /// </summary>
        public GZipStream(Stream baseStream, ZLibParallelCompressOptions pcompOpts)
            : base(baseStream, pcompOpts, ZLibOperateFormat.GZip) { }

        /// <summary>
        /// Create decompressing GZipStream.
        /// </summary>
        public GZipStream(Stream baseStream, ZLibDecompressOptions decompOpts)
            : base(baseStream, decompOpts, ZLibOperateFormat.GZip) { }
    }
    #endregion
}
