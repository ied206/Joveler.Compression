#nullable enable 

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

using System;
using System.Buffers;

namespace Joveler.Compression.ZLib
{
    /// <summary>
    /// The stream which compresses zlib-related stream format in parallel.
    /// </summary>
    internal sealed class ParallelCompressJob : IDisposable
    {
        public long SeqNum { get; }
        public bool IsLastBlock { get; set; }

        public PooledBuffer InBuffer { get; }
        public PooledBuffer DictBuffer { get; }
        public PooledBuffer OutBuffer { get; }

        public int RawInputSize { get; set; } = 0;
        public uint Checksum { get; set; } = 0;

        private bool _disposed = false;

        private const int DictWindowSize = 32 * 1024;

        /// <summary>
        /// Seq of -1 means the eof block.
        /// WorkerThreads will terminate when receiving the eof block.
        /// </summary>
        /// <remarks>
        /// N * NormalJob -> FinalJob -> Threads * EofJob
        /// </remarks>
        public const int EofBlockSeqNum = -1;

        /// <summary>
        /// Create an normal job, which contains a block of input.
        /// Most of the jobs is a normal job.
        /// </summary>
        /// <remarks>
        /// N * NormalJob -> FinalJob -> Threads * EofJob
        /// </remarks>
        /// <param name="pool"></param>
        /// <param name="seqNum"></param>
        /// <param name="inBufferSize"></param>
        /// <param name="outBufferSize"></param>
        public ParallelCompressJob(ArrayPool<byte> pool, long seqNum, int inBufferSize, int outBufferSize)
        {
            SeqNum = seqNum;

            InBuffer = new PooledBuffer(pool, inBufferSize);
            DictBuffer = new PooledBuffer(pool, DictWindowSize);
            OutBuffer = new PooledBuffer(pool, outBufferSize);

            IsLastBlock = false;
        }

        /// <summary>
        /// Create an empty finalJob, which is the last block of the input.
        /// WorkerThread will run deflate() with ZLib.Finish.
        /// </summary>
        /// <remarks>
        /// N * NormalJob -> FinalJob -> Threads * EofJob
        /// </remarks>
        /// <param name="pool"></param>
        /// <param name="seqNum"></param>
        public ParallelCompressJob(ArrayPool<byte> pool, long seqNum, int outBufferSize)
        {
            SeqNum = seqNum;

            InBuffer = new PooledBuffer(pool);
            DictBuffer = new PooledBuffer(pool, DictWindowSize);
            OutBuffer = new PooledBuffer(pool, outBufferSize);

            IsLastBlock = true;
        }

        ~ParallelCompressJob()
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
            InBuffer.Dispose();
            DictBuffer.Dispose();
            OutBuffer.Dispose();

            _disposed = true;
        }
    }
}
