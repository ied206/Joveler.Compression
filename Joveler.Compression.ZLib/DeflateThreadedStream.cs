// #define DEBUG_PARALLEL

/*   
    Written by Hajin Jang
    Copyright (C) 2024-present Hajin Jang

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

using Joveler.Compression.ZLib.Buffer;
using Joveler.Compression.ZLib.Checksum;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Joveler.Compression.ZLib
{
    public sealed class ZLibThreadedCompressOptions
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
        public int BlockSize { get; set; } = DeflateThreadedStream.DefaultBlockSize;
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
        /// Whether to leave the base stream object open after disposing the zlib stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
    }

    internal sealed class DeflateThreadedStream : Stream
    {
        #region Fields and Properties
        private readonly ZLibOperateFormat _format;
        private readonly ZLibCompLevel _compLevel;
        private readonly ZLibWindowBits _windowBits;
        private readonly TimeSpan? _writeTimeout;
        private readonly bool _leaveOpen;
        private bool _disposed = false;

        public Stream? BaseStream { get; private set; }
        public long TotalIn => _totalIn;
        private long _totalIn = 0;
        private void AddTotalIn(long value) => Interlocked.Add(ref _totalIn, value);
        public long TotalOut => _totalOut;
        private long _totalOut = 0;
        private void AddTotalOut(long value) => Interlocked.Add(ref _totalOut, value);

        // Multithread Parallel Compress
        private readonly Thread[] _workerThreads;
        private readonly CompressThreadProc[] _workerThreadProcs;

        private readonly Thread _writerThread;
        private readonly WriterThreadProc _writerThreadProc;
        
        private readonly object _abortLock = new object();
        private bool _abortedFlag = false;
        public bool IsAborted
        {
            get
            {
                lock (_abortLock)
                    return _abortedFlag;
            }
        }

        private readonly object _backgroundExceptionsLock = new object();
        private readonly List<Exception> _backgroundExceptions = new List<Exception>();
        /// <summary>
        /// Returns true if any exception has been fired in the background threads.
        /// </summary>
        public bool HasBackgroundExceptions
        {
            get
            {
                lock (_backgroundExceptionsLock)
                    return 0 < _backgroundExceptions.Count;
            }
        }
        /// <summary>
        /// Return the exceptions occured in the background threads.
        /// </summary>
        /// <remarks>
        /// Returned exceptions are cleared from the internal list.
        /// </remarks>
        public Exception[] BackgroundExceptions
        {
            get
            {
                lock (_backgroundExceptionsLock)
                {
                    Exception[] execpts = _backgroundExceptions.ToArray();
                    _backgroundExceptions.Clear();
                    return execpts;
                }
            }
        }

        /// <summary>
        /// Also represents the number of blocks passed to the worker threads.
        /// </summary>
        private long _inSeq = 0;
        private long _waitSeq = ZLibParallelCompressJob.WaitSeqInit;
        public long WaitSeq => Interlocked.Read(ref _waitSeq);

        private readonly ManualResetEvent _targetWrittenEvent = new ManualResetEvent(true);
        private bool _finalEnqueued = false;
        private readonly int _inBlockSize;
        private readonly int _outBlockSize;
        private static int CalcOutBlockSize(int rawBlockSize) => rawBlockSize + (rawBlockSize >> 3); // 9/8 * rawBlockSize

        private readonly ConcurrentQueue<ZLibParallelCompressJob> _inQueue;
        private readonly object _outListLock;
        private readonly LinkedList<ZLibParallelCompressJob> _outList;

        private readonly PooledBuffer _inputBuffer;
        /// <summary>
        /// Its ref is acquired twice in EnqueueInputData, and released in CompressThreadMain/WriterThreadMain.
        /// </summary>
        private ReferableBuffer? _nextDictBuffer;

        private readonly ArrayPool<byte> _pool;

        /// <summary>
        /// Default Block Size 
        /// </summary>
        internal const int DefaultBlockSize = 128 * 1024; // pigz uses 128KB for block size

        private static ZLibChecksumBase<uint>? FormatChecksum(ZLibOperateFormat format)
        {
            return format switch
            {
                ZLibOperateFormat.Deflate => null,
                ZLibOperateFormat.ZLib => new Adler32Checksum(),
                ZLibOperateFormat.GZip => new Crc32Checksum(),
                _ => throw new InvalidOperationException($"Compression is not supported for [{format}] format."),
            };
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Create parallel-compressing DeflateStream.
        /// </summary>
        public DeflateThreadedStream(Stream baseStream, ZLibThreadedCompressOptions pcompOpts, ZLibOperateFormat format)
        {
            ZLibInit.Manager.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));

            _disposed = false;
            _leaveOpen = pcompOpts.LeaveOpen;

            _format = format;
            _compLevel = pcompOpts.Level;
            _windowBits = pcompOpts.WindowBits;
            _writeTimeout = pcompOpts.WriteTimeout;

            int threadCount = pcompOpts.Threads;
            if (threadCount < 0)
                throw new ArgumentOutOfRangeException(nameof(pcompOpts.Threads), "Thread count must be greater than or equal to 0(auto).");
            else if (threadCount == 0)
                threadCount = Environment.ProcessorCount;

            _workerThreads = new Thread[threadCount];
            _workerThreadProcs = new CompressThreadProc[threadCount];

            _inQueue = new ConcurrentQueue<ZLibParallelCompressJob>();
            _outListLock = new object();
            _outList = new LinkedList<ZLibParallelCompressJob>();

            // Calculate the buffer size
            CheckBlockSize(pcompOpts.BlockSize);
            _inBlockSize = pcompOpts.BlockSize;
            _outBlockSize = CalcOutBlockSize(pcompOpts.BlockSize);
            Debug.Assert(_inBlockSize <= _outBlockSize);

            _pool = pcompOpts.BufferPool ?? ArrayPool<byte>.Shared;
            _inputBuffer = new PooledBuffer(_pool, _inBlockSize);
            _nextDictBuffer = null;

            // Write the header
            WriteHeader();

            // Create worker threads
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            for (int i = 0; i < _workerThreads.Length; i++)
            {
                CompressThreadProc compessThreadProc = new CompressThreadProc(this, i);
                _workerThreadProcs[i] = compessThreadProc;

                Thread workerThread = new Thread(compessThreadProc.CompressThreadMain);
                workerThread.Name = $"ZLibParallelWorkerThread_{i:X2}_{mainThreadId:X2}";
                workerThread.Start();
                _workerThreads[i] = workerThread;
            }

            // Create write thread
            _writerThreadProc = new WriterThreadProc(this);
            _writerThread = new Thread(_writerThreadProc.WriterThreadMain);
            _writerThread.Name = $"ZLibParallelWriterThread_{mainThreadId:X2}";
            _writerThread.Start();
        }
        #endregion

        #region Disposable Pattern
        ~DeflateThreadedStream()
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

                // If ZLibException has been fired, throw exception after dispoing the resources.
                try
                { // Finalize compression
                    FinishWrite();
                }
                finally
                {
                    // Dispose unmanaged resources, and set large fields to null.
                    if (BaseStream != null)
                    {
                        if (!_leaveOpen)
                            BaseStream.Dispose();
                        BaseStream = null;
                    }

                    while (_inQueue.TryDequeue(out ZLibParallelCompressJob? inJob))
                        inJob?.Dispose();

                    lock (_outListLock)
                    {
                        foreach (ZLibParallelCompressJob outJob in _outList)
                            outJob.Dispose();
                        _outList.Clear();
                    }

                    for (int i = 0; i <  _workerThreads.Length; i++)
                    {
                        Thread thread = _workerThreads[i];
                        CompressThreadProc threadProc = _workerThreadProcs[i];
                        thread.Join();
                        threadProc.Dispose();
                    }
                    _writerThread.Join();
                    _writerThreadProc.Dispose();

                    _nextDictBuffer?.Dispose();
                    _inputBuffer.Dispose();

                    _targetWrittenEvent.Dispose();

                    _disposed = true;
                }
            }

            // Dispose the base class
            base.Dispose(disposing);
        }
        #endregion

        #region MainThread - Write: push raw data into the worker threads, and wait until getting the output
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        { // For Decompress
            throw new NotSupportedException("Read() not supported on compression");
        }

        /// <inheritdoc />
#if NETCOREAPP3_1
        public override unsafe int Read(Span<byte> span)
#else
        public unsafe int Read(Span<byte> span)
#endif
        { // For Decompress
            throw new NotSupportedException("Read() not supported on compression");
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            ZLibLoader.CheckReadWriteArgs(buffer, offset, count);
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
            // Throw if any exception has occured in background threads.
            if (HasBackgroundExceptions)
                throw new AggregateException(BackgroundExceptions);

            // Do nothing if the instance was already aborted.
            if (IsAborted)
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
            SetWorkerThreadReadSignal();

            // Wait until the output is ready
            WaitWriteComplete(_inSeq, _writeTimeout);

            // Check for a last time.
            if (HasBackgroundExceptions)
                throw new AggregateException(BackgroundExceptions);
        }

        private void EnqueueInputBuffer(bool isFinal)
        {
            if (_finalEnqueued)
                throw new InvalidOperationException("The final block has already been enqueued.");

            // [RefCount]
            // First   block: _nextDictBuffer == null
            // Nor/Fin block: _nextDictBuffer != null, 1 <= _nextDictBuffer._refCount <= 2

            Debug.Assert(_inSeq == 0 && _nextDictBuffer == null ||
                _inSeq != 0 && _nextDictBuffer != null && !_nextDictBuffer.Disposed);

            _finalEnqueued |= isFinal;

            // _refCount of job.InBuffer and _nextDictBuffer is increased in constructor call.
            ZLibParallelCompressJob job = new ZLibParallelCompressJob(_pool, _inSeq, _inBlockSize, _nextDictBuffer, _outBlockSize);
            _inSeq += 1;

            // [RefCount]
            // First   block: job.InBuffer._refCount == 1, (job.DictBuffer == _nextDictBuffer) == null
            // Nor/Fin block: job.InBuffer._refCount == 1, 2 <= (job.DictBuffer == _nextDictBuffer)._refCount <= 3

            if (isFinal)
                job.IsLastBlock = true;

            job.InBuffer.Write(_inputBuffer.ReadablePortionSpan, true);
            job.RawInputSize = job.InBuffer.DataEndIdx;

            // Prepare next dictionary buffer (pass for final block)
            if (isFinal)
            {
                _nextDictBuffer = null; // No longer required
            }
            else if (ZLibParallelCompressJob.DictWindowSize <= job.InBuffer.ReadableSize)
            {
                _nextDictBuffer = job.InBuffer.AcquireRef();
            }
            else
            { // next input is less than 32K -> retain last 32K
                ReferableBuffer copyDictBuffer = new ReferableBuffer(_pool, ZLibParallelCompressJob.DictWindowSize);
                if (_nextDictBuffer == null)
                { // First block, but the input is less than 32K -> copy the full input
                    copyDictBuffer.Write(job.InBuffer.ReadablePortionSpan);
                }
                else
                { // Normal block, source 32K from the previous input + current input
                    // DO NOT USE _nextDictBuffer.DataStartIdx! It may be changed anytime because the buffer is shared.
                    int copyLastDictSize = ZLibParallelCompressJob.DictWindowSize - job.InBuffer.ReadableSize;
                    ReadOnlySpan<byte> lastDictSpan = _nextDictBuffer.Buf.AsSpan(_nextDictBuffer.DataEndIdx - copyLastDictSize, copyLastDictSize);
                    copyDictBuffer.Write(lastDictSpan);

                    // Release previous ref of the _nextDictBuffer.
                    _nextDictBuffer.ReleaseRef();

                    // Copy current input, acheiving full 32K.
                    copyDictBuffer.Write(job.InBuffer.ReadablePortionSpan);

                    Debug.Assert(copyDictBuffer.DataEndIdx == ZLibParallelCompressJob.DictWindowSize);
                }
                _nextDictBuffer = copyDictBuffer.AcquireRef();
            }

            // [RefCount]
            // First  block: (job.InBuffer == _nextDictBuffer)._refCount == 2
            // Normal block: (<=32K) (job.InBuffer == _nextDictBuffer)._refCount == 2
            // Normal block; (32K<): job.InBuffer._refCount == 1, _nextDictBuffer._refCount == 1
            // Final  block: job.InBuffer._refCount == 1, _nextDictBuffer == null

            _inputBuffer.Clear();
            _inQueue.Enqueue(job);
        }

        private void EnqueueInputEof()
        {
            // Enqueue an EOF block with empty buffer per thread
            // EOF block is only a simple marker to terminate the worker threads.
            for (int i = 0; i < _workerThreads.Length; i++)
            { // Worker threads will terminate when eof block appears.
                ZLibParallelCompressJob eofJob = new ZLibParallelCompressJob(_pool, ZLibParallelCompressJob.EofBlockSeq);
                _inQueue.Enqueue(eofJob);

                // [RefCount]
                // EOF block: job.InBuffer._refCount == 1, job.DictBuffer = null
            }
        }

        private void EnqueueOutList(ZLibParallelCompressJob job)
        {
            lock (_outListLock)
            {
                LinkedListNode<ZLibParallelCompressJob>? node = _outList.First;

                while (node != null)
                {
                    if (job.Seq < node.Value.Seq)
                        break;
                    node = node.Next;
                }

                if (node == null)
                    _outList.AddLast(job);
                else
                    _outList.AddBefore(node, job);
            }

            // Signal the write thread to resume.
            _writerThreadProc.WriteSignal.Set();
        }
        #endregion

        #region MainThread - Flush, Abort, FinalizeStream
        /// <inheritdoc />
        public override void Flush()
        {
            // Throw if any exception has occured in background threads.
            if (HasBackgroundExceptions)
                throw new AggregateException(BackgroundExceptions);

            // If aborted, return immediately
            if (IsAborted)
                return;

            if (BaseStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            Debug.Assert(_inputBuffer.DataStartIdx == 0);

            // Flush the remaining input buffer into compress worker threads.
            if (!_inputBuffer.IsEmpty)
            {
                EnqueueInputBuffer(false);

                // Alert the worker thread to un-pause compressing
                SetWorkerThreadReadSignal();
            }

            // Wait until all threads are idle.
            WaitWriteComplete(_inSeq, null);

            // Flush the remaining compressed data into BaseStream
            BaseStream.Flush();

            // Check for a last time.
            if (HasBackgroundExceptions)
                throw new AggregateException(BackgroundExceptions);
        }

        public void Abort()
        {
            // Throw if any exception has occured in background threads.
            if (HasBackgroundExceptions)
                throw new AggregateException(BackgroundExceptions);

            // If aborted, return immediately
            if (IsAborted)
                return;

            // Signal the workerThreads and writerThread to abort.
            // If threads are waiting for the read/write signal, release them.
            SignalAbort();

            // Check one more time, and check here before thread join to avoid unknown deadlock.
            if (HasBackgroundExceptions)
                throw new AggregateException(BackgroundExceptions);

            // Wait until all worker threads to finish
            foreach (Thread thread in _workerThreads)
                thread.Join();

            // Now all worker threads are terminated.
            // Wait until writerThread finishes.
            _writerThread.Join();

            // Check for a last time.
            if (HasBackgroundExceptions)
                throw new AggregateException(BackgroundExceptions);
        }

        private void SignalAbort()
        {
            // Set abort flag
            lock (_abortLock)
                _abortedFlag = true;

            // If threads are waiting for the read/write signal, release them.
            SetWorkerThreadReadSignal();
            _writerThreadProc.WriteSignal.Set();
        }

        private void FinishWrite()
        {
            // Throw if any exception has occured in background threads.
            if (HasBackgroundExceptions)
                throw new AggregateException(BackgroundExceptions);

            // If aborted, return immediately
            if (IsAborted)
                return;

            // flush and enqueue a final block with remaining buffer to run ZLibFlush.Finish.
            EnqueueInputBuffer(true);

            // Enqueue an EOF block with empty buffer per thread
            // EOF block is only a simple marker to terminate the worker threads.
            EnqueueInputEof();

            // Signal to the worker threads to finalize the compression
            SetWorkerThreadReadSignal();

            // Check one more time, and check here before thread join to avoid unknown deadlock.
            if (HasBackgroundExceptions)
                throw new AggregateException(BackgroundExceptions);

            // Wait until all worker threads to finish
            foreach (Thread thread in _workerThreads)
                thread.Join();

            // Now final block is being processed by the writerThread.
            // Wait until writerThread finishes.
            _writerThread.Join();

            // Check for a last time.
            if (HasBackgroundExceptions)
                throw new AggregateException(BackgroundExceptions);
        }

        private void WaitWriteComplete(long checkSeqNum, TimeSpan? waitMax)
        {
            Debug.Assert(0 <= checkSeqNum);
            Debug.Assert(_waitSeq <= checkSeqNum);

            Interlocked.Exchange(ref _waitSeq, checkSeqNum);

            // Throw if any exception has occured in background threads
            if (HasBackgroundExceptions)
                throw new AggregateException(BackgroundExceptions);

            // Wait until writerThread processes block of _waitSeq.
            if (waitMax == null) // Block indefinitely
                _targetWrittenEvent.WaitOne();
            else // Block for a finite time
                _targetWrittenEvent.WaitOne(waitMax.Value);
        }
        #endregion

        #region class CompressThreadProc
        internal sealed class CompressThreadProc : IDisposable
        {
            private readonly DeflateThreadedStream _owner;
            private readonly int _threadId;

            public readonly AutoResetEvent ReadSignal = new AutoResetEvent(false);
            public readonly ManualResetEvent WaitingSignal = new ManualResetEvent(false);

            private readonly ZLibChecksumBase<uint>? _blockChecksum;

            private ZStreamBase? _zs;
            private GCHandle _zsPin;

            private bool _disposed = false;

            public CompressThreadProc(DeflateThreadedStream owner, int threadId)
            {
                _owner = owner;
                _threadId = threadId;
                _blockChecksum = FormatChecksum(owner._format);

                _zs = ZLibInit.Lib.CreateZStream();
                _zsPin = GCHandle.Alloc(_zs, GCHandleType.Pinned);

                // Always initialize zstream with -15 windowBits to use raw deflate stream.
                int windowBits = DeflateStreamBase.ProcessFormatWindowBits(_owner._windowBits, ZLibStreamOperateMode.ParallelCompress, ZLibOperateFormat.Deflate);
                ZLibRet ret = ZLibInit.Lib.NativeAbi.DeflateInit(_zs, _owner._compLevel, windowBits, ZLibMemLevel.Default);
                ZLibException.CheckReturnValue(ret, _zs);
            }

            ~CompressThreadProc()
            {
                Dispose(false);
            }

            public unsafe void CompressThreadMain()
            {
                try
                {
                    if (_zs == null)
                        throw new ObjectDisposedException($"[{nameof(_zs)}] is null.");

                    ZLibRet ret;
                    bool exitLoop = false;
                    while (true)
                    {
                        // Signal that this workerThread is waiting for the next job
                        // Wait for the next job to come in
                        WaitHandle.SignalAndWait(WaitingSignal, ReadSignal);

                        if (_owner.IsAborted)
                            break;

                        // Reset the waiting signal
                        WaitingSignal.Reset();

                        // Loop until the input queue is empty
                        while (_owner._inQueue.TryDequeue(out ZLibParallelCompressJob? job) && job != null)
                        {
                            // If the input is the EOF block, break the loop immediately
                            if (job.IsEofBlock)
                            {
                                // [RefCount]
                                // EOF block: job.InBuffer._refCount == 1,    job.DictBuffer = null

                                job.Dispose();

                                // [RefCount]
                                // EOF block: job.InBuffer._refCount == 0(D), job.DictBuffer = null

                                exitLoop = true;
                                break;
                            }

                            try
                            {
                                // [RefCount]
                                // First  block:          1 <= job.InBuffer._refCount <= 3, job.DictBuffer == null
                                // Nor/Fin block (32K<=): 1 <= job.InBuffer._refCount <= 3, 1 <= job.DictBuffer._refCount <= 3
                                // Nor/Fin block (<32K):  job.InBuffer._refCount == 1,      job.DictBuffer._refCount == 1

                                // [Stage 02] Reset the zstream, and set the compression level again
                                ret = ZLibInit.Lib.NativeAbi.DeflateReset(_zs);
                                ZLibException.CheckReturnValue(ret, _zs);
                                ret = ZLibInit.Lib.NativeAbi.DeflateParams(_zs, (int)_owner._compLevel, (int)ZLibCompStrategy.Default);
                                ZLibException.CheckReturnValue(ret, _zs);

                                if (job.DictBuffer != null)
                                {
                                    // [IMPORTANT]
                                    // job.DictBuffer is a reference of last job's InBuffer, so DictBuffer.{DataStartIdx,DataSize} must be ignored.
                                    // Use Buf directly to avoid corruping/corrupted by DataStartIdx.
                                    // {In,Dict}Buffer.DataEndIdx is not changed by another worker thread, so it is safe to use.
                                    Debug.Assert(!job.DictBuffer.Disposed);

                                    int dictSize = Math.Min(job.DictBuffer.DataEndIdx, ZLibParallelCompressJob.DictWindowSize);
                                    int dictStartPos = job.DictBuffer.DataEndIdx - dictSize;

                                    // [Stage 03] Set dictionary (last 32KB of the previous input)
                                    fixed (byte* dictPtr = job.DictBuffer.Buf)
                                    {
                                        ret = ZLibInit.Lib.NativeAbi.DeflateSetDictionary(_zs, dictPtr + dictStartPos, (uint)dictSize);
                                        ZLibException.CheckReturnValue(ret, _zs);
                                    }
                                }

                                // [Stage 10] Calculate checksum
                                if (_blockChecksum != null)
                                {
                                    Debug.Assert(!job.InBuffer.Disposed);
                                    Debug.Assert(job.InBuffer.DataStartIdx == 0);
                                    Debug.Assert(job.InBuffer.DataEndIdx == job.RawInputSize);

                                    _blockChecksum.Reset();
                                    job.Checksum = _blockChecksum.Append(job.InBuffer.Buf, 0, job.InBuffer.DataEndIdx);
                                }

                                // [Stage 11] Compress (or finish) the input block
                                int bytesRead = 0;
                                if (!job.IsLastBlock)
                                { // Deflated block will end on a byte boundary, using a sync marker if necessary (SyncFlush)
                                  // ADVACNED: Bit-level output manipulation.
                                  // SIMPLE: In pre zlib 1.2.6, just call DeflateBlock once with ZLibFlush.SyncFlush.

                                    // After Z_BLOCK, Up to 7 bits of output data are waiting to be written.
                                    bytesRead = DeflateBlock(job, bytesRead, ZLibFlush.Block);

                                    // How many bits are waiting to be written?
                                    int bits = 0;
                                    ret = ZLibInit.Lib.NativeAbi.DeflatePending(_zs, null, &bits);
                                    ZLibException.CheckReturnValue(ret, _zs);

                                    // Add enough empty blocks to get to a byte boundary
                                    if (0 < (bits & 1)) // 1 bit is waiting to be written
                                    { // Flush the bit-level boundary
                                        bytesRead = DeflateBlock(job, bytesRead, ZLibFlush.SyncFlush);
                                    }
                                    else if (0 < (bits & 7)) // 3 bits or more are waiting to be written
                                    { // Add static empty blocks
                                        do
                                        { // Insert bits to next output block
                                          // Next output will start with bits leftover from a previous deflate() call.
                                          // 10 bits
                                            ret = ZLibInit.Lib.NativeAbi.DeflatePrime(_zs, 10, 2);
                                            ZLibException.CheckReturnValue(ret, _zs);

                                            // Still are 3 bits or more waiting to be written?
                                            ret = ZLibInit.Lib.NativeAbi.DeflatePending(_zs, null, &bits);
                                            ZLibException.CheckReturnValue(ret, _zs);
                                        }
                                        while (0 < (bits & 7));
                                        bytesRead = DeflateBlock(job, bytesRead, ZLibFlush.Block);
                                    }
                                }
                                else
                                { // Finish the deflate stream
                                    bytesRead = DeflateBlockFinish(job, bytesRead);
                                }

#if DEBUG_PARALLEL
                                Console.WriteLine($"-- compressed (#{job.SeqNum}) : last=[{job.IsLastBlock}] in=[{job.InBuffer}] dict=[{job.DictBuffer}] out=[{job.OutBuffer}] ");
#endif

                                Debug.Assert(bytesRead == job.RawInputSize && job.RawInputSize == job.InBuffer.DataEndIdx);

                                // [Stage 12] Insert compressed data to linked list
                                _owner.EnqueueOutList(job);
                            }
                            finally
                            {
                                // Free unnecessary resources
                                job.DictBuffer?.ReleaseRef();

                                // [RefCount]
                                // First  block:          1 <= job.InBuffer._refCount <= 3, job.DictBuffer == null
                                // Nor/Fin block (32K<=): 1 <= job.InBuffer._refCount <= 3, 0(D) <= job.DictBuffer._refCount <= 2
                                // Nor/Fin block (32K<):  job.InBuffer._refCount == 1,      job.DictBuffer._refCount == 0(D)
                            }

                            // If the abort signal is set, break the loop
                            if (_owner.IsAborted)
                            {
                                exitLoop = true;
                                break;
                            }
                        }

                        if (exitLoop)
                            break;
                    }

                    WaitingSignal.Set();
                }
                catch (Exception e)
                { // If any Exception has occured, abort the whole process.
                    WaitingSignal.Set();

                    _owner.HandleBackgroundException(e);
                }
            }

            private unsafe int DeflateBlock(ZLibParallelCompressJob job, int inputStartIdx, ZLibFlush flush)
            {
                Debug.Assert(!job.InBuffer.Disposed);

                fixed (byte* inBufPtr = job.InBuffer.Buf) // [In] RAW
                {
                    Debug.Assert(0 <= inputStartIdx && inputStartIdx <= job.InBuffer.DataEndIdx && job.InBuffer.DataStartIdx == 0);

                    _zs!.NextIn = inBufPtr + inputStartIdx;
                    _zs.AvailIn = (uint)(job.InBuffer.DataEndIdx - inputStartIdx);

                    // Loop as long as the output buffer is not full after running deflate()
                    do
                    {
                        // One compressed version of inBuffer data must fit in one outBuffer.
                        if (job.OutBuffer.IsFull)
                        { // Expand the outBuffer if the buffer is full.
                            int newSize = ZLibParallelCompressJob.CalcBufferExpandSize(job.OutBuffer.Size);
                            if (!job.OutBuffer.Expand(newSize))
                                throw new InvalidOperationException($"Failed to expand [{nameof(job.OutBuffer)}] to [{newSize}] bytes.");
                        }

                        fixed (byte* outBufPtr = job.OutBuffer.Buf) // [Out] Compressed
                        {
                            _zs.NextOut = outBufPtr + job.OutBuffer.DataEndIdx;
                            _zs.AvailOut = (uint)(job.OutBuffer.Size - job.OutBuffer.DataEndIdx);

                            uint beforeAvailIn = _zs.AvailIn;
                            uint beforeAvailOut = _zs.AvailOut;
                            ZLibRet ret = ZLibInit.Lib.NativeAbi.Deflate(_zs, flush);
                            uint bytesRead = beforeAvailIn - _zs.AvailIn;
                            uint bytesWritten = beforeAvailOut - _zs.AvailOut;

#if DEBUG_PARALLEL
                            Console.WriteLine($"DeflateBlock1 (#{job.SeqNum}): in({job.InBuffer}) dict({job.DictBuffer}) out({job.OutBuffer}) ({flush})");
#endif
                            inputStartIdx += (int)bytesRead;
                            job.OutBuffer.DataEndIdx += (int)bytesWritten;
#if DEBUG_PARALLEL
                            Console.WriteLine($"DeflateBlock2 (#{job.SeqNum}): in({job.InBuffer}) dict({job.DictBuffer}) out({job.OutBuffer}) ({flush})");
#endif

                            ZLibException.CheckReturnValue(ret, _zs);
                        }
                    }
                    while (_zs.AvailOut == 0);

                    Debug.Assert(_zs.AvailIn == 0);
                }

                return inputStartIdx;
            }

            private unsafe int DeflateBlockFinish(ZLibParallelCompressJob job, int inputStartIdx)
            {
                Debug.Assert(!job.InBuffer.Disposed);

                fixed (byte* inBufPtr = job.InBuffer.Buf) // [In] RAW
                {
                    Debug.Assert(0 <= inputStartIdx && inputStartIdx <= job.InBuffer.DataEndIdx && job.InBuffer.DataStartIdx == 0);

                    _zs!.NextIn = inBufPtr + inputStartIdx;
                    _zs.AvailIn = (uint)(job.InBuffer.DataEndIdx - inputStartIdx);

                    // Loop as long as the output buffer is not full after running deflate()
                    ZLibRet ret = ZLibRet.Ok;
                    while (ret != ZLibRet.StreamEnd)
                    {
                        // One compressed version of inBuffer data must fit in one outBuffer.
                        if (job.OutBuffer.IsFull)
                        { // Expand the outBuffer if the buffer is full.
                            int newSize = ZLibParallelCompressJob.CalcBufferExpandSize(job.OutBuffer.Size);
                            if (!job.OutBuffer.Expand(newSize))
                                throw new InvalidOperationException($"Failed to expand [{nameof(job.OutBuffer)}] to [{newSize}] bytes.");
                        }

                        fixed (byte* outBufPtr = job.OutBuffer.Buf) // [Out] Compressed
                        {
                            _zs.NextOut = outBufPtr + job.OutBuffer.DataEndIdx;
                            _zs.AvailOut = (uint)(job.OutBuffer.Size - job.OutBuffer.DataEndIdx);

                            uint beforeAvailIn = _zs.AvailIn;
                            uint beforeAvailOut = _zs.AvailOut;
                            ret = ZLibInit.Lib.NativeAbi.Deflate(_zs, ZLibFlush.Finish);
                            uint bytesRead = beforeAvailIn - _zs.AvailIn;
                            uint bytesWritten = beforeAvailOut - _zs.AvailOut;

#if DEBUG_PARALLEL
                            Console.WriteLine($"DeflateBlockFinish2 (#{job.SeqNum}): in({job.InBuffer}) dict({job.DictBuffer}) out({job.OutBuffer})");
#endif
                            inputStartIdx += (int)bytesRead;
                            job.OutBuffer.DataEndIdx += (int)bytesWritten;
#if DEBUG_PARALLEL
                            Console.WriteLine($"DeflateBlockFinish2 (#{job.SeqNum}): in({job.InBuffer}) dict({job.DictBuffer}) out({job.OutBuffer})");
#endif

                            ZLibException.CheckReturnValue(ret, _zs);
                        }
                    }

                    Debug.Assert(_zs.AvailIn == 0);
                }

                return inputStartIdx;
            }

            private void Dispose(bool disposing)
            {
                if (_disposed)
                    return;

                if (disposing)
                { // Dispose managed state.

                }

                // Dispose unmanaged resources, and set large fields to null.
                if (_zs != null)
                {
                    ZLibInit.Lib.NativeAbi.DeflateEnd(_zs);
                    _zsPin.Free();
                    _zs = null;
                }

                ReadSignal.Dispose();
                WaitingSignal.Dispose();

                _disposed = true;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        private void SetWorkerThreadReadSignal()
        {
            foreach (CompressThreadProc threadProc in _workerThreadProcs)
                threadProc.ReadSignal.Set();
        }
        #endregion

        #region class WriterThreadProc
        internal sealed class WriterThreadProc : IDisposable
        {
            private readonly DeflateThreadedStream _owner;

            private long _outSeq = 0;
            /// <summary>
            /// Also represents the number of blocks written to the BaseStream.
            /// </summary>
            public long SeqNum => Interlocked.Read(ref _outSeq);

            public readonly AutoResetEvent WriteSignal = new AutoResetEvent(false);
            public readonly ManualResetEvent WaitingSignal = new ManualResetEvent(false);

            private bool _disposed = false;

            private readonly ZLibChecksumBase<uint>? _writeChecksum;

            public WriterThreadProc(DeflateThreadedStream owner)
            {
                _owner = owner;
                _writeChecksum = FormatChecksum(owner._format);
            }

            ~WriterThreadProc()
            {
                Dispose(false);
            }

            /// <summary>
            /// WriterThread: Write compressed data into BaseStream
            /// </summary>
            public unsafe void WriterThreadMain()
            {
                try
                {
                    if (_owner.BaseStream == null)
                        throw new ObjectDisposedException("This stream had been disposed.");

                    bool exitLoop = false;
                    while (true)
                    {
                        // Loop until the write queue is empty
                        LinkedListNode<ZLibParallelCompressJob>? outJobNode = null;
                        do
                        {
                            // Get next OutJob
                            lock (_owner._outListLock)
                            {
                                outJobNode = _owner._outList.First;
                                if (outJobNode == null) // Reached end of the write queue -> then goes to the outer loop
                                    break;
                                if (outJobNode.Value.Seq != _outSeq) // The next job is not the expected one -> wait for the next signal
                                    break;
                                _owner._outList.RemoveFirst();
                            }

                            _outSeq += 1;

                            using (ZLibParallelCompressJob job = outJobNode.Value)
                            {
                                // Write to BaseStream
                                _owner.BaseStream.Write(job.OutBuffer.Buf, 0, job.OutBuffer.DataEndIdx);

#if DEBUG_PARALLEL
                                Console.WriteLine($"-- wrote (#{job.SeqNum}) : last=[{job.IsLastBlock}] in=[{job.InBuffer}] dict=[{job.DictBuffer}] out=[{job.OutBuffer}]");
#endif

                                // Increase TotalIn & TotalOut
                                _owner.AddTotalIn(job.RawInputSize);
                                _owner.AddTotalOut(job.OutBuffer.ReadableSize);

                                // Combine the checksum (if necessary)
                                if (0 < job.RawInputSize && _writeChecksum != null)
                                    _writeChecksum.Combine(job.Checksum, job.RawInputSize);

                                // In case of last block, we need to write a trailer, too.
                                if (job.IsLastBlock)
                                {
                                    // Finalize the stream - write the trailer (zlib, gzip only)
                                    if (_writeChecksum != null && !_owner.IsAborted)
                                        _owner.WriteTrailer(_writeChecksum.Checksum, _owner.TotalIn);

                                    // The session is finished, so always set the write complete signal.
                                    _owner._targetWrittenEvent.Set();

                                    // Exit loop and close the thread if the last block is reached
                                    exitLoop = true;
                                    break;
                                }

                                // Is the target block was written to the block?
                                // Inform the main thread which may be waiting for a signal.
                                if (_owner.WaitSeq <= _outSeq)
                                    _owner._targetWrittenEvent.Set();
                                else
                                    _owner._targetWrittenEvent.Reset();

                                // [RefCount]
                                // First  block:          1 <= job.InBuffer._refCount <= 3, job.DictBuffer == null
                                // Nor/Fin block (32K<=): 1 <= job.InBuffer._refCount <= 3, 0(D) <= job.DictBuffer._refCount <= 2
                                // Nor/Fin block (<32K):  job.InBuffer._refCount == 1,      job.DictBuffer._refCount == 0(D)
                            }

                            // [RefCount]
                            // First  block:          0(D) <= job.InBuffer._refCount <= 2, job.DictBuffer == null
                            // Nor/Fin block (32K<=): 0(D) <= job.InBuffer._refCount <= 2, 0(D) <= job.DictBuffers._refCount <= 1
                            // Nor/Fin block (<32K):  job.InBuffer._refCount == 0(D),      job.DictBuffer._refCount == 0(D)

                            // If the abort signal is set, break the loop
                            if (_owner.IsAborted)
                            {
                                exitLoop = true;
                                break;
                            }
                        }
                        while (outJobNode != null);

                        if (exitLoop)
                            break;

                        // Signal about the waiting
                        // Wait for the write signal at least once
                        WaitHandle.SignalAndWait(WaitingSignal, WriteSignal);

                        if (_owner.IsAborted)
                            break;

                        // Reset the waiting signal
                        WaitingSignal.Reset();
                    }

                    WaitingSignal.Set();
                    _owner._targetWrittenEvent.Set();
                }
                catch (Exception e)
                { // If any Exception has occured, abort the whole process.
                    WaitingSignal.Set();
                    _owner._targetWrittenEvent.Set();

                    _owner.HandleBackgroundException(e);
                }
            }

            private void Dispose(bool disposing)
            {
                if (_disposed)
                    return;

                if (disposing)
                { // Dispose managed state.

                }

                // Dispose unmanaged resources, and set large fields to null.
                WriteSignal.Dispose();
                WaitingSignal.Dispose();

                _disposed = true;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
        #endregion

        #region WriteHeader, WriteTrailer
        private void WriteHeader()
        {
            if (BaseStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            if (_format == ZLibOperateFormat.ZLib)
            { // https://datatracker.ietf.org/doc/html/rfc1950
                ushort zlibHead = 0x78 << 8; // deflate with 32K window
                int compLevel = (int)((_compLevel == ZLibCompLevel.Default) ? ZLibCompLevel.Level6 : _compLevel);
                if (compLevel == 1) // Fastest
                    zlibHead += 0 << 6;
                else if (9 <= compLevel) // Best
                    zlibHead += 3 << 6;
                else if (6 <= compLevel) // Default
                    zlibHead += 1 << 6;
                else
                    zlibHead += 2 << 6;
                zlibHead += (ushort)(31 - zlibHead % 31); // Make it a multiple of 31

                // zlib stream is big-endian
                byte[] headBuf = new byte[2];
                BinaryPrimitives.WriteUInt16BigEndian(headBuf, zlibHead);
                BaseStream.Write(headBuf, 0, headBuf.Length);
            }
            else if (_format == ZLibOperateFormat.GZip)
            { // https://datatracker.ietf.org/doc/html/rfc1952
                int compLevel = (int)((_compLevel == ZLibCompLevel.Default) ? ZLibCompLevel.Level6 : _compLevel);
                byte levelHead;
                if (9 <= compLevel)
                    levelHead = 2;
                else if (compLevel == 1)
                    levelHead = 4;
                else
                    levelHead = 0;

                byte[] headBuf =
                [
                    0x1F,
                    0x8B,
                    8, // deflate 
                    0, // name or comment -> does not support
                    0, // mtime (1/4) -> does not suppport
                    0, // mtime (2/4) -> does not suppport
                    0, // mtime (3/4) -> does not suppport
                    0, // mtime (4/4) -> does not suppport
                    levelHead, // level
                    3 // unix
                ];

                BaseStream.Write(headBuf, 0, headBuf.Length);
            }
        }

        private void WriteTrailer(uint finalChecksum, long rawLength)
        {
            if (BaseStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            if (_format == ZLibOperateFormat.ZLib)
            { // https://datatracker.ietf.org/doc/html/rfc1950
                // checksum is Adler32, zlib stream uses big-endian
                byte[] checkBuf = new byte[4];
                BinaryPrimitives.WriteUInt32BigEndian(checkBuf, finalChecksum);

                BaseStream.Write(checkBuf, 0, checkBuf.Length);
                AddTotalOut(checkBuf.Length);
            }
            else if (_format == ZLibOperateFormat.GZip)
            { // https://datatracker.ietf.org/doc/html/rfc1952
                // gzip uses little-endian
                byte[] trailBuf = new byte[8];

                // Checksum is CRC32
                BinaryPrimitives.WriteUInt32LittleEndian(trailBuf, finalChecksum);

                // Write the raw length (would be truncated to 32bit)
                BinaryPrimitives.WriteUInt32LittleEndian(trailBuf.AsSpan(4), (uint)rawLength);

                BaseStream.Write(trailBuf, 0, trailBuf.Length);
                AddTotalOut(trailBuf.Length);
            }
        }
        #endregion

        #region Background Thread Exception Handling
        /// <summary>
        /// This method can be called from any background threads.
        /// DO NOT JOIN the background threads in this method.
        /// </summary>
        /// <param name="e"></param>
        private void HandleBackgroundException(Exception e)
        {
#if DEBUG_PARALLEL
            Console.WriteLine($"Handled background exception: {e}");
#endif

            lock (_backgroundExceptionsLock)
                _backgroundExceptions.Add(e);

            // Signal the workerThreads and writerThread to abort.
            SignalAbort();
        }
        #endregion

        #region Stream Properties
        /// <inheritdoc />
        public override bool CanRead => false;
        /// <inheritdoc />
        public override bool CanWrite => BaseStream?.CanWrite ?? false;
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
                if (TotalIn == 0)
                    return 0;
                return 100 - TotalOut * 100.0 / TotalIn;
            }
        }
        #endregion

        #region (internal, private) Check Arguments
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CheckBlockSize(int blockSize)
        {
            if (blockSize < 0)
                throw new ArgumentOutOfRangeException(nameof(blockSize));
            return Math.Max(blockSize, DefaultBlockSize); // At least 128KB
        }
        #endregion
    }
}
