#define DEBUG_PARALLEL

/*
    Derived from LZ4 header files (BSD 2-Clause)
    Copyright (c) 2011-2016, Yann Collet

    C# Wrapper written by Hajin Jang
    Copyright (C) 2025-present Hajin Jang

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
using Joveler.Compression.LZ4.XXHash;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace Joveler.Compression.LZ4
{
    #region StreamOptions
    /// <summary>
    /// Compress options for LZ4FrameStream
    /// </summary>
    /// <remarks>
    /// Default value is based on default value of lz4 cli
    /// </remarks>
    public sealed class LZ4FrameParallelCompressOptions
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
        /// The number of threads to use for parallel compression.
        /// </summary>
        public int Threads { get; set; } = 1;
        /// <summary>
        /// <para>Control timeout to allow Write() to return early.<br/>
        /// In parallel compression, Write() may block until the data is compressed.
        /// </para>
        /// <para>
        /// Set to 0 to return immdiately after queueing the input data.<br/>
        /// Compression and writing to the BaseStream will be done in background.
        /// </para>
        /// <para>
        /// Set to null to block until all of the compressed data is written to the BaseStream.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Timeout value is kept as best effort, and it may block longer time.
        /// </remarks>
        public TimeSpan? WriteTimeout { get; set; } = null;
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
    public sealed class LZ4FrameParallelDecompressOptions
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
        /// <para>Control timeout to allow Write() to return early.<br/>
        /// In parallel compression, Write() may block until the data is compressed.
        /// </para>
        /// <para>
        /// Set to 0 to return immdiately after queueing the input data.<br/>
        /// Compression and writing to the BaseStream will be done in background.
        /// </para>
        /// <para>
        /// Set to null to block until all of the compressed data is written to the BaseStream.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Timeout value is kept as best effort, and it may block longer time.
        /// </remarks>
        public TimeSpan? WriteTimeout { get; set; } = null;
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

    #region LZ4FrameParallelStream
    public sealed class LZ4FrameParallelStream : Stream
    {
        #region enum Mode
        private enum Mode
        {
            Compress,
            Decompress,
        }
        #endregion

        #region Fields and Properties
        private readonly Mode _mode;
        private readonly TimeSpan? _writeTimeout;
        private readonly int _threads;
        private readonly bool _leaveOpen;
        private bool _disposed = false;

        // Compression
        // System.Threading.Tasks.DataFlow
        private ITargetBlock<LZ4ParallelCompressJob> CompressChunkTarget => _compWorkChunk;
        private readonly TransformBlock<LZ4ParallelCompressJob, LZ4ParallelCompressJob> _compWorkChunk;
        private ISourceBlock<LZ4ParallelCompressJob> CompressChunkSource => _compWorkChunk;
        private ITargetBlock<LZ4ParallelCompressJob> SortChunkTarget => _compSortChunk;
        private readonly LZ4SortedBufferBlock _compSortChunk;
        private ISourceBlock<LZ4ParallelCompressJob> SortChunkSource => _compSortChunk;
        private ITargetBlock<LZ4ParallelCompressJob> WriteChunkTarget => _compWriteChunk;
        private readonly ActionBlock<LZ4ParallelCompressJob> _compWriteChunk;

        private long _inSeq = 0;
        private long _waitSeq = 0;

        private bool _finalEnqueued = false;
        private readonly ManualResetEvent _targetWrittenEvent = new ManualResetEvent(true);

        private readonly ArrayPool<byte> _pool;
        private readonly int _inBlockSize;
        private readonly int _outBlockSize;
        private readonly ReferableBuffer _inputBuffer;
        private readonly PooledBuffer _outputBuffer;

        private readonly bool _usePrefix;
        private ReferableBuffer? _nextPrefixBuffer;

        private readonly bool _calcChecksum;
        private XXH32Stream? _xxh32;

        private readonly CancellationTokenSource _abortTokenSrc = new CancellationTokenSource();
        //private readonly Task[] _compressTasks;
        //private readonly Task _writerTask;

        private IntPtr _mainCctx;
        private FramePreferences _mainCompPrefs;
        private FramePreferences _workCompPrefs;
        private readonly FrameCompressOptions _frameCompOpts = new FrameCompressOptions()
        {
            StableSrc = 0,
        };

        // Decompress
        private IntPtr _mainDctx;
        private readonly FrameDecompressOptions _decompOpts = new FrameDecompressOptions()
        {
            StableDst = 0,
            SkipChecksums = 0,
        };

        // Property
        public Stream? BaseStream { get; private set; }
        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;

        // Const
        // https://github.com/lz4/lz4/blob/master/doc/lz4_Frame_format.md
        internal const uint FrameVersion = 100;

        internal const int DefaultChunkSize = 4 * 1024 * 1024;
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing LZ4FrameStream.
        /// </summary>
        public unsafe LZ4FrameParallelStream(Stream baseStream, LZ4FrameParallelCompressOptions pcompOpts)
        {
            LZ4Init.Manager.EnsureLoaded();

            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Compress;
            _disposed = false;
            _writeTimeout = pcompOpts.WriteTimeout;
            _leaveOpen = pcompOpts.LeaveOpen;

            int threadCount = pcompOpts.Threads;
            if (threadCount < 0)
                throw new InvalidOperationException("Thread count must be greater than or equal to 0(auto).");
            else if (threadCount == 0)
                threadCount = Environment.ProcessorCount;
            _threads = threadCount;

            _inBlockSize = CheckBufferSize(pcompOpts.BufferSize);
            _outBlockSize = DefaultChunkSize;

            // Prepare cctx
            nuint ret = LZ4Init.Lib.CreateFrameCompressContext!(ref _mainCctx, FrameVersion);
            LZ4FrameException.CheckReturnValue(ret);

            // Prepare xxh32
            _calcChecksum = pcompOpts.ContentChecksumFlag == FrameContentChecksum.ContentChecksumEnabled;
            if (_calcChecksum)
                _xxh32 = new XXH32Stream();

            // Prepare FramePreferences
            _workCompPrefs = new FramePreferences()
            { // Ignore pcompOpts.AutoFlush in parallel compress
                FrameInfo = new FrameInfo(pcompOpts.BlockSizeId, pcompOpts.BlockMode, FrameContentChecksum.NoContentChecksum,
                    pcompOpts.FrameType, pcompOpts.ContentSize, 0, pcompOpts.BlockChecksumFlag),
                CompressionLevel = pcompOpts.Level,
                AutoFlush = 1u, // Each compress worker should flush all of its output
                FavorDecSpeed = pcompOpts.FavorDecSpeed ? 1u : 0u,
            };

            _mainCompPrefs = new FramePreferences()
            { // Ignore pcompOpts.AutoFlush in parallel compress
                FrameInfo = new FrameInfo(pcompOpts.BlockSizeId, pcompOpts.BlockMode, pcompOpts.ContentChecksumFlag,
                    pcompOpts.FrameType, pcompOpts.ContentSize, 0, pcompOpts.BlockChecksumFlag),
                CompressionLevel = pcompOpts.Level,
                AutoFlush = 1u, // Each compress worker should flush all of its output
                FavorDecSpeed = pcompOpts.FavorDecSpeed ? 1u : 0u,
            };

            // Query the minimum required size of compress buffer
            // _bufferSize is the source size, frameSize is the (required) dest size
            nuint outBufferSizeVal = LZ4Init.Lib.FrameCompressBound!((nuint)_inBlockSize, _workCompPrefs);
            Debug.Assert(outBufferSizeVal <= int.MaxValue);
            _outBlockSize = (int)outBufferSizeVal;

            // Allocate input buffer
            _pool = pcompOpts.BufferPool ?? ArrayPool<byte>.Shared;
            _inputBuffer = new ReferableBuffer(_pool, _inBlockSize);
            _outputBuffer = new PooledBuffer(_pool, _outBlockSize);
            _usePrefix = _workCompPrefs.FrameInfo.BlockMode == FrameBlockMode.BlockLinked;

            // Write the frame header
            WriteHeader(_mainCompPrefs);

            // Launch CompressTask, WriterTask
            _compWorkChunk = new TransformBlock<LZ4ParallelCompressJob, LZ4ParallelCompressJob>(CompressProc, new ExecutionDataflowBlockOptions()
            {
                CancellationToken = _abortTokenSrc.Token,
                EnsureOrdered = false,
                MaxDegreeOfParallelism = _threads,
                SingleProducerConstrained = true,
            });

            _compSortChunk = new LZ4SortedBufferBlock(_abortTokenSrc);

            _compWriteChunk = new ActionBlock<LZ4ParallelCompressJob>(WriterProc, new ExecutionDataflowBlockOptions()
            {
                CancellationToken = _abortTokenSrc.Token,
                EnsureOrdered = false,
                MaxDegreeOfParallelism = 1,
                SingleProducerConstrained = true
            });

            DataflowLinkOptions linkOptions = new DataflowLinkOptions
            {
                PropagateCompletion = false,
                Append = true,
            };

            _compWorkChunk.LinkTo(_compSortChunk, linkOptions);
            _compSortChunk.LinkTo(_compWriteChunk, linkOptions);
        }

        /// <summary>
        /// Create decompressing LZ4FrameStream.
        /// </summary>
        public unsafe LZ4FrameParallelStream(Stream baseStream, LZ4FrameParallelDecompressOptions decompOpts)
        {
            LZ4Init.Manager.EnsureLoaded();

            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Decompress;
            _disposed = false;

            throw new NotImplementedException();
        }
        #endregion

        #region Disposable Pattern
        ~LZ4FrameParallelStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposing)
                { // Dispose managed state.

                }

                if (LZ4Init.Lib == null)
                    throw new ObjectDisposedException(nameof(LZ4Init));

                // Dispose unmanaged resources, and set large fields to null.
                if (_mainCctx != IntPtr.Zero)
                { // Compress
                    FinishWrite();

                    nuint ret = LZ4Init.Lib.FreeFrameCompressContext!(_mainCctx);
                    LZ4FrameException.CheckReturnValue(ret);

                    _mainCctx = IntPtr.Zero;
                }

                if (_mainDctx != IntPtr.Zero)
                {
                    nuint ret = LZ4Init.Lib.FreeFrameDecompressContext!(_mainDctx);
                    LZ4FrameException.CheckReturnValue(ret);

                    _mainDctx = IntPtr.Zero;
                }

                if (_xxh32 != null)
                {
                    _xxh32.Dispose();
                    _xxh32 = null;
                }

                _nextPrefixBuffer?.Dispose();
                _inputBuffer.Dispose();
                _outputBuffer.Dispose();

                _abortTokenSrc.Dispose();

                if (BaseStream != null)
                {
                    BaseStream.Flush();
                    if (!_leaveOpen)
                        BaseStream.Dispose();
                    BaseStream = null;
                }

                _disposed = true;
            }
        }
        #endregion

        #region Main Thread - Read/Write
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

            throw new NotImplementedException();
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
            if (span.Length == 0)
                return;

            // Do nothing if the instance was already aborted.
            if (_abortTokenSrc.IsCancellationRequested)
                return;

            // Pool the input buffer until it is full.
            bool enqueued = false;
            int bytesWritten = 0;
            do
            {
                bytesWritten += _inputBuffer.Write(span.Slice(bytesWritten), false);

                // If the input buffer is full -> enqueue the job, and reset the input buffer.
                if (_inputBuffer.IsFull)
                {
                    EnqueueInputBuffer(false);
                    enqueued = true;
                }
            }
            while (bytesWritten < span.Length);

            // Alert the worker thread to resume compressing
            if (!enqueued)
                return;

            // Wait until the output is ready
            WaitWriteJobComplete(_inSeq, _writeTimeout);
        }

        private void EnqueueInputBuffer(bool isFinal)
        {
            if (_finalEnqueued)
                throw new InvalidOperationException("The final block has already been enqueued.");

            Debug.Assert(_inSeq == 0 && _nextPrefixBuffer == null ||
                _inSeq != 0 && _nextPrefixBuffer != null && !_nextPrefixBuffer.Disposed);

            _finalEnqueued |= isFinal;

            // _refCount of job.InBuffer and _nextDictBuffer is increased in constructor call.
            LZ4ParallelCompressJob job = new LZ4ParallelCompressJob(_pool, _inSeq, _inBlockSize, _nextPrefixBuffer, _outBlockSize);
            _inSeq += 1;

            if (isFinal)
                job.IsLastBlock = true;

            // Calculate xxh32
            _xxh32?.Write(_inputBuffer.ReadablePortionSpan);
            job.InBuffer.Write(_inputBuffer.ReadablePortionSpan, true);
            job.RawInputSize = job.InBuffer.DataEndIdx;

            // Prepare next dictionary buffer (pass for final block)
            if (_usePrefix)
            {
                if (isFinal)
                {
                    _nextPrefixBuffer = null; // No longer required
                }
                else if (LZ4ParallelCompressJob.DictWindowSize <= job.InBuffer.ReadableSize)
                {
                    _nextPrefixBuffer = job.InBuffer.AcquireRef();
                }
                else
                { // next input is less than 64K -> retain last 64K
                    ReferableBuffer copyPrefixBuffer = new ReferableBuffer(_pool, LZ4ParallelCompressJob.DictWindowSize);
                    if (_nextPrefixBuffer == null)
                    { // First block, but the input is less than 64K -> copy the full input
                        copyPrefixBuffer.Write(job.InBuffer.ReadablePortionSpan);
                    }
                    else
                    { // Normal block, source 64K from the previous input + current input
                        // DO NOT USE _nextPrefixBuffer.DataStartIdx! It may be changed anytime because the buffer is shared.
                        int copyLastDictSize = LZ4ParallelCompressJob.DictWindowSize - job.InBuffer.ReadableSize;
                        ReadOnlySpan<byte> lastDictSpan = _nextPrefixBuffer.Buf.AsSpan(_nextPrefixBuffer.DataEndIdx - copyLastDictSize, copyLastDictSize);
                        copyPrefixBuffer.Write(lastDictSpan);

                        // Release previous ref of the _nextDictBuffer.
                        _nextPrefixBuffer.ReleaseRef();

                        // Copy current input, acheiving full 32K.
                        copyPrefixBuffer.Write(job.InBuffer.ReadablePortionSpan);

                        Debug.Assert(copyPrefixBuffer.DataEndIdx == LZ4ParallelCompressJob.DictWindowSize);
                    }
                    _nextPrefixBuffer = copyPrefixBuffer.AcquireRef();
                }
            }

            _inputBuffer.Clear();
            CompressChunkTarget.Post(job);
        }

        private void EnqueueInputEof()
        {
            CompressChunkTarget.Complete();
        }
        #endregion

        #region MainThread - Flush, Abort, FinishWrite
        private unsafe void FinishWrite()
        {
            Debug.Assert(_mode == Mode.Compress, "FinishWrite() cannot be called in decompression");
            if (BaseStream == null)
                throw new ObjectDisposedException(nameof(LZ4FrameStream));

            // If aborted, return immediately
            if (_abortTokenSrc.IsCancellationRequested)
                return;

            // Flush and enqueue a final block with remaining buffer.
            EnqueueInputBuffer(true);

            // Wait until dataflow completes its job.
            _compWorkChunk.Completion.Wait();
            _compSortChunk.Completion.Wait();
            _compWriteChunk.Completion.Wait();
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
                _outputBuffer.Clear();

                nuint ret;
                fixed (byte* outPtr = _outputBuffer.Buf)
                {
                    ret = LZ4Init.Lib.FrameFlush!(_mainCctx, outPtr, (nuint)_outputBuffer.Capacity, _frameCompOpts);
                }
                LZ4FrameException.CheckReturnValue(ret);

                int outSize = (int)ret;
                if (0 < outSize)
                    BaseStream.Write(_outputBuffer.Buf, 0, outSize);

                _outputBuffer.Clear();
            }
            BaseStream.Flush();
        }

        public void Abort()
        {
            if (_abortTokenSrc.IsCancellationRequested)
                return;
            _abortTokenSrc.Cancel();
        }

        private void WaitWriteJobComplete(long checkSeqNum, TimeSpan? waitMax)
        {
            Debug.Assert(0 <= checkSeqNum);
            Debug.Assert(_waitSeq <= checkSeqNum);

            Interlocked.Exchange(ref _waitSeq, checkSeqNum);

            // Wait until writerThread processes block of _waitSeq.
            if (waitMax == null) // Block indefinitely
                _targetWrittenEvent.WaitOne();
            else // Block for a finite time
                _targetWrittenEvent.WaitOne(waitMax.Value);
        }
        #endregion

        #region CompressThread 
        internal unsafe LZ4ParallelCompressJob CompressProc(LZ4ParallelCompressJob job)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));
            if (BaseStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            IntPtr compCctx = IntPtr.Zero;
            try
            {
                // Prepare cctx
                nuint ret = LZ4Init.Lib.CreateFrameCompressContext!(ref compCctx, LZ4FrameStream.FrameVersion);
                LZ4FrameException.CheckReturnValue(ret);

                // Init and nullify frame header
                // - Do not increase job.OutBuffer.DataEndIdx
                if (job.PrefixBuffer == null)
                { // First job
                    nuint headerSizeVal;
                    fixed (byte* outPtr = job.OutBuffer.Buf)
                    {
                        headerSizeVal = LZ4Init.Lib.FrameCompressBegin!(compCctx, outPtr, (nuint)job.OutBuffer.Capacity, _workCompPrefs);
                    }
                    LZ4FrameException.CheckReturnValue(headerSizeVal);
                    Debug.Assert(0 <= headerSizeVal && headerSizeVal < int.MaxValue);
                }
                else
                { // Put previous 64KB as a prefix
                    int prefixSize = Math.Min(job.PrefixBuffer.DataEndIdx, LZ4ParallelCompressJob.DictWindowSize);
                    int prefixStartPos = job.PrefixBuffer.DataEndIdx - prefixSize;

                    nuint headerSizeVal;
                    fixed (byte* outPtr = job.OutBuffer.Buf)
                    fixed (byte* prefixPtr = job.PrefixBuffer.Buf)
                    {
                        headerSizeVal = LZ4Init.Lib.FrameCompressBeginUsingDict!(compCctx, outPtr, (nuint)job.OutBuffer.Capacity, prefixPtr + prefixStartPos, (nuint)prefixSize, _workCompPrefs);
                    }
                    LZ4FrameException.CheckReturnValue(headerSizeVal);
                    Debug.Assert(0 <= headerSizeVal && headerSizeVal < int.MaxValue);
                }

                // Compress actual data
                nuint outSizeVal;
                fixed (byte* inPtr = job.InBuffer.Buf)
                fixed (byte* outPtr = job.OutBuffer.Buf)
                {
                    outSizeVal = LZ4Init.Lib.FrameCompressUpdate!(compCctx, outPtr, (nuint)job.OutBuffer.Capacity, inPtr, (nuint)job.InBuffer.ReadableSize, _frameCompOpts);
                }
                LZ4FrameException.CheckReturnValue(outSizeVal);

                Debug.Assert(outSizeVal < int.MaxValue, "BufferSize should be <2GB");
                job.OutBuffer.DataEndIdx += (int)outSizeVal;

                if (job.IsLastBlock)
                    _compWorkChunk.Complete();
            }
            finally
            {
                job.PrefixBuffer?.ReleaseRef();

                if (compCctx != IntPtr.Zero)
                {
                    nuint ret = LZ4Init.Lib.FreeFrameCompressContext!(compCctx);
                    LZ4FrameException.CheckReturnValue(ret);

                    compCctx = IntPtr.Zero;
                }
            }

#if DEBUG_PARALLEL
            Console.WriteLine($"-- compressed: {job}");
#endif

            return job;
        }
        #endregion

        #region WriterThread
        internal unsafe void WriterProc(LZ4ParallelCompressJob job)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));
            if (BaseStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            try
            {
                // Write to BaseStream
                BaseStream.Write(job.OutBuffer.Buf, 0, job.OutBuffer.DataEndIdx);

#if DEBUG_PARALLEL
                Console.WriteLine($"-- wrote: {job}");
#endif

                // Increase TotalIn & TotalOut
                TotalIn += job.RawInputSize;
                TotalOut += job.OutBuffer.ReadableSize;

                if (job.IsLastBlock)
                { // Write trailer
                    WriteTrailer();
                    _targetWrittenEvent.Set();
                    _compWriteChunk.Complete();
                    return;
                }

                // Is the target block was written to the block?
                // Inform the main thread which may be waiting for a signal.
                if (_waitSeq <= job.Seq)
                    _targetWrittenEvent.Set();
                else
                    _targetWrittenEvent.Reset();
            }
            finally
            {
                job.Dispose();
            }
        }
        #endregion

        #region WriteHeader, WriteTrailer
        private unsafe void WriteHeader(FramePreferences prefs)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));
            if (BaseStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            _outputBuffer.Clear();

            nuint headerSizeVal;
            fixed (byte* dest = _outputBuffer.Buf)
            {
                headerSizeVal = LZ4Init.Lib.FrameCompressBegin!(_mainCctx, dest, (nuint)_inBlockSize, prefs);
                LZ4FrameException.CheckReturnValue(headerSizeVal);
            }
            
            Debug.Assert(0 <= headerSizeVal && headerSizeVal < int.MaxValue);

            int headerSize = (int)headerSizeVal;
            BaseStream.Write(_outputBuffer.Buf, 0, headerSize);
            TotalOut += headerSize;
            _outputBuffer.Clear();
        }

        private unsafe void WriteTrailer()
        {
            if (BaseStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            _outputBuffer.Clear();

            // *NOTE*: DO NOT CALL FrameCompressEnd, we do it ourselves.
            // - FrameCompressEnd checks for ContentSize, but in parallel compression mainCctx does not know about raw data size.
            // - FrameCompressEnd writes XXH32 checksum if the flag is set, but in parallel compression mainCctx produces invalid xxh32.
            // 
            // LZ4F_compressEnd: LZ4F_flush() + 0x00000000 + XXH32_digest
            // - mainCctx is not used to compress -> Nothing to write on LZ4F_flush

            // Check ContentSize with TotalIn ourselves (if it is set)
            if (_workCompPrefs.FrameInfo.ContentSize != 0 && TotalIn != (long)_workCompPrefs.FrameInfo.ContentSize)
                throw new InvalidDataException($"Total bytes of input [{TotalIn}] does not match stated ContentSize [{_workCompPrefs.FrameInfo.ContentSize}].");

            // End of Frame Marker (0x00000000)
            byte[] endMarkBuf = [0, 0, 0, 0];
            BaseStream.Write(endMarkBuf, 0, endMarkBuf.Length);
            TotalOut += 4;

            // Write content checksum ourselves.
            if (_xxh32 != null)
            {
                byte[] checkBuf = _xxh32.HashBytesLE;
                BaseStream.Write(checkBuf, 0, checkBuf.Length);
                TotalOut += checkBuf.Length;
            }
        }
        #endregion

        #region Stream Properties
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

        private static int CheckBufferSize(int bufferSize)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            return Math.Max(bufferSize, DefaultChunkSize);
        }
        #endregion
    }
    #endregion
}
