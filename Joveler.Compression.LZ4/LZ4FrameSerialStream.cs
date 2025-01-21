/*
    Derived from LZ4 header files (BSD 2-Clause)
    Copyright (c) 2011-2016, Yann Collet

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-present Hajin Jang

    Redistribution and use in source and binary forms, with or without modification,
    are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this
      list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice, this
      list of conditions and the following disclaimer in the documentation and/or
      other materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
    WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
    ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
    (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
    LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
    ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
    (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
    SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using Joveler.Compression.LZ4.Buffer;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Joveler.Compression.LZ4
{
    #region StreamOptions
    /// <summary>
    /// Compress options for LZ4FrameStream
    /// </summary>
    /// <remarks>
    /// Default value is based on default value of lz4 cli
    /// </remarks>
    public sealed class LZ4FrameCompressOptions
    {
        /// <summary>
        /// 0: default (fast mode); values > LZ4CompLevel.Level12 count as LZ4CompLevel.Level12; values < 0 trigger "fast acceleration"
        /// </summary>
        public LZ4CompLevel Level { get; set; } = LZ4CompLevel.Default;
        /// <summary>
        /// max64KB, max256KB, max1MB, max4MB
        /// </summary>
        public FrameBlockSizeId BlockSizeId { get; set; } = FrameBlockSizeId.Max4MB;
        /// <summary>
        /// LZ4F_blockLinked, LZ4F_blockIndependent
        /// </summary>
        public FrameBlockMode BlockMode { get; set; } = FrameBlockMode.BlockLinked;
        /// <summary>
        /// if enabled, frame is terminated with a 32-bits checksum of decompressed data
        /// </summary>
        public FrameContentChecksum ContentChecksumFlag { get; set; } = FrameContentChecksum.ContentChecksumEnabled;
        /// <summary>
        /// read-only field : LZ4F_frame or LZ4F_skippableFrame
        /// </summary>
        public FrameType FrameType { get; set; } = FrameType.Frame;
        /// <summary>
        /// if enabled, each block is followed by a checksum of block's compressed data
        /// </summary>
        public FrameBlockChecksum BlockChecksumFlag { get; set; } = FrameBlockChecksum.NoBlockChecksum;
        /// <summary>
        /// Size of uncompressed content ; 0 == unknown
        /// </summary>
        public ulong ContentSize { get; set; } = 0;
        /// <summary>
        /// 1 == always flush, to reduce usage of internal buffers
        /// </summary>
        public bool AutoFlush { get; set; } = false;
        /// <summary>
        /// 1 == parser favors decompression speed vs compression ratio.<br/>
        /// Only works for high compression modes (>= LZ4CompLevel.Level10)
        /// </summary>
        /// <remarks>
        /// v1.8.2+ 
        /// </remarks>
        public bool FavorDecSpeed { get; set; } = false;
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = LZ4FrameStream.DefaultBufferSize;
        /// <summary>
        /// Buffer pool to use for internal buffers.
        /// </summary>
        public ArrayPool<byte>? BufferPool { get; set; } = ArrayPool<byte>.Shared;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the lz4 stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
    }

    /// <summary>
    /// Decompress options for LZ4FrameStream
    /// </summary>
    public sealed class LZ4FrameDecompressOptions
    {
        /// <summary>
        /// disable checksum calculation and verification, even when one is present in frame, to save CPU time.
        /// Setting this option to 1 once disables all checksums for the rest of the frame.
        /// </summary>
        public bool SkipChecksums { get; set; } = false;
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = LZ4FrameStream.DefaultBufferSize;
        /// <summary>
        /// Buffer pool to use for internal buffers.
        /// </summary>
        public ArrayPool<byte>? BufferPool { get; set; } = ArrayPool<byte>.Shared;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the lz4 stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
    }
    #endregion

    #region LZ4FrameSerialStream
    internal sealed class LZ4FrameSerialStream : Stream
    {
        #region enum Mode
        private enum Mode
        {
            Compress,
            Decompress,
        }
        #endregion

        #region Fields and Properties
        // Field
        private readonly Mode _mode;
        private readonly bool _leaveOpen;
        private bool _disposed = false;

        private IntPtr _cctx = IntPtr.Zero;
        private IntPtr _dctx = IntPtr.Zero;

        private readonly int _bufferSize = DefaultBufferSize;
        private readonly ArrayPool<byte> _pool;
        private readonly PooledBuffer _workBuf;

        // Compression
        private readonly int _destBufSize;

        // Decompression
        private bool _firstRead = true;
        private int _decompSrcIdx = 0;
        private int _decompSrcCount = 0;

        // Property
        public Stream? BaseStream { get; private set; }
        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;

        // LZ4F_compressOptions_t, LZ4F_decompressOptions_t
        private FrameCompressOptions _compOpts = new FrameCompressOptions()
        {
            StableSrc = 0,
        };
        private FrameDecompressOptions _decompOpts = new FrameDecompressOptions()
        {
            StableDst = 0,
            SkipChecksums = 0,
        };

        // Const
        private const int DecompressComplete = -1;
        // https://github.com/lz4/lz4/blob/master/doc/lz4_Frame_format.md
        
        private static readonly byte[] FrameMagicNumber = { 0x04, 0x22, 0x4D, 0x18 }; // 0x184D2204 (LE)
        private static readonly byte[] FrameMagicSkippableStart = { 0x50, 0x2A, 0x4D, 0x18 }; // 0x184D2A50 (LE)
        /*
        private const int FrameSizeToKnowHeaderLength = 5;
        /// <summary>
        /// LZ4 Frame header size can vary, depending on selected paramaters
        /// </summary>
        private const int FrameHeaderSizeMin = 7;
        private const int FrameHeaderSizeMax = 19;
        */

        // Default Buffer Size
        /* Benchmark - 1MB is the fastest, due to less pinvoke overhead
           LZ4 is a fast algorithm, so pinvoke overhead impact is critical.
        AMD Ryzen 5 3600 / .NET Core 3.1.13 / Windows 10.0.19042 x64 / lz4 1.9.2
        | Method | BufferSize |        Mean |     Error |    StdDev |
        |------- |----------- |------------:|----------:|----------:|
        |    LZ4 |       4096 |  1,016.2 us |  19.22 us |  19.74 us |
        |    LZ4 |      16384 |    970.4 us |  19.28 us |  36.69 us |
        |    LZ4 |      65536 |    911.6 us |   7.72 us |  12.46 us |
        |    LZ4 |     262144 |    946.9 us |   4.01 us |   3.35 us |
        |    LZ4 |    1048576 |    637.4 us |  12.55 us |  22.95 us |
        |    LZ4 |    4194304 |    904.2 us |   4.15 us |   3.88 us |
         */
        internal const int DefaultBufferSize = 1024 * 1024;
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing LZ4FrameStream.
        /// </summary>
        public unsafe LZ4FrameSerialStream(Stream baseStream, LZ4FrameCompressOptions compOpts)
        {
            LZ4Init.Manager.EnsureLoaded();

            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Compress;
            _disposed = false;

            // Check and set compress options
            _pool = compOpts.BufferPool ?? ArrayPool<byte>.Shared;
            _leaveOpen = compOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(compOpts.BufferSize);

            // Prepare cctx
            nuint ret = LZ4Init.Lib.CreateFrameCompressContext!(ref _cctx, LZ4FrameStream.FrameVersion);
            LZ4FrameException.CheckReturnValue(ret);

            // Prepare FramePreferences
            FramePreferences prefs = new FramePreferences
            {
                FrameInfo = new FrameInfo(compOpts.BlockSizeId, compOpts.BlockMode, FrameContentChecksum.NoContentChecksum,
                    compOpts.FrameType, compOpts.ContentSize, 0, compOpts.BlockChecksumFlag),
                CompressionLevel = compOpts.Level,
                AutoFlush = compOpts.AutoFlush ? 1u : 0u,
                FavorDecSpeed = compOpts.FavorDecSpeed ? 1u : 0u,
            };

            // Query the minimum required size of compress buffer
            // _bufferSize is the source size, frameSize is the (required) dest size
            nuint frameSizeVal = LZ4Init.Lib.FrameCompressBound!((nuint)_bufferSize, prefs);
            Debug.Assert(frameSizeVal <= int.MaxValue);
            uint frameSize = (uint)frameSizeVal;

            _destBufSize = _bufferSize;
            if (_bufferSize < frameSize)
                _destBufSize = (int)frameSize;
            _workBuf = new PooledBuffer(_pool, _destBufSize);

            // Write the frame header into _workBuf
            nuint headerSizeVal;
            fixed (byte* dest = _workBuf.Buf)
            {
                headerSizeVal = LZ4Init.Lib.FrameCompressBegin!(_cctx, dest, (nuint)_destBufSize, prefs);
            }
            LZ4FrameException.CheckReturnValue(headerSizeVal);
            Debug.Assert(0 <= headerSizeVal && headerSizeVal < int.MaxValue);

            int headerSize = (int)headerSizeVal;
            BaseStream.Write(_workBuf.Buf, 0, headerSize);
            TotalOut += headerSize;
        }

        /// <summary>
        /// Create decompressing LZ4FrameStream.
        /// </summary>
        public unsafe LZ4FrameSerialStream(Stream baseStream, LZ4FrameDecompressOptions decompOpts)
        {
            LZ4Init.Manager.EnsureLoaded();

            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Decompress;
            _disposed = false;

            // Check and set compress options
            _pool = decompOpts.BufferPool ?? ArrayPool<byte>.Shared;
            _leaveOpen = decompOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(decompOpts.BufferSize);

            // Prepare dctx
            nuint ret = LZ4Init.Lib.CreateFrameDecompressContext!(ref _dctx, LZ4FrameStream.FrameVersion);
            LZ4FrameException.CheckReturnValue(ret);

            // Prepare LZ4F_decompressOptions_t*
            if (decompOpts.SkipChecksums)
                _decompOpts.SkipChecksums = 1;

            // Remove LZ4 frame header from the baseStream
            byte[] headerBuf = new byte[4];
            int readHeaderSize = BaseStream.Read(headerBuf, 0, 4);
            TotalIn += 4;

            if (readHeaderSize != 4 || !headerBuf.SequenceEqual(FrameMagicNumber))
                throw new InvalidDataException("BaseStream is not a valid LZ4 Frame Format");

            // Prepare a work buffer
            _workBuf = new PooledBuffer(_pool, _bufferSize);
        }
        #endregion

        #region Disposable Pattern
        ~LZ4FrameSerialStream()
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

                if (LZ4Init.Lib == null)
                    throw new ObjectDisposedException(nameof(LZ4Init));

                Flush();

                // Dispose unmanaged resources, and set large fields to null.
                if (_cctx != IntPtr.Zero)
                { // Compress
                    FinishWrite();

                    nuint ret = LZ4Init.Lib.FreeFrameCompressContext!(_cctx);
                    LZ4FrameException.CheckReturnValue(ret);

                    _cctx = IntPtr.Zero;
                }

                if (_dctx != IntPtr.Zero)
                {
                    nuint ret = LZ4Init.Lib.FreeFrameDecompressContext!(_dctx);
                    LZ4FrameException.CheckReturnValue(ret);

                    _dctx = IntPtr.Zero;
                }

                if (BaseStream != null)
                {
                    if (!_leaveOpen)
                        BaseStream.Dispose();
                    BaseStream = null;
                }

                if (!_workBuf.Disposed)
                    _workBuf.Dispose();

                _disposed = true;
            }
        }
        #endregion

        #region Stream Methods
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_mode != Mode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));
            CheckReadWriteArgs(buffer, offset, count);
            if (count == 0)
                return 0;

            Span<byte> span = buffer.AsSpan(offset, count);
            return Read(span);
        }

        /// <inheritdoc />
#if NETCOREAPP
        public override unsafe int Read(Span<byte> span)
#else
        public unsafe int Read(Span<byte> span)
#endif
        {
            if (_mode != Mode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));
            if (BaseStream == null)
                throw new ObjectDisposedException(nameof(LZ4FrameStream));

            // Reached end of stream
            if (_decompSrcIdx == DecompressComplete)
                return 0;

            int readSize = 0;
            int destSize = span.Length;
            int destLeftBytes = span.Length;

            if (_firstRead)
            {
                // Write FrameMagicNumber into LZ4F_decompress
                nuint headerSizeVal = 4;
                nuint destSizeVal = (nuint)destSize;

                nuint ret;
                fixed (byte* header = FrameMagicNumber)
                fixed (byte* dest = span)
                {
                    ret = LZ4Init.Lib.FrameDecompress!(_dctx, dest, ref destSizeVal, header, ref headerSizeVal, _decompOpts);
                }
                LZ4FrameException.CheckReturnValue(ret);

                Debug.Assert(headerSizeVal <= int.MaxValue);
                Debug.Assert(destSizeVal <= int.MaxValue);

                if (headerSizeVal != 4u)
                    throw new InvalidOperationException("Not enough dest buffer");
                int destWritten = (int)destSizeVal;

                span = span.Slice(destWritten);
                TotalOut += destWritten;

                _firstRead = false;
            }

            while (0 < destLeftBytes)
            {
                if (_decompSrcIdx == _decompSrcCount)
                {
                    // Read from _baseStream
                    _decompSrcIdx = 0;
                    _decompSrcCount = BaseStream.Read(_workBuf.Buf, 0, _workBuf.Capacity);
                    TotalIn += _decompSrcCount;

                    // _baseStream reached its end
                    if (_decompSrcCount == 0)
                    {
                        _decompSrcIdx = DecompressComplete;
                        break;
                    }
                }

                nuint srcSizeVal = (nuint)(_decompSrcCount - _decompSrcIdx);
                nuint destSizeVal = (nuint)(destLeftBytes);

                nuint ret;
                fixed (byte* src = _workBuf.Span.Slice(_decompSrcIdx))
                fixed (byte* dest = span)
                {
                    ret = LZ4Init.Lib.FrameDecompress!(_dctx, dest, ref destSizeVal, src, ref srcSizeVal, _decompOpts);
                }
                LZ4FrameException.CheckReturnValue(ret);

                // The number of bytes consumed from srcBuffer will be written into *srcSizePtr (necessarily <= original value).
                Debug.Assert(srcSizeVal <= int.MaxValue);
                int srcConsumed = (int)srcSizeVal;
                _decompSrcIdx += srcConsumed;
                Debug.Assert(_decompSrcIdx <= _decompSrcCount);

                // The number of bytes decompressed into dstBuffer will be written into *dstSizePtr (necessarily <= original value).
                Debug.Assert(destSizeVal <= int.MaxValue);
                int destWritten = (int)destSizeVal;

                span = span.Slice(destWritten);
                destLeftBytes -= destWritten;
                TotalOut += destWritten;
                readSize += destWritten;
            }

            return readSize;
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_mode != Mode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");
            CheckReadWriteArgs(buffer, offset, count);
            if (count == 0)
                return;

            ReadOnlySpan<byte> span = buffer.AsSpan(offset, count);
            Write(span);
        }

        /// <inheritdoc />
#if NETCOREAPP
        public override unsafe void Write(ReadOnlySpan<byte> span)
#else
        public unsafe void Write(ReadOnlySpan<byte> span)
#endif
        {
            if (_mode != Mode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));
            if (BaseStream == null)
                throw new ObjectDisposedException(nameof(LZ4FrameStream));

            int inputSize = span.Length;

            while (0 < span.Length)
            {
                int srcWorkSize = _bufferSize < span.Length ? _bufferSize : span.Length;

                nuint outSizeVal;
                fixed (byte* dest = _workBuf.Buf)
                fixed (byte* src = span)
                {
                    outSizeVal = LZ4Init.Lib.FrameCompressUpdate!(_cctx, dest, (nuint)_destBufSize, src, (nuint)srcWorkSize, _compOpts);
                }

                LZ4FrameException.CheckReturnValue(outSizeVal);

                Debug.Assert(outSizeVal < int.MaxValue, "BufferSize should be <2GB");
                int outSize = (int)outSizeVal;

                BaseStream.Write(_workBuf.Buf, 0, outSize);
                TotalOut += outSize;

                span = span.Slice(srcWorkSize);
            }

            TotalIn += inputSize;
        }

        private unsafe void FinishWrite()
        {
            Debug.Assert(_mode == Mode.Compress, "FinishWrite() cannot be called in decompression");
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));
            if (BaseStream == null)
                throw new ObjectDisposedException(nameof(LZ4FrameStream));

            nuint outSizeVal;
            fixed (byte* dest = _workBuf.Buf)
            {
                outSizeVal = LZ4Init.Lib.FrameCompressEnd!(_cctx, dest, (nuint)_destBufSize, _compOpts);
            }
            LZ4FrameException.CheckReturnValue(outSizeVal);

            Debug.Assert(outSizeVal <= int.MaxValue, "BufferSize should be <=2GB");
            int outSize = (int)outSizeVal;

            BaseStream.Write(_workBuf.Buf, 0, outSize);
            TotalOut += outSize;
        }

        /// <inheritdoc />
        public override unsafe void Flush()
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));
            if (BaseStream == null)
                throw new ObjectDisposedException(nameof(LZ4FrameStream));

            if (_mode == Mode.Compress)
            {
                nuint outSizeVal = 0;
                do
                {
                    fixed (byte* dest = _workBuf.Buf)
                    {
                        outSizeVal = LZ4Init.Lib.FrameFlush!(_cctx, dest, (nuint)_destBufSize, _compOpts);
                    }
                    LZ4FrameException.CheckReturnValue(outSizeVal);

                    Debug.Assert(outSizeVal <= int.MaxValue, "BufferSize should be <=2GB");
                    int outSize = (int)outSizeVal;

                    if (0 < outSize)
                        BaseStream.Write(_workBuf.Buf, 0, outSize);
                    TotalOut += outSize;
                }
                while (0 < outSizeVal);
            }

            BaseStream.Flush();
        }

        /// <inheritdoc />
        public override bool CanRead => _mode == Mode.Decompress && BaseStream != null && BaseStream.CanRead;
        /// <inheritdoc />
        public override bool CanWrite => _mode == Mode.Compress && BaseStream != null && BaseStream.CanWrite;
        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seek() not supported");
        }
        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException("SetLength not supported");
        }
        /// <inheritdoc />
        public override long Length => throw new NotSupportedException("Length not supported");
        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException("Position not supported");
            set => throw new NotSupportedException("Position not supported");
        }

        public double CompressionRatio
        {
            get
            {
                switch (_mode)
                {
                    case Mode.Compress:
                        if (TotalIn == 0)
                            return 0;
                        return 100 - TotalOut * 100.0 / TotalIn;
                    case Mode.Decompress:
                        if (TotalOut == 0)
                            return 0;
                        return 100 - TotalIn * 100.0 / TotalOut;
                    default:
                        throw new InvalidOperationException($"Internal Logic Error at {nameof(LZ4FrameStream)}.{nameof(CompressionRatio)}");
                }
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
            return Math.Max(bufferSize, 64 * 1024);
        }
        #endregion
    }
    #endregion
}
