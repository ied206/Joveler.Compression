/*
    Derived from Zstandard header files (BSD 2-Clause)
    Copyright (c) 2016-present, Yann Collet, Facebook, Inc. All rights reserved.

    C# Wrapper written by Hajin Jang
    Copyright (C) 2020-2021 Hajin Jang

    Redistribution and use in source and binary forms, with or without modification,
    are permitted provided that the following conditions are met:

     * Redistributions of source code must retain the above copyright notice, this
       list of conditions and the following disclaimer.

     * Redistributions in binary form must reproduce the above copyright notice,
       this list of conditions and the following disclaimer in the documentation
       and/or other materials provided with the distribution.

     * Neither the name Facebook nor the names of its contributors may be used to
       endorse or promote products derived from this software without specific
       prior written permission.

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

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Joveler.Compression.Zstd
{
    #region StreamOptions
    /// <summary>
    /// Compress options for ZstdStream
    /// </summary>
    /// <remarks>
    /// Default value is based on default value of lz4 cli
    /// </remarks>
    public class ZstdCompressOptions
    {
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = ZstdFrameStream.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the zstd stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
        /// <summary>
        /// Compression level of <cref>ZstdFrameStream</cref>. 
        /// Default level is 0, which is controlled by <cref>ZstdFrameStream.CLevelDefault</cref>.
        /// Refer to <cref>ZstdFrameStream.MinCompressionLevel()</cref> and <cref>ZstdFrameStream.MaxCompressionLevel()</cref> for bounds.
        /// </summary>
        public int CompressionLevel { get; set; } = 0;

        #region Advanced compression parameters
        /// <summary>
        /// largest match distance : larger == more compression, more memory needed during decompression
        /// </summary>
        public uint WindowLog;
        /// <summary>
        /// fully searched segment : larger == more compression, slower, more memory (useless for fast)
        /// </summary>
        public uint ChainLog;
        /// <summary>
        /// dispatch table : larger == faster, more memory
        /// </summary>
        public uint HashLog;
        /// <summary>
        /// nb of searches : larger == more compression, slower
        /// </summary>
        public uint SearchLog;
        /// <summary>
        /// match length searched : larger == faster decompression, sometimes less compression
        /// </summary>
        public uint MinMatch;
        /// <summary>
        /// acceptable match size for optimal parser (only) : larger == more compression, slower
        /// </summary>
        public uint TargetLength;
        /// <summary>
        /// see ZSTD_strategy definition above
        /// </summary>
        public Strategy Strategy;
        #endregion

        // TODO: LDM mode parameters

        #region Frame parameters
        /// <summary>
        /// Content size will be written into frame header _whenever known_ (default:1)
        /// Content size must be known at the beginning of compression.
        /// This is automatically the case when using ZSTD_compress2(),
        /// For streaming scenarios, content size must be provided with ZSTD_CCtx_setPledgedSrcSize()
        /// </summary>
        public int ContentSizeFlag { get; set; } = 1;
        /// <summary>
        /// A 32-bits checksum of content is written at end of frame (default:0)
        /// </summary>
        public int ChecksumFlag { get; set; } = 0;
        /// <summary>
        /// When applicable, dictionary's ID is written into frame header (default:1)
        /// </summary>
        public int DictIdFlag { get; set; } = 1;
        #endregion

        #region Multi-threading parameters
        /// <summary>
        /// Select how many threads will be spawned to compress in parallel.
        /// When nbWorkers >= 1, triggers asynchronous mode while compression work is performed in parallel, within worker threads.
        /// More workers improve speed, but also increase memory usage.
        /// Default value is `0`, aka "single-threaded mode" : no worker is spawned, compression is performed inside Caller's thread, all invocations are blocking
        /// </summary>
        public int MTWorkers { get; set; } = 0;
        /// <summary>
        /// (Multi-thread only)
        /// Size of a compression job. 
        /// Each compression job is completed in parallel, so this value can indirectly impact the nb of active threads.
        /// 0 means default, which is dynamically determined based on compression parameters.
        /// Job size must be a minimum of overlap size, or 1 MB, whichever is largest.
        /// The minimum size is automatically and transparently enforced.
        /// </summary>
        public int MTJobSize { get; set; } = 0;
        /// <summary>
        /// (Multi-thread only)
        /// Control the overlap size, as a fraction of window size.
        /// The overlap size is an amount of data reloaded from previous job at the beginning of a new job.
        /// It helps preserve compression ratio, while each job is compressed in parallel.
        /// Larger values increase compression ratio, but decrease speed.
        /// Possible values range from 0 to 9 :
        /// - 0 means "default" : value will be determined by the library, depending on strategy
        /// - 1 means "no overlap"
        /// - 9 means "full overlap", using a full window size.
        /// Each intermediate rank increases/decreases load size by a factor 2 :
        /// 9: full window;  8: w/2;  7: w/4;  6: w/8;  5:w/16;  4: w/32;  3:w/64;  2:w/128;  1:no overlap;  0:default
        /// default value varies between 6 and 9, depending on strategy
        /// </summary>
        public int MTOverlapLog { get; set; } = 0;
        #endregion
    }

    /// <summary>
    /// Decompress options for ZstdStream
    /// </summary>
    public class ZstdDecompressOptions
    {
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = ZstdFrameStream.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the zstd stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
    }
    #endregion

    #region ZstdStream
    // ReSharper disable once InconsistentNaming
    public unsafe class ZstdFrameStream : Stream
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

        private IntPtr _cstream = IntPtr.Zero;
        private IntPtr _dstream = IntPtr.Zero;

        private readonly int _bufferSize = DefaultBufferSize;
        private readonly byte[] _srcBuf;
        private readonly uint _srcBufSize;
        private readonly byte[] _destBuf;
        private readonly uint _destBufSize;

        // Compression

        // Decompression
        private bool _firstRead = true;
        private int _decompSrcIdx = 0;
        private int _decompSrcCount = 0;

        // Property
        public Stream BaseStream { get; private set; }
        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;

        // Const
        private const int DecompressComplete = -1;
        //internal const uint FrameVersion = 100;
        internal const int DefaultBufferSize = 1024 * 1024;
        public const int CLevelDefault = 3;
        private const uint BlocksizeMax = 1 << 17;
        private static readonly byte[] FrameMagicNumber = { 0x28, 0xB5, 0x2F, 0xFD }; // 0xFD2FB528 (LE), valid since v0.8.0
        private static readonly byte[] MagicDictionary = { 0x37, 0xA4, 0x30, 0xEC }; // 0xEC30A437 (LE), valid since v0.7.0
        private static readonly byte[] MagicSkippableStart = { 0x50, 0x2A, 0x4D, 0x18 }; // 0x184D2A50 (LE), all 16 values, from 0x184D2A50 to 0x184D2A5F, signal the beginning of a skippable frame
        private static readonly byte[] MagicSkippableMask = { 0xF0, 0xFF, 0xFF, 0xFF };
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing ZstdFrameStream.
        /// </summary>
        public unsafe ZstdFrameStream(Stream baseStream, ZstdCompressOptions compOpts)
        {
            ZstdInit.Manager.EnsureLoaded();

            // Check arguments
            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            if (compOpts.MTWorkers < 0)
                throw new ArgumentException($"{nameof(ZstdCompressOptions)}.{nameof(ZstdCompressOptions.MTWorkers)} [{compOpts.MTWorkers}] must be equal or larger than 0");

            _mode = Mode.Compress;
            _disposed = false;
            _leaveOpen = compOpts.LeaveOpen;

            // Get recommended size for input buffer.
            UIntPtr srcBufSize = ZstdInit.Lib.CStreamInSize(); 
            if (uint.MaxValue < srcBufSize.ToUInt64())
                _srcBufSize = uint.MaxValue;
            else
                _srcBufSize = srcBufSize.ToUInt32();
            _srcBuf = new byte[_srcBufSize];

            // Get recommended size for output buffer. Guarantee to successfully flush at least one complete compressed block.
            UIntPtr destBufSize = ZstdInit.Lib.CStreamOutSize();
            if (uint.MaxValue < destBufSize.ToUInt64())
                _destBufSize = uint.MaxValue;
            else
                _destBufSize = destBufSize.ToUInt32();
            _destBuf = new byte[_destBufSize];

            // Allocate resources (Based on FIO_createCResources())
            // Check and set compress options
            _leaveOpen = compOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(compOpts.BufferSize);

            // Prepare cctx
            _cstream = ZstdInit.Lib.CreateCStream();
            if (_cstream == IntPtr.Zero)
            {
                // Unable to create cctx
                throw new InvalidOperationException("allocation error: cannot create ZSTD_CCtx");
            }

            // Set advanced compression parameters
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.CompressionLevel, compOpts.CompressionLevel));
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.WindowLog, (int)compOpts.WindowLog));
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.ChainLog, (int)compOpts.ChainLog));
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.HashLog, (int)compOpts.HashLog));
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.SearchLog, (int)compOpts.SearchLog));
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.MinMatch, (int)compOpts.MinMatch));
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.TargetLength, (int)compOpts.TargetLength));
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.Strategy, (int)compOpts.Strategy));

            // Set frame parameters
            // TODO: ContentSizeFlag
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.ChecksumFlag, (int)compOpts.ChecksumFlag));
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.DictIdFlag, (int)compOpts.DictIdFlag));

            // Set multithread parameters
            if (0 < compOpts.MTWorkers)
            {
                ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.NbWorkers, compOpts.MTWorkers));
                ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.JobSize, compOpts.MTJobSize));
                ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.OverlapLog, compOpts.MTOverlapLog));
            }

            // TODO: Dictionary parameters

            // TODO: Use ZSTD_CCtx_setPledgedSrcSize? -> Not required



            // Prepare FramePreferences
            /*
            FramePreferences prefs = new FramePreferences
            {
                FrameInfo = new FrameInfo
                {
                    BlockSizeId = compOpts.BlockSizeId,
                    BlockMode = compOpts.BlockMode,
                    ContentChecksumFlag = compOpts.ContentChecksumFlag,
                    FrameType = compOpts.FrameType,
                    ContentSize = compOpts.ContentSize,
                    DictId = 0,
                    BlockChecksumFlag = compOpts.BlockChecksumFlag,
                },
                CompressionLevel = compOpts.Level,
                AutoFlush = compOpts.AutoFlush ? 1u : 0u,
                FavorDecSpeed = compOpts.FavorDecSpeed ? 1u : 0u,
            };
            */

            // Query the minimum required size of compress buffer
            // _bufferSize is the source size, frameSize is the (required) dest size
            /*
            UIntPtr frameSizeVal = LZ4Init.Lib.FrameCompressBound((UIntPtr)_bufferSize, prefs);
            Debug.Assert(frameSizeVal.ToUInt64() <= int.MaxValue);
            uint frameSize = frameSizeVal.ToUInt32();
            */
            //if (_bufferSize < frameSize)
            //    _destBufSize = frameSize;



            /*
            _destBufSize = (uint)_bufferSize;
            if (_bufferSize < frameSize)
                _destBufSize = frameSize;
            _workBuf = new byte[_destBufSize];

            _destBufSize = 1024 * 1024;


                        _destBufSize = 1024 * 1024;
            _workBuf = new byte[_destBufSize];

            // Write the frame header into _workBuf
            UIntPtr headerSizeVal;
            fixed (byte* dest = _workBuf)
            {
                headerSizeVal = ZstdLoader.Lib.FrameCompressBegin(_cstream, dest, (UIntPtr)_bufferSize, prefs);
            }
            LZ4FrameException.CheckReturnValue(headerSizeVal);
            Debug.Assert(headerSizeVal.ToUInt64() < int.MaxValue);

            int headerSize = (int)headerSizeVal.ToUInt32();
            BaseStream.Write(_workBuf, 0, headerSize);
            TotalOut += headerSize;
            */
        }

        /// <summary>
        /// Create decompressing ZstdFrameStream.
        /// </summary>
        public unsafe ZstdFrameStream(Stream baseStream, ZstdDecompressOptions compOpts)
        {
            ZstdInit.Manager.EnsureLoaded();

            // Check arguments
            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Decompress;
            _disposed = false;

            // Check and set compress options
            _leaveOpen = compOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(compOpts.BufferSize);

            // From FIO_createDResources

            // Prepare dctx
            UIntPtr ret = LZ4Init.Lib.CreateFrameDecompressContext(ref _dctx, FrameVersion);
            LZ4FrameException.CheckReturnValue(ret);

            // Remove LZ4 frame header from the baseStream
            byte[] headerBuf = new byte[4];
            int readHeaderSize = BaseStream.Read(headerBuf, 0, 4);
            TotalIn += 4;

            if (readHeaderSize != 4 || !headerBuf.SequenceEqual(FrameMagicNumber))
                throw new InvalidDataException("BaseStream is not a valid LZ4 Frame Format");

            // Prepare a work buffer
            _workBuf = new byte[_bufferSize];
        }
        #endregion

        #region Disposable Pattern
        ~ZstdFrameStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                /*
                if (_cctx != IntPtr.Zero)
                { // Compress
                    FinishWrite();

                    UIntPtr ret = LZ4Init.Lib.FreeFrameCompressContext(_cctx);
                    LZ4FrameException.CheckReturnValue(ret);

                    _cctx = IntPtr.Zero;
                }

                if (_dctx != IntPtr.Zero)
                {
                    UIntPtr ret = LZ4Init.Lib.FreeFrameDecompressContext(_dctx);
                    LZ4FrameException.CheckReturnValue(ret);

                    _dctx = IntPtr.Zero;

                }

                if (BaseStream != null)
                {
                    Flush();
                    if (!_leaveOpen)
                        BaseStream.Dispose();
                    BaseStream = null;
                }

                _disposed = true;
                */
            }
        }
        #endregion

        #region Stream Methods
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_mode != Mode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");
            CheckReadWriteArgs(buffer, offset, count);
            if (count == 0)
                return 0;

            Span<byte> span = buffer.AsSpan(offset, count);
            return Read(span);
        }

        /// <inheritdoc />
#if NETSTANDARD2_1
        public override unsafe int Read(Span<byte> span)
#else
        public unsafe int Read(Span<byte> span)
#endif
        {
            if (_mode != Mode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");

            // Reached end of stream
            if (_decompSrcIdx == DecompressComplete)
                return 0;

            int readSize = 0;
            int destSize = span.Length;
            int destLeftBytes = span.Length;

            /*
            if (_firstRead)
            {
                // Write FrameMagicNumber into LZ4F_decompress
                UIntPtr headerSizeVal = (UIntPtr)4;
                UIntPtr destSizeVal = (UIntPtr)destSize;

                UIntPtr ret;
                fixed (byte* header = FrameMagicNumber)
                fixed (byte* dest = span)
                {
                    ret = LZ4Init.Lib.FrameDecompress(_dctx, dest, ref destSizeVal, header, ref headerSizeVal, null);
                }
                LZ4FrameException.CheckReturnValue(ret);

                Debug.Assert(headerSizeVal.ToUInt64() <= int.MaxValue);
                Debug.Assert(destSizeVal.ToUInt64() <= int.MaxValue);

                if (headerSizeVal.ToUInt32() != 4u)
                    throw new InvalidOperationException("Not enough dest buffer");
                int destWritten = (int)destSizeVal.ToUInt32();

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
                    _decompSrcCount = BaseStream.Read(_workBuf, 0, _workBuf.Length);
                    TotalIn += _decompSrcCount;

                    // _baseStream reached its end
                    if (_decompSrcCount == 0)
                    {
                        _decompSrcIdx = DecompressComplete;
                        break;
                    }
                }

                UIntPtr srcSizeVal = (UIntPtr)(_decompSrcCount - _decompSrcIdx);
                UIntPtr destSizeVal = (UIntPtr)(destLeftBytes);

                UIntPtr ret;
                fixed (byte* src = _workBuf.AsSpan(_decompSrcIdx))
                fixed (byte* dest = span)
                {
                    ret = LZ4Init.Lib.FrameDecompress(_dctx, dest, ref destSizeVal, src, ref srcSizeVal, null);
                }
                LZ4FrameException.CheckReturnValue(ret);

                // The number of bytes consumed from srcBuffer will be written into *srcSizePtr (necessarily <= original value).
                Debug.Assert(srcSizeVal.ToUInt64() <= int.MaxValue);
                int srcConsumed = (int)srcSizeVal.ToUInt32();
                _decompSrcIdx += srcConsumed;
                Debug.Assert(_decompSrcIdx <= _decompSrcCount);

                // The number of bytes decompressed into dstBuffer will be written into *dstSizePtr (necessarily <= original value).
                Debug.Assert(destSizeVal.ToUInt64() <= int.MaxValue);
                int destWritten = (int)destSizeVal.ToUInt32();

                span = span.Slice(destWritten);
                destLeftBytes -= destWritten;
                TotalOut += destWritten;
                readSize += destWritten;
            }
            */
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
#if NETSTANDARD2_1
        public override unsafe void Write(ReadOnlySpan<byte> span)
#else
        public unsafe void Write(ReadOnlySpan<byte> span)
#endif
        {
            if (_mode != Mode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");

            // Based on FIO_compressZstdFrame()
            // Main compression loop
            EndDirective directive = EndDirective.Continue;
            do
            {
                fixed (byte* src = span)
                fixed (byte* dest = _destBuf)
                {
                    InBuffer inBuff = new InBuffer()
                    {
                        Src = src,
                        Size = (UIntPtr)span.Length,
                        Pos = (UIntPtr)0,
                    };

                    TotalIn += span.Length;

                    if (span.Length == 0)
                        directive = EndDirective.End;

                    UIntPtr stillToFlush = (UIntPtr)1;
                    while ((inBuff.Pos != inBuff.Size) || // input buffer must be entirely ingested
                           (directive == EndDirective.End && stillToFlush != (UIntPtr)0))
                    {
                        UIntPtr oldInPos = inBuff.Pos;
                        OutBuffer outBuff = new OutBuffer()
                        {
                            Dst = dest,
                            Size = (UIntPtr)_destBufSize,
                            Pos = (UIntPtr)0,
                        };

                        UIntPtr toFlushNow = ZstdInit.Lib.ToFlushNow(_cstream);
                        stillToFlush = ZstdInit.Lib.CompressionStream2(_cstream, outBuff, inBuff, directive);
                        ZstdException.CheckReturnValue(stillToFlush);

                        // Write compressed stream
                        Debug.Assert(outBuff.Pos.ToUInt64() < int.MaxValue, "OutBufferPos should be <2GB");
                        int outBufPos = (int)outBuff.Pos;
                        Debug.Assert(outBuff.Size.ToUInt64() < int.MaxValue, "OutBufferSize should be <2GB");
                        int outBufSize = (int)outBuff.Size;
                        if (outBufPos != 0)
                        {
                            BaseStream.Write(_destBuf, outBufPos, outBufSize);
                            TotalOut += outBufPos;
                        }
                    }
                }
            }
            while (directive != EndDirective.End);

            /*
            // Based on FIO_compressZstdFrame()
            // Main compression loop
            EndDirective directive = EndDirective.Continue;
            do
            {
                fixed (byte* src = span)
                fixed (byte* dest = _destBuf)
                {
                    InBuffer inBuff = new InBuffer()
                    {
                        Src = src,
                        Size = (UIntPtr)span.Length,
                        Pos = (UIntPtr)0,
                    };

                    TotalIn += span.Length;

                    if (span.Length == 0)
                        directive = EndDirective.End;

                    UIntPtr stillToFlush = (UIntPtr)1;
                    while ((inBuff.Pos != inBuff.Size) || // input buffer must be entirely ingested
                           (directive == EndDirective.End && stillToFlush != (UIntPtr)0))
                    {
                        UIntPtr oldInPos = inBuff.Pos;
                        OutBuffer outBuff = new OutBuffer()
                        {
                            Dst = dest,
                            Size = (UIntPtr)_destBufSize,
                            Pos = (UIntPtr)0,
                        };

                        UIntPtr toFlushNow = ZstdInit.Lib.ToFlushNow(_cstream);
                        stillToFlush = ZstdInit.Lib.CompressionStream2(_cstream, outBuff, inBuff, directive);
                        ZstdException.CheckReturnValue(stillToFlush);

                        // Write compressed stream
                        Debug.Assert(outBuff.Pos.ToUInt64() < int.MaxValue, "OutBufferPos should be <2GB");
                        int outBufPos = (int)outBuff.Pos;
                        Debug.Assert(outBuff.Size.ToUInt64() < int.MaxValue, "OutBufferSize should be <2GB");
                        int outBufSize = (int)outBuff.Size;
                        if (outBufPos != 0)
                        {
                            BaseStream.Write(_destBuf, outBufPos, outBufSize);
                            TotalOut += outBufPos;
                        }
                    }
                }
            }
            while (directive != EndDirective.End);
            */
        }

        private unsafe void FinishWrite()
        {
            /*
            Debug.Assert(_mode == Mode.Compress, "FinishWrite() cannot be called in decompression");

            UIntPtr outSizeVal;
            fixed (byte* dest = _workBuf)
            {
                outSizeVal = LZ4Init.Lib.FrameCompressEnd(_cctx, dest, (UIntPtr)_destBufSize, null);
            }
            LZ4FrameException.CheckReturnValue(outSizeVal);

            Debug.Assert(outSizeVal.ToUInt64() < int.MaxValue, "BufferSize should be <2GB");
            int outSize = (int)outSizeVal.ToUInt64();

            BaseStream.Write(_workBuf, 0, outSize);
            TotalOut += outSize;
            */
        }

        /// <inheritdoc />
        public override void Flush()
        {
            BaseStream.Flush();
        }

        /// <inheritdoc />
        public override bool CanRead => _mode == Mode.Decompress && BaseStream.CanRead;
        /// <inheritdoc />
        public override bool CanWrite => _mode == Mode.Compress && BaseStream.CanWrite;
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
                        throw new InvalidOperationException($"Internal Logic Error at {nameof(ZstdFrameStream)}.{nameof(CompressionRatio)}");
                }
            }
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// minimum negative compression level allowed, requires v1.4.0+
        /// </summary>
        /// <returns></returns>
        public static int MinCompressionLevel()
        {
            ZstdInit.Manager.EnsureLoaded();

            return ZstdInit.Lib.MinCLevel();
        }

        /// <summary>
        /// maximum compression level available
        /// </summary>
        /// <returns></returns>
        public static int MaxCompressionLevel()
        {
            ZstdInit.Manager.EnsureLoaded();

            return ZstdInit.Lib.MaxCLevel();
        }

        /// <summary>
        /// default compression level, specified by ZSTD_CLEVEL_DEFAULT, requires v1.5.0+
        /// </summary>
        public static int DefaultCompressionLevel()
        {
            ZstdInit.Manager.EnsureLoaded();

            return ZstdInit.Lib.DefaultCLevel();
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
        #endregion
    }
    #endregion
}
