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
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Joveler.Compression.ZLib
{
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
        public int BlockSize { get; set; } = DeflateParallelStream.DefaultChunkSize;
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

    internal sealed class DeflateParallelStream : Stream
    {
        #region Fields and Properties
        private readonly int _threads;
        private readonly ZLibOperateFormat _format;
        private readonly ZLibCompLevel _compLevel;
        private readonly ZLibWindowBits _windowBits;

        private readonly TimeSpan? _writeTimeout;
        private readonly bool _leaveOpen;
        private bool _disposed = false;

        // Compression
        // System.Threading.Tasks.DataFlow
        private readonly BlockingCollection<ZStreamHandle> _zsHandles = new BlockingCollection<ZStreamHandle>();

        private readonly TransformBlock<ZLibParallelCompressJob, ZLibParallelCompressJob> _compWorkChunk;
        private readonly ActionBlock<ZLibParallelCompressJob> _compSortChunk;

        private readonly SortedSet<ZLibParallelCompressJob> _outSet = new SortedSet<ZLibParallelCompressJob>(new ZLibParallelCompressJobComparator());

        private readonly ActionBlock<ZLibParallelCompressJob> _compWriteChunk;

        
        private long _inSeq = 0;
        private long _outSeq = 0;
        private long _latestSeq = -1;
        private long _waitSeq = -1;

        private bool _finalEnqueued = false;
        private readonly ManualResetEvent _targetWrittenEvent = new ManualResetEvent(true);

        private readonly ZLibChecksumBase<uint>? _writeChecksum;
        private readonly ArrayPool<byte> _pool;
        private readonly int _inBlockSize;
        private readonly int _outBlockSize;
        private readonly PooledBuffer _inputBuffer;

        /// <summary>
        /// Its ref is acquired twice in EnqueueInputData, and released in CompressThreadMain/WriterThreadMain.
        /// </summary>
        private ReferableBuffer? _nextDictBuffer;

        private readonly CancellationTokenSource _abortTokenSrc = new CancellationTokenSource();
        public bool IsAborted => _abortTokenSrc.IsCancellationRequested;

        private readonly List<Exception> _taskExcepts = new List<Exception>();

        public Stream? BaseStream { get; private set; }
        public long TotalIn { get; private set; }
        public long TotalOut { get; private set; }
        
        public long WaitSeq => Interlocked.Read(ref _waitSeq);

        private static int CalcOutBlockSize(int rawBlockSize) => rawBlockSize + (rawBlockSize >> 3); // 9/8 * rawBlockSize

        /// <summary>
        /// Default Block Size 
        /// </summary>
        internal const int DefaultChunkSize = 128 * 1024; // pigz uses fixed 128KB for block size

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
        public DeflateParallelStream(Stream baseStream, ZLibParallelCompressOptions pcompOpts, ZLibOperateFormat format)
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
            _threads = threadCount;

            // Calculate the buffer size
            CheckBlockSize(pcompOpts.BlockSize);
            _inBlockSize = pcompOpts.BlockSize;
            _outBlockSize = CalcOutBlockSize(pcompOpts.BlockSize);
            Debug.Assert(_inBlockSize <= _outBlockSize);

            _pool = pcompOpts.BufferPool ?? ArrayPool<byte>.Shared;
            _inputBuffer = new PooledBuffer(_pool, _inBlockSize);
            _nextDictBuffer = null;
            _writeChecksum = FormatChecksum(_format);

            // Write the header
            WriteHeader();

            // Launch CompressTask, WriterTask
            // - If BoundedCapacity is set, it will discard incoming message from Post() when the its queue is full.
            // - So in that case, take care to use SendAsync() instead of Post().
            int maxWaitingJobs = 4 * _threads;
            _compWorkChunk = new TransformBlock<ZLibParallelCompressJob, ZLibParallelCompressJob>(CompressProc, new ExecutionDataflowBlockOptions()
            {
                CancellationToken = _abortTokenSrc.Token,
                BoundedCapacity = maxWaitingJobs,
                EnsureOrdered = true,
                MaxDegreeOfParallelism = _threads,
            });
            _compSortChunk = new ActionBlock<ZLibParallelCompressJob>(WriteSortProc, new ExecutionDataflowBlockOptions()
            {
                CancellationToken = _abortTokenSrc.Token,
                EnsureOrdered = false,
                MaxDegreeOfParallelism = 1,
            });

            _compWriteChunk = new ActionBlock<ZLibParallelCompressJob>(WriterProc, new ExecutionDataflowBlockOptions()
            {
                CancellationToken = _abortTokenSrc.Token,
                EnsureOrdered = true,
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = maxWaitingJobs,
            });

            DataflowLinkOptions linkOptions = new DataflowLinkOptions
            {
                PropagateCompletion = false,
                Append = true,
            };

            _compWorkChunk.LinkTo(_compSortChunk, linkOptions);

            for (int i = 0; i < _threads; i++)
            {
                ZStreamHandle zsh = new ZStreamHandle(_format, _compLevel, _windowBits);
                _zsHandles.Add(zsh);
            }
        }
#endregion

        #region Disposable Pattern
        ~DeflateParallelStream()
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
                    if (_nextDictBuffer != null)
                    {
                        _nextDictBuffer.Dispose();
                        _nextDictBuffer = null;
                    }
                    _inputBuffer.Dispose();

                    Debug.Assert(_zsHandles.Count == _threads);
                    while (_zsHandles.TryTake(out ZStreamHandle? zsh))
                        zsh.Dispose();
                    Debug.Assert(_zsHandles.Count == 0);
                    _zsHandles.Dispose();

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

        #region MainThread - Write: push raw data into the worker threads, and wait until getting the output
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        { // For Decompress
            throw new NotSupportedException("Read() not supported on compression");
        }

        /// <inheritdoc />
#if NETCOREAPP
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
            if (span.Length == 0)
                return;

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();

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

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();

            // Wait until the output is ready
            WaitWriteComplete(_latestSeq, _writeTimeout);

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();
        }

        private void EnqueueInputBuffer(bool isFinal)
        {
            if (_finalEnqueued)
                throw new InvalidOperationException("The final block has already been enqueued.");

            // Do nothing if all compression is already done.
            if (_compSortChunk.Completion.IsCompleted)
                return;

            // [RefCount]
            // First   block: _nextDictBuffer == null
            // Nor/Fin block: _nextDictBuffer != null, 1 <= _nextDictBuffer._refCount <= 2

            Debug.Assert(_inSeq == 0 && _nextDictBuffer == null ||
                _inSeq != 0 && _nextDictBuffer != null && !_nextDictBuffer.Disposed);

            _finalEnqueued |= isFinal;

            // _refCount of job.InBuffer and _nextDictBuffer is increased in constructor call.
            ZLibParallelCompressJob job = new ZLibParallelCompressJob(_pool, _inSeq, _inBlockSize, _nextDictBuffer, _outBlockSize);
            _latestSeq = job.Seq;
            _inSeq += 1;

            // [RefCount]
            // First   block: job.InBuffer._refCount == 1, (job.DictBuffer == _nextDictBuffer) == null
            // Nor/Fin block: job.InBuffer._refCount == 1, 2 <= (job.DictBuffer == _nextDictBuffer)._refCount <= 3

            if (isFinal)
                job.IsLastBlock = true;

            job.InBuffer.Write(_inputBuffer.ReadablePortionSpan, true);

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

                    ReadOnlySpan<byte> lastDictSpan;
                    if (copyLastDictSize <= _nextDictBuffer.DataEndIdx)
                        lastDictSpan = _nextDictBuffer.Buf.AsSpan(_nextDictBuffer.DataEndIdx - copyLastDictSize, copyLastDictSize);
                    else
                        lastDictSpan = _nextDictBuffer.Span;
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
            _compWorkChunk.SendAsync(job).Wait();
        }
        #endregion

        #region MainThread - Flush, Abort, FinalizeStream
        private void FinishWrite()
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

            Debug.Assert(_zsHandles.Count == _threads);

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();
        }

        /// <inheritdoc />
        public override void Flush()
        {
            if (ZLibInit.Lib == null)
                throw new ObjectDisposedException(nameof(ZLibInit));
            if (BaseStream == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();

            Debug.Assert(_inputBuffer.DataStartIdx == 0);

            // Flush the remaining input buffer into compress worker threads.
            if (!_inputBuffer.IsEmpty)
                EnqueueInputBuffer(false);

            // Check exceptions in Task instances.
            CheckBackgroundExceptions();

#if DEBUG_PARALLEL
            Console.WriteLine($"SEQ latest({_latestSeq}) wait({_waitSeq}) in({_inSeq}) out({_outSeq})");
#endif

            // Wait until all threads are idle.
            WaitWriteComplete(_latestSeq, null);

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

        private void WaitWriteComplete(long checkSeqNum, TimeSpan? waitMax)
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

        #region ZStreamProc
        public sealed class ZStreamHandle : IDisposable
        {
            public ZStreamBase? ZStream;
            private GCHandle _zsPin;
            public ZLibChecksumBase<uint>? BlockChecksum;

            private bool _disposed = false;

            public ZStreamHandle(ZLibOperateFormat format, ZLibCompLevel compLevel, ZLibWindowBits windowBits)
            {
                if (ZLibInit.Lib == null)
                    throw new ObjectDisposedException(nameof(ZLibInit));

                ZStream = ZLibInit.Lib.CreateZStream();
                _zsPin = GCHandle.Alloc(ZStream, GCHandleType.Pinned);

                BlockChecksum = FormatChecksum(format);

                // Always initialize zstream with -15 windowBits to use raw deflate stream.
                int windowBitsVal = DeflateStreamBase.ProcessFormatWindowBits(windowBits, ZLibStreamOperateMode.ParallelCompress, ZLibOperateFormat.Deflate);
                ZLibRet ret = ZLibInit.Lib.NativeAbi.DeflateInit(ZStream, compLevel, windowBitsVal, ZLibMemLevel.Default);
                ZLibException.CheckReturnValue(ret, ZStream);
            }

            ~ZStreamHandle()
            {
                Dispose(false);
            }
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (_disposed)
                    return;

                if (disposing)
                { // Dispose managed state.

                }

                // Dispose unmanaged resources, and set large fields to null.
                if (ZStream != null)
                {
                    if (ZLibInit.Lib == null)
                        throw new ObjectDisposedException(nameof(ZLibInit));

                    ZLibInit.Lib.NativeAbi.DeflateEnd(ZStream);
                    _zsPin.Free();
                    ZStream = null;
                }

                _disposed = true;
            }
        }
        #endregion

        #region CompressProc
        public unsafe ZLibParallelCompressJob CompressProc(ZLibParallelCompressJob job)
        {
            try
            {
                ZStreamHandle? zsh = null;
                try
                {
                    if (ZLibInit.Lib == null)
                        throw new ObjectDisposedException(nameof(ZLibInit));

                    zsh = _zsHandles.Take(_abortTokenSrc.Token);
                    if (zsh.ZStream == null)
                        throw new ObjectDisposedException("zlib stream handle had been disposed.");

                    // NOTE: Do not check for '0 < job.InBuffer.ReadableSize'.
                    // job of input size 0 is valid for lastInput.

                    // [RefCount]
                    // First  block:          1 <= job.InBuffer._refCount <= 3, job.DictBuffer == null
                    // Nor/Fin block (32K<=): 1 <= job.InBuffer._refCount <= 3, 1 <= job.DictBuffer._refCount <= 3
                    // Nor/Fin block (<32K):  job.InBuffer._refCount == 1,      job.DictBuffer._refCount == 1

                    // [Stage 02] Reset the zstream, and set the compression level again
                    ZLibRet ret = ZLibInit.Lib.NativeAbi.DeflateReset(zsh.ZStream);
                    ZLibException.CheckReturnValue(ret, zsh.ZStream);
                    ret = ZLibInit.Lib.NativeAbi.DeflateParams(zsh.ZStream, (int)_compLevel, (int)ZLibCompStrategy.Default);
                    ZLibException.CheckReturnValue(ret, zsh.ZStream);

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
                        unsafe
                        {
                            fixed (byte* dictPtr = job.DictBuffer.Buf)
                            {
                                ret = ZLibInit.Lib.NativeAbi.DeflateSetDictionary(zsh.ZStream, dictPtr + dictStartPos, (uint)dictSize);
                                ZLibException.CheckReturnValue(ret, zsh.ZStream);
                            }
                        }
                    }

                    // [Stage 10] Calculate checksum
                    if (zsh.BlockChecksum != null)
                    {
                        Debug.Assert(!job.InBuffer.Disposed);
                        Debug.Assert(job.InBuffer.DataStartIdx == 0);

                        zsh.BlockChecksum.Reset();
                        job.Checksum = zsh.BlockChecksum.Append(job.InBuffer.Buf, 0, job.InBuffer.DataEndIdx);
                    }

                    // [Stage 11] Compress (or finish) the input block
                    int bytesRead = 0;
                    if (!job.IsLastBlock)
                    { // Deflated block will end on a byte boundary, using a sync marker if necessary (SyncFlush)
                        // ADVACNED: Bit-level output manipulation.
                        // SIMPLE: In pre zlib 1.2.6, just call DeflateBlock once with ZLibFlush.SyncFlush.

                        // After Z_BLOCK, Up to 7 bits of output data are waiting to be written.
                        bytesRead = DeflateBlock(job, zsh, bytesRead, ZLibFlush.Block);

                        // How many bits are waiting to be written?
                        int bits = 0;
                        ret = ZLibInit.Lib.NativeAbi.DeflatePending(zsh.ZStream, null, &bits);
                        ZLibException.CheckReturnValue(ret, zsh.ZStream);

                        // Add enough empty blocks to get to a byte boundary
                        if (0 < (bits & 1)) // 1 bit is waiting to be written
                        { // Flush the bit-level boundary
                            bytesRead = DeflateBlock(job, zsh, bytesRead, ZLibFlush.SyncFlush);
                        }
                        else if (0 < (bits & 7)) // 3 bits or more are waiting to be written
                        { // Add static empty blocks
                            do
                            { // Insert bits to next output block
                                // Next output will start with bits leftover from a previous deflate() call.
                                // 10 bits
                                ret = ZLibInit.Lib.NativeAbi.DeflatePrime(zsh.ZStream, 10, 2);
                                ZLibException.CheckReturnValue(ret, zsh.ZStream);

                                // Still are 3 bits or more waiting to be written?
                                ret = ZLibInit.Lib.NativeAbi.DeflatePending(zsh.ZStream, null, &bits);
                                ZLibException.CheckReturnValue(ret, zsh.ZStream);
                            }
                            while (0 < (bits & 7));
                            bytesRead = DeflateBlock(job, zsh, bytesRead, ZLibFlush.Block);
                        }
                    }
                    else
                    { // Finish the deflate stream
                        bytesRead = DeflateBlockFinish(job, zsh, bytesRead);
                    }
                    
#if DEBUG_PARALLEL
                    Console.WriteLine($"-- compressed: {job}");
#endif

                    Debug.Assert(bytesRead == job.InBuffer.DataEndIdx);
                }
                finally
                {
                    // Free unnecessary resources
                    job.DictBuffer?.ReleaseRef();

                    // Push zsh into handle queue again
                    if (zsh != null)
                        _zsHandles.Add(zsh);

                    if (job.IsLastBlock)
                        _compWorkChunk.Complete();
                }
            }
            catch (Exception e)
            { // If any Exception has occured, abort the whole process.
                _abortTokenSrc.Cancel();
                _taskExcepts.Add(e);
            }

            return job;
        }

        private unsafe int DeflateBlock(ZLibParallelCompressJob job, ZStreamHandle zsh, int inputStartIdx, ZLibFlush flush)
        {
            if (ZLibInit.Lib == null)
                throw new ObjectDisposedException(nameof(ZLibInit));
            if (zsh.ZStream == null)
                throw new ObjectDisposedException("zlib stream handle had been disposed.");

            Debug.Assert(!job.InBuffer.Disposed);

            fixed (byte* inBufPtr = job.InBuffer.Buf) // [In] RAW
            {
                Debug.Assert(0 <= inputStartIdx && inputStartIdx <= job.InBuffer.DataEndIdx && job.InBuffer.DataStartIdx == 0);

                zsh.ZStream.NextIn = inBufPtr + inputStartIdx;
                zsh.ZStream.AvailIn = (uint)(job.InBuffer.DataEndIdx - inputStartIdx);

                // Loop as long as the output buffer is not full after running deflate()
                do
                {
                    // One compressed version of inBuffer data must fit in one outBuffer.
                    if (job.OutBuffer.IsFull)
                    { // Expand the outBuffer if the buffer is full.
                        int newSize = ZLibParallelCompressJob.CalcBufferExpandSize(job.OutBuffer.Capacity);
                        if (!job.OutBuffer.Expand(newSize))
                            throw new InvalidOperationException($"Failed to expand [{nameof(job.OutBuffer)}] to [{newSize}] bytes.");
                    }

                    fixed (byte* outBufPtr = job.OutBuffer.Buf) // [Out] Compressed
                    {
                        zsh.ZStream.NextOut = outBufPtr + job.OutBuffer.DataEndIdx;
                        zsh.ZStream.AvailOut = (uint)(job.OutBuffer.Capacity - job.OutBuffer.DataEndIdx);

                        uint beforeAvailIn = zsh.ZStream.AvailIn;
                        uint beforeAvailOut = zsh.ZStream.AvailOut;
                        ZLibRet ret = ZLibInit.Lib.NativeAbi.Deflate(zsh.ZStream, flush);
                        uint bytesRead = beforeAvailIn - zsh.ZStream.AvailIn;
                        uint bytesWritten = beforeAvailOut - zsh.ZStream.AvailOut;

#if DEBUG_PARALLEL
                        Console.WriteLine($"DeflateBlock1: {job} ({flush})");
#endif
                        inputStartIdx += (int)bytesRead;
                        job.OutBuffer.DataEndIdx += (int)bytesWritten;
#if DEBUG_PARALLEL
                        Console.WriteLine($"DeflateBlock2: {job} ({flush})");
#endif

                        ZLibException.CheckReturnValue(ret, zsh.ZStream);
                    }
                }
                while (zsh.ZStream.AvailOut == 0);

                Debug.Assert(zsh.ZStream.AvailIn == 0);
            }

            return inputStartIdx;
        }

        private unsafe int DeflateBlockFinish(ZLibParallelCompressJob job, ZStreamHandle zsh, int inputStartIdx)
        {
            if (ZLibInit.Lib == null)
                throw new ObjectDisposedException(nameof(ZLibInit));
            if (zsh.ZStream == null)
                throw new ObjectDisposedException("zlib stream handle had been disposed.");

            Debug.Assert(!job.InBuffer.Disposed);

            fixed (byte* inBufPtr = job.InBuffer.Buf) // [In] RAW
            {
                Debug.Assert(0 <= inputStartIdx && inputStartIdx <= job.InBuffer.DataEndIdx && job.InBuffer.DataStartIdx == 0);

                zsh.ZStream.NextIn = inBufPtr + inputStartIdx;
                zsh.ZStream.AvailIn = (uint)(job.InBuffer.DataEndIdx - inputStartIdx);

                // Loop as long as the output buffer is not full after running deflate()
                ZLibRet ret = ZLibRet.Ok;
                while (ret != ZLibRet.StreamEnd)
                {
                    // One compressed version of inBuffer data must fit in one outBuffer.
                    if (job.OutBuffer.IsFull)
                    { // Expand the outBuffer if the buffer is full.
                        int newSize = ZLibParallelCompressJob.CalcBufferExpandSize(job.OutBuffer.Capacity);
                        if (!job.OutBuffer.Expand(newSize))
                            throw new InvalidOperationException($"Failed to expand [{nameof(job.OutBuffer)}] to [{newSize}] bytes.");
                    }

                    fixed (byte* outBufPtr = job.OutBuffer.Buf) // [Out] Compressed
                    {
                        zsh.ZStream.NextOut = outBufPtr + job.OutBuffer.DataEndIdx;
                        zsh.ZStream.AvailOut = (uint)(job.OutBuffer.Capacity - job.OutBuffer.DataEndIdx);

                        uint beforeAvailIn = zsh.ZStream.AvailIn;
                        uint beforeAvailOut = zsh.ZStream.AvailOut;
                        ret = ZLibInit.Lib.NativeAbi.Deflate(zsh.ZStream, ZLibFlush.Finish);
                        uint bytesRead = beforeAvailIn - zsh.ZStream.AvailIn;
                        uint bytesWritten = beforeAvailOut - zsh.ZStream.AvailOut;

#if DEBUG_PARALLEL
                        Console.WriteLine($"DeflateBlockFinish2: {job}");
#endif
                        inputStartIdx += (int)bytesRead;
                        job.OutBuffer.DataEndIdx += (int)bytesWritten;
#if DEBUG_PARALLEL
                        Console.WriteLine($"DeflateBlockFinish3: {job}");
#endif

                        ZLibException.CheckReturnValue(ret, zsh.ZStream);
                    }
                }

                Debug.Assert(zsh.ZStream.AvailIn == 0);
            }

            return inputStartIdx;
        }
        #endregion

        #region WriteSortProc
        public async void WriteSortProc(ZLibParallelCompressJob item)
        {
            try
            {
                // Receive a ParallelCompressJob (which completed compressing), then put it into sorted list
                _outSet.Add(item);

                // Check if the jobs of right seq is available.
                // If available, post all of the designated jobs.
                while (0 < _outSet.Count)
                {
                    ZLibParallelCompressJob? outJob = _outSet.FirstOrDefault(x => x.Seq == _outSeq);
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
        /// <summary>
        /// WriterThread: Write compressed data into BaseStream
        /// </summary>
        internal unsafe void WriterProc(ZLibParallelCompressJob job)
        {
            try
            {
                try
                {
                    if (ZLibInit.Lib == null)
                        throw new ObjectDisposedException(nameof(ZLibInit));
                    if (BaseStream == null)
                        throw new ObjectDisposedException("This stream had been disposed.");

                    // Write to BaseStream
                    BaseStream.Write(job.OutBuffer.Buf, 0, job.OutBuffer.DataEndIdx);

#if DEBUG_PARALLEL
                    Console.WriteLine($"-- wrote: {job}");
#endif

                    // Increase TotalIn & TotalOut
                    TotalIn += job.InBuffer.DataEndIdx;
                    TotalOut += job.OutBuffer.DataEndIdx;

                    // Combine the checksum (if necessary)
                    _writeChecksum?.Combine(job.Checksum, job.InBuffer.ReadableSize);

                    if (job.IsLastBlock)
                    { // Write trailer
                        if (_writeChecksum != null)
                            WriteTrailer(_writeChecksum.Checksum, TotalIn);
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
                TotalOut += headBuf.Length;
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
                TotalOut += headBuf.Length;
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
                TotalOut += checkBuf.Length;
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
                TotalOut += trailBuf.Length;
            }
        }
        #endregion

        #region Exception Handling
        public void CheckBackgroundExceptions()
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
            List<Exception> innerExcepts = new List<Exception>();
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
            return Math.Max(blockSize, DefaultChunkSize); // At least 128KB
        }
        #endregion
    }
}
