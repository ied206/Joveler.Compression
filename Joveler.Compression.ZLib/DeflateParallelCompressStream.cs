#nullable enable 

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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Joveler.Compression.ZLib
{
    internal sealed class DeflateParallelCompressStream : Stream
    {
        #region Fields and Properties
        private readonly ZLibOperateFormat _format;
        private readonly ZLibCompLevel _compLevel;
        private readonly ZLibWindowBits _windowBits;
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

        private long _seqNum = 0;
        private bool _finalEnqueued = false;
        private readonly int _inBlockSize;
        private readonly int _outBlockSize;
        private static int CalcOutBlockSize(int rawBlockSize)
        { // 17/16 * rawBlockSize + 32KB
            return rawBlockSize + (rawBlockSize >> 4) + ParallelCompressJob.DictWindowSize;
        }

        private readonly ConcurrentQueue<ParallelCompressJob> _inQueue;
        private readonly object _outListLock;
        private readonly LinkedList<ParallelCompressJob> _outList;

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

        private static ChecksumBase<uint>? FormatChecksum(ZLibOperateFormat format)
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
        /// Create compressing DeflateStream.
        /// </summary>
        public DeflateParallelCompressStream(Stream baseStream, ZLibParallelCompressOptions pcompOpts, ZLibOperateFormat format)
        {
            ZLibInit.Manager.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));

            _disposed = false;
            _leaveOpen = pcompOpts.LeaveOpen;

            _format = format;
            _compLevel = pcompOpts.Level;
            _windowBits = pcompOpts.WindowBits;

            int threadCount = pcompOpts.Threads;
            if (threadCount == 0)
                threadCount = Environment.ProcessorCount;

            _workerThreads = new Thread[threadCount];
            _workerThreadProcs = new CompressThreadProc[threadCount];

            _inQueue = new ConcurrentQueue<ParallelCompressJob>();
            _outListLock = new object();
            _outList = new LinkedList<ParallelCompressJob>();

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

            // Create write thread
            _writerThreadProc = new WriterThreadProc(this);
            _writerThread = new Thread(_writerThreadProc.WriterThreadMain);
            _writerThread.Start();

            // Create worker threads
            for (int i = 0; i < _workerThreads.Length; i++)
            {
                CompressThreadProc compessThreadProc = new CompressThreadProc(this);
                _workerThreadProcs[i] = compessThreadProc;

                Thread workerThread = new Thread(compessThreadProc.CompressThreadMain);
                workerThread.Start();
                _workerThreads[i] = workerThread;
            }
        }
        #endregion

        #region Disposable Pattern
        ~DeflateParallelCompressStream()
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
                {
                    // Finalize compression
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

                    while (_inQueue.TryDequeue(out ParallelCompressJob? inJob))
                        inJob?.Dispose();

                    lock (_outListLock)
                    {
                        foreach (ParallelCompressJob outJob in _outList)
                            outJob.Dispose();
                        _outList.Clear();
                    }

                    foreach (CompressThreadProc threadProc in _workerThreadProcs)
                        threadProc.Dispose();
                    _writerThreadProc.Dispose();

                    _nextDictBuffer?.Dispose();
                    _inputBuffer.Dispose();

                    _disposed = true;
                }
            }

            // Dispose the base class
            base.Dispose(disposing);
        }
        #endregion

        #region MainThread - Write: push raw data into the worker threads
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
            // [Stage 10] Pool the input buffer until it is full.
            bool enqueued = false;
            int bytesWritten = 0;
            do
            {
                bytesWritten += _inputBuffer.Write(span.Slice(bytesWritten), false);

                // [Stage 11 If the input buffer is full -> enqueue the job, and reset the input buffer.
                if (_inputBuffer.IsFull)
                {
                    EnqueueInputBuffer(false);
                    enqueued = true;
                }
            }
            while (bytesWritten < span.Length);

            // [Stage 12] Signal the worker threads
            // Alert the worker thread to resume compressing
            if (enqueued)
                SetWorkerThreadReadSignal();
        }

        private void EnqueueInputBuffer(bool isFinal)
        {
            if (_finalEnqueued)
                throw new InvalidOperationException("The final block has already been enqueued.");

            // [RefCount]
            // First   block: _nextDictBuffer == null
            // Nor/Fin block: _nextDictBuffer != null, 1 <= _nextDictBuffer._refCount <= 2

            Debug.Assert(_seqNum == 0 && _nextDictBuffer == null || 
                _seqNum != 0 && _nextDictBuffer != null && !_nextDictBuffer.Disposed);

            _finalEnqueued |= isFinal;

            // _refCount of job.InBuffer and _nextDictBuffer is increased in constructor call.
            ParallelCompressJob job = new ParallelCompressJob(_pool, _seqNum, _inBlockSize, _nextDictBuffer, _outBlockSize);
            _seqNum += 1;

            // [RefCount]
            // First   block: job.InBuffer._refCount == 1, (job.DictBuffer == _nextDictBuffer) == null
            // Nor/Fin block: job.InBuffer._refCount == 1, 2 <= (job.DictBuffer == _nextDictBuffer)._refCount <= 3

            if (isFinal)
                job.IsLastBlock = true;

            job.InBuffer.Write(_inputBuffer.ReadablePortionSpan);
            job.RawInputSize = job.InBuffer.DataEndIdx;

            // Prepare next dictionary buffer (pass for final block)
            if (isFinal)
            {
                _nextDictBuffer = null; // No longer required
            }
            else if (ParallelCompressJob.DictWindowSize <= job.InBuffer.ReadableSize)
            {
                _nextDictBuffer = job.InBuffer.AcquireRef();
            }
            else
            { // next input is less than 32K -> retain last 32K
                ReferableBuffer copyDictBuffer = new ReferableBuffer(_pool, ParallelCompressJob.DictWindowSize);
                if (_nextDictBuffer == null)
                { // First block, but the input is less than 32K -> copy the full input
                    copyDictBuffer.Write(job.InBuffer.ReadablePortionSpan);
                }
                else
                { // Normal block, source 32K from the previous input + current input
                    int copyLastDictSize = ParallelCompressJob.DictWindowSize - job.InBuffer.ReadableSize;
                    ReadOnlySpan<byte> lastDictSpan = _nextDictBuffer.ReadablePortionSpan.Slice(_nextDictBuffer.DataEndIdx - copyLastDictSize, copyLastDictSize);
                    copyDictBuffer.Write(lastDictSpan);
                    _nextDictBuffer.ReleaseRef();
                        
                    copyDictBuffer.Write(job.InBuffer.ReadablePortionSpan);

                    Debug.Assert(copyDictBuffer.DataEndIdx == ParallelCompressJob.DictWindowSize);
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
                ParallelCompressJob eofJob = new ParallelCompressJob(_pool, ParallelCompressJob.EofBlockSeqNum);
                _inQueue.Enqueue(eofJob);

                // [RefCount]
                // EOF block: job.InBuffer._refCount == 1, job.DictBuffer = null
            }
        }

        private void EnqueueOutList(ParallelCompressJob job)
        {
            lock (_outListLock)
            {
                LinkedListNode<ParallelCompressJob>? node = _outList.First;

                while (node != null)
                {
                    if (job.SeqNum < node.Value.SeqNum)
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
            while (true)
            {
                // [Idle state]
                // - Queues
                //   * inQueue: 0
                //   * outList: 0
                // - WorkerThread
                //   * readSignal: false
                //   * waitingSignal: true
                // - WriterThread
                //   * writeSignal: false
                //   * waitingSignal: true
                int outCount;
                lock (_outListLock)
                    outCount = _outList.Count;

                if (_inQueue.Count == 0 && outCount == 0 &&
                    IsAllWorkerThreadWaitSignalSet(null) && _writerThreadProc.WaitingSignal.WaitOne())
                    break;
            }

            // Flush the remaining compressed data into BaseStream
            BaseStream.Flush();
        }

        public void Abort()
        {
            // Signal the workerThreads and writerThread to abort
            SetWorkerThreadAbortSignal();
            _writerThreadProc.AbortSignal.Set();

            // Wait until all worker threads to finish
            foreach (Thread thread in _workerThreads)
                thread.Join();

            // Now all worker threads are terminated.
            // Wait until writerThread finishes.
            _writerThread.Join();
        }

        private void FinishWrite()
        {
            // If aborted, return immediately
            if (_workerThreadProcs.All(x => x.AbortSignal.WaitOne(0)) && _writerThreadProc.AbortSignal.WaitOne(0))
                return;

            // flush and enqueue a final block with remaining buffer to run ZLibFlush.Finish.
            EnqueueInputBuffer(true);

            // Enqueue an EOF block with empty buffer per thread
            // EOF block is only a simple marker to terminate the worker threads.
            EnqueueInputEof();

            // Signal to the worker threads to finalize the compression
            SetWorkerThreadReadSignal();

            // Wait until all worker threads to finish
            foreach (Thread thread in _workerThreads)
                thread.Join();

            // Now final block is being processed by the writerThread.
            // Wait until writerThread finishes.
            _writerThread.Join();
        }
        #endregion

        #region class CompressThreadProc
        internal sealed class CompressThreadProc : IDisposable
        {
            private readonly DeflateParallelCompressStream _owner;

            public readonly AutoResetEvent ReadSignal = new AutoResetEvent(false);
            public readonly ManualResetEvent WaitingSignal = new ManualResetEvent(false);
            public readonly ManualResetEvent AbortSignal = new ManualResetEvent(false);

            private readonly ChecksumBase<uint>? _blockChecksum;

            private ZStreamBase? _zs;
            private GCHandle _zsPin;

            private bool _disposed = false;

            public CompressThreadProc(DeflateParallelCompressStream owner)
            {
                _owner = owner;
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
                if (_zs == null)
                    throw new NullReferenceException($"[{nameof(_zs)}] is null.");

                ZLibRet ret;
                bool exitLoop = false;
                while (true)
                {
                    // Signal that this workerThread is waiting for the next job
                    // Wait for the next job to come in
                    WaitHandle.SignalAndWait(WaitingSignal, ReadSignal);

                    // Reset the waiting signal
                    WaitingSignal.Reset();

                    // Loop until the input queue is empty
                    while (_owner._inQueue.TryDequeue(out ParallelCompressJob? job) && job != null)
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

                                int dictSize = Math.Min(job.DictBuffer.DataEndIdx, ParallelCompressJob.DictWindowSize);
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
                            if (!job.IsLastBlock)
                            { // Deflated block will end on a byte boundary, using a sync marker if necessary (SyncFlush)
                                // ADVACNED: Bit-level output manipulation.
                                // SIMPLE: In pre zlib 1.2.6, just call DeflateBlock once with ZLibFlush.SyncFlush.

                                // After Z_BLOCK, Up to 7 bits of output data are waiting to be written.
                                DeflateBlock(job, ZLibFlush.Block);

                                // How many bits are waiting to be written?
                                int bits = 0;
                                ret = ZLibInit.Lib.NativeAbi.DeflatePending(_zs, null, &bits);
                                ZLibException.CheckReturnValue(ret, _zs);

                                // Add enough empty blocks to get to a byte boundary
                                if (0 < (bits & 1)) // 1 bit is waiting to be written
                                { // Flush the bit-level boundary
                                    DeflateBlock(job, ZLibFlush.SyncFlush);
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
                                    DeflateBlock(job, ZLibFlush.Block);
                                }
                            }
                            else
                            { // Finish the deflate stream
                                DeflateBlockFinish(job);
                            }

#if DEBUG_PARALLEL
                            Console.WriteLine($"-- compressed (#{job.SeqNum}) : last=[{job.IsLastBlock}] in=[{job.InBuffer}] dict=[{job.DictBuffer}] out=[{job.OutBuffer}] ");
#endif

                            Debug.Assert(job.InBuffer.DataStartIdx == job.RawInputSize && job.RawInputSize == job.InBuffer.DataEndIdx);

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
                        if (AbortSignal.WaitOne(0))
                        {
                            exitLoop = true;
                            break;
                        }
                    }

                    if (exitLoop)
                        break;
                }
            }

            private unsafe void DeflateBlock(ParallelCompressJob job, ZLibFlush flush)
            {
                fixed (byte* inBufPtr = job.InBuffer.Buf) // [In] RAW
                {
                    Debug.Assert(job.InBuffer.DataEndIdx >= job.InBuffer.DataStartIdx);

                    _zs!.NextIn = inBufPtr + job.InBuffer.DataStartIdx;
                    _zs.AvailIn = (uint)(job.InBuffer.DataEndIdx - job.InBuffer.DataStartIdx);

                    // Loop as long as the output buffer is not full after running deflate()
                    do
                    {
                        // One compressed version of inBuffer data must fit in one outBuffer.
                        if (job.OutBuffer.IsFull)
                        { // Expand the outBuffer if the buffer is full.
                            int newSize = ParallelCompressJob.CalcBufferExpandSize(job.OutBuffer.Size);
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
                            job.InBuffer.DataStartIdx += (int)bytesRead;
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
            }

            private unsafe void DeflateBlockFinish(ParallelCompressJob job)
            {
                fixed (byte* inBufPtr = job.InBuffer.Buf) // [In] RAW
                {
                    Debug.Assert(job.InBuffer.DataEndIdx >= job.InBuffer.DataStartIdx);

                    _zs!.NextIn = inBufPtr + job.InBuffer.DataStartIdx;
                    _zs.AvailIn = (uint)(job.InBuffer.DataEndIdx - job.InBuffer.DataStartIdx);

                    // Loop as long as the output buffer is not full after running deflate()
                    ZLibRet ret = ZLibRet.Ok;
                    while (ret != ZLibRet.StreamEnd)
                    {
                        // One compressed version of inBuffer data must fit in one outBuffer.
                        if (job.OutBuffer.IsFull)
                        { // Expand the outBuffer if the buffer is full.
                            int newSize = ParallelCompressJob.CalcBufferExpandSize(job.OutBuffer.Size);
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
                            job.InBuffer.DataStartIdx += (int)bytesRead;
                            job.OutBuffer.DataEndIdx += (int)bytesWritten;
#if DEBUG_PARALLEL
                            Console.WriteLine($"DeflateBlockFinish2 (#{job.SeqNum}): in({job.InBuffer}) dict({job.DictBuffer}) out({job.OutBuffer})");
#endif

                            ZLibException.CheckReturnValue(ret, _zs);
                        }
                    }

                    Debug.Assert(_zs.AvailIn == 0);
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
                ZLibInit.Lib.NativeAbi.DeflateEnd(_zs);
                _zsPin.Free();
                _zs = null;

                ReadSignal.Dispose();
                WaitingSignal.Dispose();
                AbortSignal.Dispose();

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

        private void SetWorkerThreadAbortSignal()
        {
            foreach (CompressThreadProc threadProc in _workerThreadProcs)
                threadProc.AbortSignal.Set();
        }

        private bool IsAllWorkerThreadWaitSignalSet(TimeSpan? timeout)
        {
            bool isAllWaiting = true;
            foreach (CompressThreadProc threadProc in _workerThreadProcs)
            {
                bool isWaiting;
                if (timeout.HasValue)
                    isWaiting = threadProc.WaitingSignal.WaitOne(timeout.Value);
                else
                    isWaiting = threadProc.WaitingSignal.WaitOne();

                if (!isWaiting)
                {
                    isAllWaiting = false;
                    break;
                }
            }
            return isAllWaiting;
        }
#endregion

        #region class WriterThreadProc
        internal sealed class WriterThreadProc : IDisposable
        {
            private readonly DeflateParallelCompressStream _owner;

            public readonly AutoResetEvent WriteSignal = new AutoResetEvent(false);
            public readonly ManualResetEvent WaitingSignal = new ManualResetEvent(false);
            public readonly ManualResetEvent AbortSignal = new ManualResetEvent(false);

            private bool _disposed = false;

            private readonly ChecksumBase<uint>? _writeChecksum;

            public WriterThreadProc(DeflateParallelCompressStream owner)
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
                if (_owner.BaseStream == null)
                    throw new ObjectDisposedException("This stream had been disposed.");

                long findSeqNum = 0;
                bool exitLoop = false;
                while (true)
                {
                    // Signal about the waiting
                    // Wait for the write signal at least once
                    WaitHandle.SignalAndWait(WaitingSignal, WriteSignal);

                    // Reset the waiting signal
                    WaitingSignal.Reset();

                    // Loop until the write queue is empty
                    LinkedListNode<ParallelCompressJob>? outJobNode = null;
                    do
                    {
                        // Get next OutJob
                        lock (_owner._outListLock)
                        {
                            outJobNode = _owner._outList.First;
                            if (outJobNode == null) // Reached end of the write queue -> then goes to the outer loop
                                break;
                            if (outJobNode.Value.SeqNum != findSeqNum) // The next job is not the expected one -> wait for the next signal
                                break;
                            _owner._outList.RemoveFirst();
                        }

                        findSeqNum += 1;

                        using (ParallelCompressJob job = outJobNode.Value)
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
                            if (0 < job.RawInputSize)
                                _writeChecksum?.Combine(job.Checksum, job.RawInputSize);

                            // Exit if the last block is reached
                            if (job.IsLastBlock)
                            {
                                exitLoop = true;
                                break;
                            }

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
                        if (AbortSignal.WaitOne(0))
                        {
                            exitLoop = true;
                            break;
                        }
                    }
                    while (outJobNode != null);

                    if (exitLoop)
                        break;
                }

                // Finalize the stream - write the trailer (zlib, gzip only)
                if (_writeChecksum != null)
                    _owner.WriteTrailer(_writeChecksum.Checksum, _owner.TotalIn);
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
                AbortSignal.Dispose();

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
                if (BitConverter.IsLittleEndian) // zlib stream is big-endian
                    zlibHead = BinaryPrimitives.ReverseEndianness(zlibHead);
                byte[] headBuf = BitConverter.GetBytes(zlibHead);
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
                uint finalAdler32 = finalChecksum;
                if (BitConverter.IsLittleEndian) // zlib stream is big-endian
                    finalAdler32 = BinaryPrimitives.ReverseEndianness(finalAdler32);

                byte[] checkBuf = BitConverter.GetBytes(finalAdler32);
                BaseStream.Write(checkBuf, 0, checkBuf.Length);
                AddTotalOut(checkBuf.Length);
            }
            else if (_format == ZLibOperateFormat.GZip)
            { // https://datatracker.ietf.org/doc/html/rfc1952
                uint finalCrc32 = finalChecksum;
                byte[] checkBuf = BitConverter.GetBytes(finalCrc32);
                BaseStream.Write(checkBuf, 0, checkBuf.Length);

                // Write the raw length (would be truncated to 32bit)
                byte[] rawLenBuf = BitConverter.GetBytes((uint)rawLength);
                BaseStream.Write(rawLenBuf, 0, rawLenBuf.Length);
                AddTotalOut(checkBuf.Length + rawLenBuf.Length);
            }
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
        private static int CheckBlockSize(int blockSize)
        {
            if (blockSize < 0)
                throw new ArgumentOutOfRangeException(nameof(blockSize));
            return Math.Max(blockSize, 128 * 1024); // At least 128KB
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
}
