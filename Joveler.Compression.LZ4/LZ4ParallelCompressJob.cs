﻿/*   
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

using Joveler.Compression.LZ4.Buffer;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace Joveler.Compression.LZ4
{
    // // https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-implement-a-producer-consumer-dataflow-pattern

    internal sealed class LZ4ParallelCompressJob : IDisposable, IEquatable<LZ4ParallelCompressJob>, IComparable<LZ4ParallelCompressJob>
    {
        public long Seq { get; }
        public bool IsLastBlock { get; set; } = false;
        public bool IsEofBlock => Seq == EofBlockSeq;

        /// <summary>
        /// Acquired in the constructor, released in CompressThreadMain().
        /// </summary>
        public ReferableBuffer InBuffer { get; }
        /// <summary>
        /// Acquired in the EnqueueInputData(), released in CompressThreadMain().
        /// </summary>
        public ReferableBuffer? PrefixBuffer { get; }
        public PooledBuffer OutBuffer { get; }

        public int RawInputSize { get; set; } = 0;
        public uint Checksum { get; set; } = 0;

        private bool _disposed = false;

        public const int DictWindowSize = 64 * 1024;

        /// <summary>
        /// Seq of -1 means the eof block.
        /// WorkerThreads will terminate when receiving the eof block.
        /// </summary>
        /// <remarks>
        /// N * NormalJob -> FinalJob -> Threads * EofJob
        /// </remarks>
        public const int EofBlockSeq = -1;
        public const int WaitSeqInit = -2;

        /// <summary>
        /// Create an first/normal/final job which contains two block of input, one being the current data and another as a dictionary (last input).
        /// Most of the jobs are a normal job.
        /// </summary>
        /// <remarks>
        /// FirstJob -> N * NormalJob -> FinalJob -> Threads * EofJob
        /// </remarks>
        /// <param name="pool"></param>
        /// <param name="seqNum"></param>
        /// <param name="inBufferSize"></param>
        /// <param name="dictBuffer">
        /// dictBuffer is set to null in first block.
        /// In other blocks, dictBuffer is set to the previous block's InBuffer.
        /// </param>
        /// <param name="outBufferSize"></param>
        public LZ4ParallelCompressJob(ArrayPool<byte> pool, long seqNum, int inBufferSize, ReferableBuffer? dictBuffer, int outBufferSize)
        {
            Seq = seqNum;

            Debug.Assert(DictWindowSize <= inBufferSize);
            Debug.Assert(inBufferSize <= outBufferSize);

            InBuffer = new ReferableBuffer(pool, inBufferSize);
            PrefixBuffer = dictBuffer;
            OutBuffer = new PooledBuffer(pool, outBufferSize);

            Debug.Assert(Seq == 0 && dictBuffer == null || Seq != 0 && PrefixBuffer != null && !PrefixBuffer.Disposed);

            InBuffer.AcquireRef();
            PrefixBuffer?.AcquireRef();
        }

        /// <summary>
        /// Create an empty eofJob, which terminates the worker threads.
        /// </summary>
        /// <remarks>
        /// FirstJob -> N * NormalJob -> FinalJob -> Threads * EofJob
        /// </remarks>
        /// <param name="pool"></param>
        /// <param name="seqNum"></param>
        public LZ4ParallelCompressJob(ArrayPool<byte> pool, long seqNum)
        {
            Seq = seqNum;

            InBuffer = new ReferableBuffer(pool);
            PrefixBuffer = null;
            OutBuffer = new PooledBuffer(pool);

            InBuffer.AcquireRef();
        }

        ~LZ4ParallelCompressJob()
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
            { // Dispose managed state, and set large fields to null.

            }

            // Dispose unmanaged resources, and set large fields to null.
            InBuffer.ReleaseRef(); // ReleaseRef calls Dispose when necessary
            PrefixBuffer?.ReleaseRef(); // ReleaseRef calls Dispose when necessary
            OutBuffer.Dispose();

            _disposed = true;
        }

        /// <summary>
        /// Return 1.5x of the oldSize, with some safety checks.
        /// </summary>
        /// <param name="oldSize"></param>
        /// <returns>New size to be used to increase the buffer.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static int CalcBufferExpandSize(int oldSize)
        {
            if (oldSize < 0)
                throw new ArgumentOutOfRangeException(nameof(oldSize));
            if (oldSize == 0) // Return at least 32KB
                return DictWindowSize;

            // return 1.5x of the oldSize
            uint oldVal = (uint)oldSize;
            uint newVal;
            try
            {
                newVal = checked(oldVal + (oldVal >> 1));
            }
            catch (OverflowException)
            { // Overflow? return the max value (would not be likely)
                return int.MaxValue;
            }

            // .NET runtime maxes out plain buffer size at int.MaxValue
            if (int.MaxValue < newVal)
                return int.MaxValue;

            return (int)newVal;
        }

        public override string ToString()
        {
            return $"[JOB #{Seq,3}] [F={(Seq == 0 ? "F" : " ")}{(IsLastBlock ? "L" : " ")}]: in=[{InBuffer}] prefix=[{PrefixBuffer}] out=[{OutBuffer}]";
        }

        public override bool Equals(object? obj)
        {
            if (obj is not LZ4ParallelCompressJob other)
                return false;
            return Equals(other);
        }

        public bool Equals(LZ4ParallelCompressJob? other)
        {
            if (other == null)
                return false;

            return Seq == other.Seq;
        }

        public int CompareTo(LZ4ParallelCompressJob? other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return Seq.CompareTo(other.Seq);
        }

        public override int GetHashCode()
        {
            return Seq.GetHashCode();
        }
    }

    internal sealed class LZ4ParallelCompressJobComparator : IComparer<LZ4ParallelCompressJob>, IEqualityComparer<LZ4ParallelCompressJob>
    {
        public int Compare(LZ4ParallelCompressJob? x, LZ4ParallelCompressJob? y)
        {
            if (x == null)
                throw new ArgumentNullException(nameof(x));
            if (y == null)
                throw new ArgumentNullException(nameof(x));

            return x.CompareTo(y);
        }

        public bool Equals(LZ4ParallelCompressJob? x, LZ4ParallelCompressJob? y)
        {
            if (x == null)
                return y == null;
            if (y == null)
                return false;

            return x.Equals(y);
        }

        public int GetHashCode(LZ4ParallelCompressJob obj)
        {
            return obj.GetHashCode();
        }
    }
}
