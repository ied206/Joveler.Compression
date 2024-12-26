/*
    Derived from Zstandard header files (BSD 2-Clause)
    Copyright (c) 2016-present, Yann Collet, Facebook, Inc. All rights reserved.

    C# Wrapper written by Hajin Jang
    Copyright (C) 2020-2023 Hajin Jang

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
    public sealed class ZstdCompressOptions
    {
        #region General parameters
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = ZstdStream.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the zstd stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
        /// <summary>
        /// Compression level of <cref>ZstdFrameStream</cref>. 
        /// 
        /// Default level is 0, which is controlled by <cref>ZstdFrameStream.CLevelDefault</cref>.
        /// 
        /// Refer to <cref>ZstdStream.MinCompressionLevel()</cref> and <cref>ZstdStream.MaxCompressionLevel()</cref> for bounds.
        /// </summary>
        /// <remarks>
        /// Note 1 : it's possible to pass a negative compression level.
        /// Note 2 : setting a level does not automatically set all other compression parameters
        ///   to default. Setting this will however eventually dynamically impact the compression
        ///   parameters which have not been manually set. The manually set
        ///   ones will 'stick'.
        /// </remarks>
        public int CompressionLevel { get; set; } = 0;
        /// <summary>
        /// Size of uncompressed content ; 0 == unknown
        /// This value will also be controlled at end of frame, and trigger an error if not respected.
        /// </summary>
        public ulong ContentSize { get; set; } = 0;
        #endregion

        #region Advanced compression parameters
        // It's possible to pin down compression parameters to some specific values.
        // In which case, these values are no longer dynamically selected by the compressor
        /// <summary>
        /// Maximum allowed back-reference distance, expressed as power of 2.
        /// 
        /// This will set a memory budget for streaming decompression,
        /// with larger values requiring more memory
        /// and typically compressing more.
        /// Must be clamped between ZSTD_WINDOWLOG_MIN and ZSTD_WINDOWLOG_MAX.
        /// 
        /// Note: Using a windowLog greater than ZSTD_WINDOWLOG_LIMIT_DEFAULT
        ///       requires explicitly allowing such size at streaming decompression stage.
        /// </summary>
        /// <remarks>
        /// Special: value 0 means "use default windowLog".
        /// </remarks>
        public uint WindowLog { get; set; } = 0;
        /// <summary>
        /// Size of the initial probe table, as a power of 2.
        /// Resulting memory usage is (1 << (hashLog+2)).
        /// Must be clamped between ZSTD_HASHLOG_MIN and ZSTD_HASHLOG_MAX.
        /// Larger tables improve compression ratio of strategies <= dFast,
        /// and improve speed of strategies > dFast.
        /// </summary>
        /// <remarks>
        /// Special: value 0 means "use default hashLog".
        /// </remarks>
        public uint HashLog { get; set; } = 0;
        /// <summary>
        /// Size of the multi-probe search table, as a power of 2.
        /// Resulting memory usage is (1 << (chainLog+2)).
        /// Must be clamped between ZSTD_CHAINLOG_MIN and ZSTD_CHAINLOG_MAX.
        /// Larger tables result in better and slower compression.
        /// This parameter is useless for "fast" strategy.
        /// It's still useful when using "dfast" strategy,
        /// in which case it defines a secondary probe table.
        /// </summary>
        /// <remarks> 
        /// Special: value 0 means "use default chainLog".
        /// </remarks>
        public uint ChainLog { get; set; } = 0;
        /// <summary>
        /// Number of search attempts, as a power of 2.
        /// More attempts result in better and slower compression.
        /// This parameter is useless for "fast" and "dFast" strategies.
        /// </summary>
        /// <remarks>
        /// Special: value 0 means "use default searchLog".
        /// </remarks>
        public uint SearchLog { get; set; } = 0;
        /// <summary>
        /// Minimum size of searched matches.
        /// 
        /// Note that Zstandard can still find matches of smaller size,
        /// it just tweaks its search algorithm to look for this size and larger.
        /// Larger values increase compression and decompression speed, but decrease ratio.
        /// Must be clamped between ZSTD_MINMATCH_MIN and ZSTD_MINMATCH_MAX.
        /// Note that currently, for all strategies < btopt, effective minimum is 4.
        ///                    , for all strategies > fast, effective maximum is 6.
        /// </summary>
        /// <remarks>
        /// Special: value 0 means "use default minMatchLength". 
        /// </remarks>
        public uint MinMatch { get; set; } = 0;
        /// <summary>
        /// Impact of this field depends on strategy.
        /// For strategies btopt, btultra & btultra2:
        ///     Length of Match considered "good enough" to stop search.
        ///     Larger values make compression stronger, and slower.
        /// For strategy fast:
        ///     Distance between match sampling.
        ///     Larger values make compression faster, and weaker.
        /// </summary>
        /// <remarks>
        /// Special: value 0 means "use default targetLength".
        /// </remarks>
        public uint TargetLength { get; set; } = 0;
        /// <summary>
        /// See ZSTD_strategy enum definition.
        /// The higher the value of selected strategy, the more complex it is,
        /// resulting in stronger and slower compression.
        /// Special: value 0 means "use default strategy".
        /// </summary>
        public Strategy Strategy { get; set; } = Strategy.Default;
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

        #region zstd Dictionary
        /// <summary>
        /// A buffer to feed zstd custom dictionary.
        /// </summary>
        public byte[] DictBuffer { get; set; } = null;
        #endregion
    }

    /// <summary>
    /// Decompress options for ZstdStream
    /// </summary>
    public sealed class ZstdDecompressOptions
    {
        #region General parameters
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = ZstdStream.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the zstd stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
        #endregion

        #region zstd Dictionary
        /// <summary>
        /// A buffer to feed zstd custom dictionary.
        /// </summary>
        public byte[] DictBuffer { get; set; } = null;
        #endregion
    }
    #endregion

    #region ZstdStream
    public unsafe sealed class ZstdStream : Stream
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

        private readonly byte[] _buf;
        private readonly int _bufSize = DefaultBufferSize;

        // Decompression
        private int _decompSrcIdx = 0;
        private int _decompSrcCount = 0;

        // Property
        public Stream BaseStream { get; private set; }
        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;

        // Const
        private const int DecompressComplete = -1;
        internal const int DefaultBufferSize = 1024 * 1024;
        private const uint BlocksizeMax = 1 << 17;
        // private static readonly byte[] FrameMagicNumber = { 0x28, 0xB5, 0x2F, 0xFD }; // 0xFD2FB528 (LE), valid since v0.8.0
        // private static readonly byte[] MagicDictionary = { 0x37, 0xA4, 0x30, 0xEC }; // 0xEC30A437 (LE), valid since v0.7.0
        // private static readonly byte[] MagicSkippableStart = { 0x50, 0x2A, 0x4D, 0x18 }; // 0x184D2A50 (LE), all 16 values, from 0x184D2A50 to 0x184D2A5F, signal the beginning of a skippable frame
        // private static readonly byte[] MagicSkippableMask = { 0xF0, 0xFF, 0xFF, 0xFF };
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing ZstdFrameStream.
        /// </summary>
        public unsafe ZstdStream(Stream baseStream, ZstdCompressOptions compOpts)
        {
            // From FIO_createCResources()
            ZstdInit.Manager.EnsureLoaded();

            // Check arguments
            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            if (compOpts.MTWorkers < 0)
                throw new ArgumentException($"{nameof(ZstdCompressOptions)}.{nameof(ZstdCompressOptions.MTWorkers)} [{compOpts.MTWorkers}] must be equal or larger than 0");

            _mode = Mode.Compress;
            _disposed = false;
            _leaveOpen = compOpts.LeaveOpen;

            // Get recommended size for input buffer.
            /*
            UIntPtr srcBufSize = ZstdInit.Lib.CStreamInSize();
            if (uint.MaxValue < srcBufSize.ToUInt64())
                _srcBufSize = uint.MaxValue;
            else
                _srcBufSize = srcBufSize.ToUInt32();
            _srcBuf = new byte[_srcBufSize];
            */

            /*
            // Get recommended size for output buffer. Guarantee to successfully flush at least one complete compressed block.
            UIntPtr destBufSize = ZstdInit.Lib.CStreamOutSize();
            if (uint.MaxValue < destBufSize.ToUInt64())
                _bufSize = uint.MaxValue;
            else
                _bufSize = destBufSize.ToUInt32();
            _buf = new byte[_bufSize];
            */

            // Allocate resources (Based on FIO_createCResources())
            // Check and set compress options
            _leaveOpen = compOpts.LeaveOpen;
            _bufSize = CheckBufferSize(compOpts.BufferSize);
            _buf = new byte[_bufSize];

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
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.ChecksumFlag, compOpts.ChecksumFlag));
            ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.DictIdFlag, compOpts.DictIdFlag));

            // Set multithread parameters
            if (0 < compOpts.MTWorkers)
            {
                ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.NbWorkers, compOpts.MTWorkers));
                ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.JobSize, compOpts.MTJobSize));
                ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.OverlapLog, compOpts.MTOverlapLog));
            }

            // Set Dictionary
            if (compOpts.DictBuffer != null && 0 < compOpts.DictBuffer.Length)
            {
                fixed (byte* bufPtr = compOpts.DictBuffer)
                {
                    ZstdInit.Lib.CctxLoadDictionary(_cstream, bufPtr, (UIntPtr)compOpts.DictBuffer.Length);
                }
            }

            // Set PledgedSrcSize if given
            if (0 < compOpts.ContentSize)
            {
                ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.ContentSizeFlag, 1));
                ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetPledgedSrcSize(_cstream, compOpts.ContentSize));
            }
            else
            {
                ZstdException.CheckReturnValue(ZstdInit.Lib.CCtxSetParameter(_cstream, CParameter.ContentSizeFlag, 0));
            }
        }

        /// <summary>
        /// Create decompressing ZstdFrameStream.
        /// </summary>
        public unsafe ZstdStream(Stream baseStream, ZstdDecompressOptions decompOpts)
        {
            ZstdInit.Manager.EnsureLoaded();

            // Check arguments
            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Decompress;
            _disposed = false;

            // Check and set compress options
            _leaveOpen = decompOpts.LeaveOpen;
            _bufSize = CheckBufferSize(decompOpts.BufferSize);
            _buf = new byte[_bufSize];

            // From FIO_createDResources
            _dstream = ZstdInit.Lib.CreateDStream();
            if (_dstream == IntPtr.Zero)
            { // Unable to create dctx
                throw new InvalidOperationException("allocation error: cannot create ZSTD_DStream");
            }

            // Dictionary
            if (decompOpts.DictBuffer != null && 0 < decompOpts.DictBuffer.Length)
            {
                fixed (byte* bufPtr = decompOpts.DictBuffer)
                {
                    ZstdException.CheckReturnValue(ZstdInit.Lib.DctxLoadDictionary(_dstream, bufPtr, (UIntPtr)decompOpts.DictBuffer.Length));
                }
            }
        }
        #endregion

        #region Disposable Pattern
        ~ZstdStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (_cstream != IntPtr.Zero)
                {
                    FinishWrite();

                    ZstdInit.Lib.FreeCStream(_cstream);
                    _cstream = IntPtr.Zero;
                }

                if (_dstream != IntPtr.Zero)
                {
                    ZstdInit.Lib.FreeDStream(_dstream);
                    _dstream = IntPtr.Zero;
                }

                if (BaseStream != null)
                {
                    Flush();
                    if (!_leaveOpen)
                        BaseStream.Dispose();
                    BaseStream = null;
                }

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

            // Reached end of stream
            if (_decompSrcIdx == DecompressComplete)
                return 0;

            int readSize = 0;

            // Main decompression loop
            fixed (byte* src = _buf)
            fixed (byte* dest = span)
            {
                // Setup OutBuffer
                OutBuffer outBuf = new OutBuffer()
                {
                    Dst = dest,
                    Size = (UIntPtr)span.Length,
                    Pos = UIntPtr.Zero,
                };

                int outBufPosBak = 0;

                // C#'s indexing is limited to int.MaxValue.
                while (outBuf.Pos.ToUInt32() < outBuf.Size.ToUInt32())
                {
                    // If we are out of buffer to decompress, read it from BaseStream.
                    if (_decompSrcIdx == _decompSrcCount)
                    {
                        // Read from BaseStream
                        _decompSrcIdx = 0;
                        _decompSrcCount = BaseStream.Read(_buf, 0, _buf.Length);
                        TotalIn += _decompSrcCount;

                        // BaseStream readched its end
                        if (_decompSrcCount == 0)
                        {
                            _decompSrcIdx = DecompressComplete;
                            break;
                        }
                    }

                    // Setup InBuffer
                    InBuffer inBuf = new InBuffer()
                    {
                        Src = src,
                        Size = (UIntPtr)_decompSrcCount,
                        Pos = (UIntPtr)_decompSrcIdx,
                    };

                    // Call ZSTD_decompressStream()
                    UIntPtr ret = ZstdInit.Lib.DecompressStream(_dstream, outBuf, inBuf);
                    ZstdException.CheckReturnValueWithDStream(ret, _dstream);
                    ulong readSizeHint = ret.ToUInt64();

                    // How many source bytes are decompressed?
                    // _decompSrcIdx += (int)inBuf.Pos.ToUInt32() - _decompSrcIdx;
                    _decompSrcIdx = (int)inBuf.Pos.ToUInt32();

                    // How many destination bytes are written?
                    int outBufPos = (int)outBuf.Pos;
                    int outBufWritten = outBufPos - outBufPosBak;
                    outBufPosBak = outBufPos;

                    TotalOut += outBufWritten;
                    readSize += outBufWritten;

                    // Is it the end of zstd frame?
                    if (readSizeHint == 0)
                        break;

                    // Is outBuf full?
                    if (outBuf.Pos.ToUInt64() == outBuf.Size.ToUInt64())
                        break;
                }
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

            fixed (byte* src = span)
            fixed (byte* dest = _buf)
            {
                EndDirective endDirective = EndDirective.Continue;
                int inBufPosBak = 0;
                InBuffer inBuf = new InBuffer()
                {
                    Src = src,
                    Size = (UIntPtr)span.Length,
                    Pos = (UIntPtr)0,
                };
                OutBuffer outBuf = new OutBuffer()
                {
                    Dst = dest,
                    Size = (UIntPtr)_bufSize,
                    Pos = (UIntPtr)0,
                };

                // C#'s indexing is limited to int.MaxValue.
                while (inBuf.Pos.ToUInt32() < inBuf.Size.ToUInt32())
                {
                    UIntPtr ret = UIntPtr.Zero;

                    // How many bytes are ready to be flushed?
                    // ret = ZstdInit.Lib.ToFlushNow(_cstream);
                    // ulong toFlushNow = ret.ToUInt64();

                    // Compress the input buffer
                    ret = ZstdInit.Lib.CompressStream2(_cstream, outBuf, inBuf, endDirective);
                    ZstdException.CheckReturnValueWithCStream(ret, _cstream);
                    // ulong stillToFlush = ret.ToUInt64();

                    // Reset EndDirective
                    endDirective = EndDirective.Continue;

                    // Write output buffer to baseStream, and reset it
                    int outBufPos = (int)outBuf.Pos;
                    if (0 < outBufPos)
                    {
                        BaseStream.Write(_buf, 0, outBufPos);
                        TotalOut += outBufPos;
                    }
                    outBuf.Pos = (UIntPtr)0;

                    // Check remaining input buffer 
                    int inBufPos = (int)inBuf.Pos;
                    int inBufRead = inBufPos - inBufPosBak;
                    inBufPosBak = inBufPos;
                    TotalIn += inBufRead;

                    // Check if flush is necessary
                    if (inBufRead == 0) // Buffer is full, need flush
                        endDirective = EndDirective.Flush;
                }
            }
        }

        private unsafe void FinishWrite()
        {
            fixed (byte* dest = _buf)
            {
                InBuffer inBuf = new InBuffer()
                {
                    Src = null,
                    Size = UIntPtr.Zero,
                    Pos = UIntPtr.Zero,
                };
                OutBuffer outBuf = new OutBuffer()
                {
                    Dst = dest,
                    Size = (UIntPtr)_bufSize,
                    Pos = UIntPtr.Zero,
                };

                ulong stillToFlush = 0;
                do
                {
                    UIntPtr ret = ZstdInit.Lib.CompressStream2(_cstream, outBuf, inBuf, EndDirective.End);
                    ZstdException.CheckReturnValue(ret);

                    // stillToFlush must be 0 to finish.
                    stillToFlush = ret.ToUInt64();

                    // Write output buffer to baseStream, and reset it
                    int outBufPos = (int)outBuf.Pos;
                    if (0 < outBufPos)
                    {
                        BaseStream.Write(_buf, 0, outBufPos);
                        TotalOut += outBufPos;
                    }
                    outBuf.Pos = (UIntPtr)0;
                }
                while (0 < stillToFlush);
            }

        }

        /// <inheritdoc />
        public override void Flush()
        {
            if (_mode == Mode.Compress)
            {
                /*
                fixed (byte* dest = _buf)
                {
                    InBuffer inBuf = new InBuffer()
                    {
                        Src = null,
                        Size = UIntPtr.Zero,
                        Pos = UIntPtr.Zero,
                    };
                    OutBuffer outBuf = new OutBuffer()
                    {
                        Dst = dest,
                        Size = (UIntPtr)_bufSize,
                        Pos = UIntPtr.Zero,
                    };

                    ulong stillToFlush = 0;
                    do
                    {
                        UIntPtr ret = ZstdInit.Lib.CompressStream2(_cstream, outBuf, inBuf, EndDirective.Flush);
                        ZstdException.CheckReturnValue(ret);

                        // stillToFlush must be 0 to finish.
                        stillToFlush = ret.ToUInt64();

                        // Write output buffer to baseStream, and reset it
                        int outBufPos = (int)outBuf.Pos;
                        if (0 < outBufPos)
                        {
                            BaseStream.Write(_buf, 0, outBufPos);
                            TotalOut += outBufPos;
                        }
                        outBuf.Pos = (UIntPtr)0;
                    }
                    while (0 < stillToFlush);
                }
                */
            }
            else if (_mode == Mode.Decompress)
            {

            }

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
                        throw new InvalidOperationException($"Internal Logic Error at {nameof(ZstdStream)}.{nameof(CompressionRatio)}");
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
