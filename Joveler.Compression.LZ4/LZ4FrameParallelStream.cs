// #define DEBUG_PARALLEL

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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Joveler.Compression.LZ4
{
    #region StreamOptions
    /// <summary>
    /// Options to control parallel LZ4 frame compression
    /// </summary>
    /// <remarks>
    /// Default value is based on default value of lz4 cli
    /// </remarks>
    public sealed class LZ4FrameParallelCompressOptions
    {
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
    }
    #endregion

    #region LZ4FrameParallelStream
    public sealed class LZ4FrameParallelStream : Stream
    {
        #region Fields and Properties
        private readonly TimeSpan? _writeTimeout;
        private readonly int _threads;
        private readonly bool _leaveOpen;
        private bool _disposed = false;

        // Compression
        // System.Threading.Tasks.DataFlow
        private readonly TransformBlock<LZ4ParallelCompressJob, LZ4ParallelCompressJob> _compWorkChunk;
        private readonly ActionBlock<LZ4ParallelCompressJob> _compSortChunk;

        private readonly SortedSet<LZ4ParallelCompressJob> _outSet = new SortedSet<LZ4ParallelCompressJob>(new LZ4ParallelCompressJobComparator());

        private readonly ActionBlock<LZ4ParallelCompressJob> _compWriteChunk;

        private long _inSeq = 0;
        private long _outSeq = 0;
        private long _latestSeq = -1;
        private long _waitSeq = -1;

        private bool _finalEnqueued = false;
        private readonly ManualResetEvent _targetWrittenEvent = new ManualResetEvent(true);

        private readonly ArrayPool<byte> _pool;
        private readonly int _inBlockSize;
        private readonly int _outBlockSize;
        private readonly PooledBuffer _inputBuffer;

        private readonly bool _isBlockLinked;
        private ReferableBuffer? _nextDictBuffer;

        private readonly bool _calcChecksum;
        private XXH32Stream? _xxh32;

        private readonly CancellationTokenSource _abortTokenSrc = new CancellationTokenSource();
        public bool IsAborted => _abortTokenSrc.IsCancellationRequested;

        private readonly List<Exception> _taskExcepts = new List<Exception>();

        private IntPtr _mainCctx;
        private readonly FramePreferences _workCompPrefs;
        private readonly FrameCompressOptions _frameCompOpts = new FrameCompressOptions()
        {
            StableSrc = 0,
        };

        // Property
        public Stream? BaseStream { get; private set; }
        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;

        // Const
        // https://github.com/lz4/lz4/blob/master/doc/lz4_Frame_format.md
        internal const uint FrameVersion = 100;
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing LZ4FrameStream.
        /// </summary>
        public unsafe LZ4FrameParallelStream(Stream baseStream, LZ4FrameCompressOptions compOpts, LZ4FrameParallelCompressOptions pcompOpts)
        {
            LZ4Init.Manager.EnsureLoaded();

            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _disposed = false;
            _writeTimeout = pcompOpts.WriteTimeout;
            _leaveOpen = compOpts.LeaveOpen;

            int threadCount = pcompOpts.Threads;
            if (threadCount < 0)
                throw new InvalidOperationException("Thread count must be greater than or equal to 0(auto).");
            else if (threadCount == 0)
                threadCount = Environment.ProcessorCount;
            _threads = threadCount;

            // Prepare cctx
            nuint ret = LZ4Init.Lib.CreateFrameCompressContext!(ref _mainCctx, FrameVersion);
            LZ4FrameException.CheckReturnValue(ret);

            // Prepare xxh32
            _calcChecksum = compOpts.ContentChecksumFlag == FrameContentChecksum.ContentChecksumEnabled;
            if (_calcChecksum)
                _xxh32 = new XXH32Stream();

            // Prepare FramePreferences
            _workCompPrefs = new FramePreferences()
            { // Ignore pcompOpts.AutoFlush in parallel compress
                FrameInfo = new FrameInfo(compOpts.BlockSizeId, compOpts.BlockMode, FrameContentChecksum.NoContentChecksum,
                    compOpts.FrameType, compOpts.ContentSize, 0, compOpts.BlockChecksumFlag),
                CompressionLevel = compOpts.Level,
                AutoFlush = 1u, // Each compress worker should flush all of its output
                FavorDecSpeed = compOpts.FavorDecSpeed ? 1u : 0u,
            };

            FramePreferences mainCompPrefs = new FramePreferences()
            { // Ignore pcompOpts.AutoFlush in parallel compress
                FrameInfo = new FrameInfo(compOpts.BlockSizeId, compOpts.BlockMode, compOpts.ContentChecksumFlag,
                    compOpts.FrameType, compOpts.ContentSize, 0, compOpts.BlockChecksumFlag),
                CompressionLevel = compOpts.Level,
                AutoFlush = 1u, // Each compress worker should flush all of its output
                FavorDecSpeed = compOpts.FavorDecSpeed ? 1u : 0u,
            };

            // Get block size of the compression config
            nuint blockSizeVal = LZ4Init.Lib.FrameGetBlockSize!(compOpts.BlockSizeId);
            LZ4FrameException.CheckReturnValue(blockSizeVal);
            _inBlockSize = (int)blockSizeVal;

            // Query the minimum required size of destination buffer
            nuint outBufferSizeVal = LZ4Init.Lib.FrameCompressBound!((nuint)_inBlockSize, _workCompPrefs);
            Debug.Assert(outBufferSizeVal <= int.MaxValue);
            _outBlockSize = (int)outBufferSizeVal;

            // Allocate input buffer
            _pool = compOpts.BufferPool ?? ArrayPool<byte>.Shared;
            _inputBuffer = new PooledBuffer(_pool, _inBlockSize);
            _isBlockLinked = _workCompPrefs.FrameInfo.BlockMode == FrameBlockMode.BlockLinked;

            // Write the frame header
            WriteHeader(mainCompPrefs);

            // Launch CompressTask, WriterTask
            // - If BoundedCapacity is set, it will discard incoming message from Post() when the its queue is full.
            // - So in that case, take care to use SendAsync() instead of Post().
            int maxWaitingJobs = 4 * _threads;
            _compWorkChunk = new TransformBlock<LZ4ParallelCompressJob, LZ4ParallelCompressJob>(CompressProc, new ExecutionDataflowBlockOptions()
            {
                CancellationToken = _abortTokenSrc.Token,
                BoundedCapacity = maxWaitingJobs,
                EnsureOrdered = false,
                MaxDegreeOfParallelism = _threads,
            });

            _compSortChunk = new ActionBlock<LZ4ParallelCompressJob>(WriteSortProc, new ExecutionDataflowBlockOptions()
            {
                CancellationToken = _abortTokenSrc.Token,
                EnsureOrdered = false,
                MaxDegreeOfParallelism = 1,
            });

            _compWriteChunk = new ActionBlock<LZ4ParallelCompressJob>(WriterProc, new ExecutionDataflowBlockOptions()
            {
                CancellationToken = _abortTokenSrc.Token,
                BoundedCapacity = maxWaitingJobs,
                EnsureOrdered = true,
                MaxDegreeOfParallelism = 1,
            });

            DataflowLinkOptions linkOptions = new DataflowLinkOptions
            {
                PropagateCompletion = false,
                Append = true,
            };

            _compWorkChunk.LinkTo(_compSortChunk, linkOptions);
        }
        #endregion

        #region Disposable Pattern
        ~LZ4FrameParallelStream()
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

                // Dispose unmanaged resources, and set large fields to null.
                try
                {
                    // Compress
                    if (_mainCctx != IntPtr.Zero)
                        FinishWrite();
                }
                finally
                {
                    if (_mainCctx != IntPtr.Zero)
                    { // Compress
                        nuint ret = LZ4Init.Lib.FreeFrameCompressContext!(_mainCctx);
                        LZ4FrameException.CheckReturnValue(ret);

                        _mainCctx = IntPtr.Zero;
                    }

                    if (_xxh32 != null)
                    {
                        _xxh32.Dispose();
                        _xxh32 = null;
                    }

                    if (_nextDictBuffer != null)
                    {
                        _nextDictBuffer.Dispose();
                        _nextDictBuffer = null;
                    }
                    _inputBuffer.Dispose();

                    _abortTokenSrc.Dispose();

                    _targetWrittenEvent.Dispose();

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

            // Dispose the base class
            base.Dispose(disposing);
        }
        #endregion

        #region Main Thread - Read/Write
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Read() not supported on compression.");
        }

        /// <inheritdoc />
#if NETCOREAPP
        public override unsafe int Read(Span<byte> span)
#else
        public unsafe int Read(Span<byte> span)
#endif
        {
            throw new NotSupportedException("Read() not supported on compression.");
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            LZ4FrameStream.CheckReadWriteArgs(buffer, offset, count);
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
            if (BaseStream == null)
                throw new ObjectDisposedException(nameof(LZ4FrameStream));
            if (span.Length == 0)
                return;

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();

            // Throw if the instance was already aborted.
            if (_abortTokenSrc.IsCancellationRequested)
                throw new InvalidOperationException("This stream had been aborted.");

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

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();

            // Wait until the output is ready
            WaitWriteJobComplete(_latestSeq, _writeTimeout);

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();
        }

        private void EnqueueInputBuffer(bool isFinal)
        {
            if (_finalEnqueued)
                throw new InvalidOperationException("The final block has already been enqueued.");

            // Throw if the instance was already aborted.
            if (_abortTokenSrc.IsCancellationRequested)
                throw new InvalidOperationException("This stream had been aborted.");

            // Do nothing if all compression is already done.
            if (_compSortChunk.Completion.IsCompleted)
                return;

            Debug.Assert(_inSeq == 0 && _nextDictBuffer == null ||
                _inSeq != 0 && _nextDictBuffer != null && !_nextDictBuffer.Disposed);

            _finalEnqueued |= isFinal;

            // _refCount of job.InBuffer and _nextDictBuffer is increased in constructor call.
            LZ4ParallelCompressJob job = new LZ4ParallelCompressJob(_pool, _inSeq, _inBlockSize, _nextDictBuffer, _outBlockSize);
            _latestSeq = job.Seq;
            _inSeq += 1;

            if (isFinal)
                job.IsLastBlock = true;

            // Calculate xxh32
            _xxh32?.Write(_inputBuffer.ReadablePortionSpan);
            job.InBuffer.Write(_inputBuffer.ReadablePortionSpan, true);

            // Prepare next dictionary buffer (pass for final block)
            if (_isBlockLinked)
            {
                if (isFinal)
                {
                    _nextDictBuffer = null; // No longer required
                }
                else if (LZ4ParallelCompressJob.DictWindowSize <= job.InBuffer.ReadableSize)
                {
                    _nextDictBuffer = job.InBuffer.AcquireRef();
                }
                else
                { // next input is less than 64K -> retain last 64K
                    ReferableBuffer copyDictBuffer = new ReferableBuffer(_pool, LZ4ParallelCompressJob.DictWindowSize);
                    if (_nextDictBuffer == null)
                    { // First block, but the input is less than 64K -> copy the full input
                        copyDictBuffer.Write(job.InBuffer.ReadablePortionSpan);
                    }
                    else
                    { // Normal block, source 64K from the previous input + current input
                        // DO NOT USE _nextPrefixBuffer.DataStartIdx! It may be changed anytime because the buffer is shared.
                        int copyLastDictSize = LZ4ParallelCompressJob.DictWindowSize - job.InBuffer.ReadableSize;

                        ReadOnlySpan<byte> lastDictSpan;
                        if (copyLastDictSize <= _nextDictBuffer.DataEndIdx)
                            lastDictSpan = _nextDictBuffer.Buf.AsSpan(_nextDictBuffer.DataEndIdx - copyLastDictSize, copyLastDictSize);
                        else
                            lastDictSpan = _nextDictBuffer.Span;
                        copyDictBuffer.Write(lastDictSpan);

                        // Release previous ref of the _nextDictBuffer.
                        _nextDictBuffer.ReleaseRef();

                        // Copy current input, acheiving full 64K.
                        copyDictBuffer.Write(job.InBuffer.ReadablePortionSpan);

                        Debug.Assert(copyDictBuffer.DataEndIdx == LZ4ParallelCompressJob.DictWindowSize);
                    }
                    _nextDictBuffer = copyDictBuffer.AcquireRef();
                }
            }

            _inputBuffer.Clear();
            _compWorkChunk.SendAsync(job).Wait();
        }
        #endregion

        #region MainThread - Flush, Abort, FinishWrite
        private unsafe void FinishWrite()
        {
            // Check exceptions in Task instances.
            CheckBackgroundExceptions();

            // Flush and enqueue a final block with remaining buffer.
            if (!_abortTokenSrc.IsCancellationRequested)
                EnqueueInputBuffer(true);

            // Wait until dataflow completes its job.
            Task.WaitAll(_compWorkChunk.Completion,
                _compSortChunk.Completion,
                _compWriteChunk.Completion);

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();
        }

        /// <inheritdoc />
        public override unsafe void Flush()
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));
            if (BaseStream == null)
                throw new ObjectDisposedException(nameof(LZ4FrameStream));

            // Throw if the instance was already aborted.
            if (_abortTokenSrc.IsCancellationRequested)
                throw new InvalidOperationException("This stream had been aborted.");

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();

            // LZ4 parallel compression always auto-flush the output.
            // No need to call LZ4F_flush() here.

            // Flush remaining input
            if (!_inputBuffer.IsEmpty)
                EnqueueInputBuffer(false);

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();

            // Block until write is complete
            WaitWriteJobComplete(_latestSeq, null);

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();

            // Flush the remaining compressed data into BaseStream
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
            Debug.Assert(_waitSeq <= checkSeqNum);

            Interlocked.Exchange(ref _waitSeq, checkSeqNum);

            // Wait until writerThread processes block of _waitSeq.
            if (waitMax == null) // Block indefinitely
                _targetWrittenEvent.WaitOne();
            else // Block for a finite time
                _targetWrittenEvent.WaitOne(waitMax.Value);
        }
        #endregion

        #region CompressProc
        internal unsafe LZ4ParallelCompressJob CompressProc(LZ4ParallelCompressJob job)
        {
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));
            if (BaseStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            IntPtr compCctx = IntPtr.Zero;
            try
            {
                try
                {
                    if (0 < job.InBuffer.ReadableSize)
                    {
                        // Prepare cctx
                        nuint ret = LZ4Init.Lib.CreateFrameCompressContext!(ref compCctx, LZ4FrameStream.FrameVersion);
                        LZ4FrameException.CheckReturnValue(ret);

                        // Init and nullify frame header
                        // - Do not increase job.OutBuffer.DataEndIdx
                        if (job.DictBuffer == null)
                        { // First job
                            nuint headerSizeVal;
                            fixed (byte* outPtr = job.OutBuffer.Buf)
                            {
                                headerSizeVal = LZ4Init.Lib.FrameCompressBegin!(compCctx, outPtr, (nuint)job.OutBuffer.Capacity, _workCompPrefs);
                            }
                            LZ4FrameException.CheckReturnValue(headerSizeVal);
                            Debug.Assert(0 <= headerSizeVal && headerSizeVal <= int.MaxValue);
                        }
                        else
                        { // Put previous 64KB as a dictionary
                            int dictSize = Math.Min(job.DictBuffer.DataEndIdx, LZ4ParallelCompressJob.DictWindowSize);
                            int dictStartPos = job.DictBuffer.DataEndIdx - dictSize;

                            nuint headerSizeVal;
                            fixed (byte* outPtr = job.OutBuffer.Buf)
                            fixed (byte* dictPtr = job.DictBuffer.Buf)
                            {
                                headerSizeVal = LZ4Init.Lib.FrameCompressBeginUsingDict!(compCctx, outPtr, (nuint)job.OutBuffer.Capacity, dictPtr + dictStartPos, (nuint)dictSize, _workCompPrefs);
                            }
                            LZ4FrameException.CheckReturnValue(headerSizeVal);
                            Debug.Assert(0 <= headerSizeVal && headerSizeVal < int.MaxValue);
                        }

                        // Compress actual data
                        nuint outSizeVal;
                        fixed (byte* inPtr = job.InBuffer.Buf)
                        fixed (byte* outPtr = job.OutBuffer.Buf)
                        {
                            outSizeVal = LZ4Init.Lib.FrameCompressUpdate!(compCctx, outPtr, (nuint)job.OutBuffer.Capacity, inPtr, (nuint)job.InBuffer.DataEndIdx, _frameCompOpts);
                        }
                        LZ4FrameException.CheckReturnValue(outSizeVal);

                        Debug.Assert(outSizeVal <= int.MaxValue, "BufferSize should be <=2GB");
                        job.OutBuffer.DataEndIdx += (int)outSizeVal;
                    }
                }
                finally
                {
                    if (job.IsLastBlock)
                        _compWorkChunk.Complete();

                    job.DictBuffer?.ReleaseRef();

                    if (compCctx != IntPtr.Zero)
                    {
                        nuint ret = LZ4Init.Lib.FreeFrameCompressContext!(compCctx);
                        LZ4FrameException.CheckReturnValue(ret);

                        compCctx = IntPtr.Zero;
                    }
                }
            }
            catch (Exception e)
            {
                _abortTokenSrc.Cancel();
                _taskExcepts.Add(e);
            }

#if DEBUG_PARALLEL
            Console.WriteLine($"-- compressed: {job}");
#endif

            return job;
        }
        #endregion

        #region WriteSortProc
        internal async void WriteSortProc(LZ4ParallelCompressJob item)
        {
            try
            {
                // Receive a LZ4ParallelCompressJob (which completed compressing), then put it into sorted list
                _outSet.Add(item);

                // Check if the jobs of right seq is available.
                // If available, post all of the designated jobs.
                while (0 < _outSet.Count)
                {
                    LZ4ParallelCompressJob? outJob = _outSet.FirstOrDefault(x => x.Seq == _outSeq);
                    if (outJob == null)
                        break;
                    _outSet.Remove(outJob);

                    _outSeq += 1;
                    bool isLastBlock = outJob.IsLastBlock;

                    await _compWriteChunk.SendAsync(outJob);

                    if (isLastBlock)
                        _compSortChunk.Complete();
                }
            }
            catch (Exception e)
            {
                _abortTokenSrc.Cancel();
                _taskExcepts.Add(e);
            }
        }
        #endregion

        #region WriterProc
        internal unsafe void WriterProc(LZ4ParallelCompressJob job)
        {
            try
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
                    TotalIn += job.InBuffer.DataEndIdx;
                    TotalOut += job.OutBuffer.DataEndIdx;

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
            catch (Exception e)
            {
                _abortTokenSrc.Cancel();
                _targetWrittenEvent.Set();
                _taskExcepts.Add(e);
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

            using (PooledBuffer outBuf = new PooledBuffer(_pool, _outBlockSize))
            {
                nuint headerSizeVal;
                fixed (byte* dest = outBuf.Buf)
                {
                    headerSizeVal = LZ4Init.Lib.FrameCompressBegin!(_mainCctx, dest, (nuint)_inBlockSize, prefs);
                    LZ4FrameException.CheckReturnValue(headerSizeVal);
                }

                Debug.Assert(0 <= headerSizeVal && headerSizeVal <= int.MaxValue);

                int headerSize = (int)headerSizeVal;
                BaseStream.Write(outBuf.Buf, 0, headerSize);
                TotalOut += headerSize;
            }
        }

        private unsafe void WriteTrailer()
        {
            if (BaseStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");
            if (LZ4Init.Lib == null)
                throw new ObjectDisposedException(nameof(LZ4Init));

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

        #region Exception Handling
        internal void CheckBackgroundExceptions()
        {
            AggregateException?[] rawExcepts =
            [
                _compWorkChunk.Completion.Exception,
                _compSortChunk.Completion.Exception,
                _compWriteChunk.Completion.Exception
            ];

            List<AggregateException> aggExcepts = rawExcepts.Where(x => x != null).Select(x => x!).ToList();

            // No exceptions has been fired -> return peacefully.
            if (aggExcepts.Count == 0)
            {
                if (_taskExcepts.Count == 0)
                {
                    return;
                }
                else
                {
                    AggregateException ae = new AggregateException(_taskExcepts);
                    _taskExcepts.Clear();
                    throw ae;
                }
            }

            // Preserve AggregateException if only one Dataflow Block has AggregateException.
            if (aggExcepts.Count == 1)
            {
                if (_taskExcepts.Count == 0)
                    throw aggExcepts.First(x => x != null);
            }

            // Merge instances of AggregateException.
            List<Exception> innerExcepts = [];
            foreach (AggregateException ae in aggExcepts)
                innerExcepts.AddRange(ae.InnerExceptions);
            innerExcepts.AddRange(_taskExcepts);
            _taskExcepts.Clear();

            Debug.Assert(0 < innerExcepts.Count);
            throw new AggregateException(innerExcepts);
        }
        #endregion

        #region Stream Properties
        /// <inheritdoc />
        public override bool CanRead => false;
        /// <inheritdoc />
        public override bool CanWrite => BaseStream != null && BaseStream.CanWrite && !_finalEnqueued && !_abortTokenSrc.IsCancellationRequested;
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
                if (TotalOut == 0)
                    return 0;
                return 100 - TotalIn * 100.0 / TotalOut;
            }
        }
        #endregion
    }
    #endregion
}
