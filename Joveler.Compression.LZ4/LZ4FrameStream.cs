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

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Joveler.Compression.LZ4
{
    #region LZ4FrameStream
    public sealed class LZ4FrameStream : Stream
    {
        #region Fields and Properties
        private bool _disposed = false;

        internal const uint FrameVersion = 100;

        public Stream? BaseStream
        {
            get
            {
                if (_serialStream != null)
                    return _serialStream.BaseStream;
                if (_parallelStream != null)
                    return _parallelStream.BaseStream;
                throw new ObjectDisposedException("This stream had been disposed.");
            }
        }
        private long _totalIn = 0;
        public long TotalIn
        {
            get
            {
                if (_disposed)
                    return _totalIn;

                if (_serialStream != null)
                    _totalIn = _serialStream.TotalIn;
                if (_parallelStream != null)
                    _totalIn = _parallelStream.TotalIn;
                return _totalIn;
            }
        }
        private long _totalOut = 0;
        public long TotalOut
        {
            get
            {
                if (_disposed)
                    return _totalOut;

                if (_serialStream != null)
                    _totalOut = _serialStream.TotalOut;
                if (_parallelStream != null)
                    _totalOut = _parallelStream.TotalOut;
                return _totalOut;
            }
        }

        // Singlethread Compress/Decompress
        private LZ4FrameSerialStream? _serialStream = null;
        // Multithread Parallel Compress
        private LZ4FrameParallelStream? _parallelStream = null;

        /// <summary>
        /// Default buffer size for internal buffer, to be used in single-threaded operation.
        /// </summary>
        internal const int DefaultBufferSize = LZ4FrameSerialStream.DefaultBufferSize;
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing LZ4FrameStream.
        /// </summary>
        public unsafe LZ4FrameStream(Stream baseStream, LZ4FrameCompressOptions compOpts)
        {
            _serialStream = new LZ4FrameSerialStream(baseStream, compOpts);
        }

        public LZ4FrameStream(Stream baseStream, LZ4FrameParallelCompressOptions pcompOpts)
        {
            _parallelStream = new LZ4FrameParallelStream(baseStream, pcompOpts);
        }

        /// <summary>
        /// Create decompressing LZ4FrameStream.
        /// </summary>
        public unsafe LZ4FrameStream(Stream baseStream, LZ4FrameDecompressOptions decompOpts)
        {
            _serialStream = new LZ4FrameSerialStream(baseStream, decompOpts);
        }

        /*
        public LZ4FrameStream(Stream baseStream, LZ4FrameParallelDecompressOptions pdecompOpts)
        {
            _parallelStream = new LZ4FrameParallelStream(baseStream, pdecompOpts);
        }
        */
        #endregion

        #region Disposable Pattern
        ~LZ4FrameStream()
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

                // Dispose unmanaged resources, and set large fields to null.
                if (_serialStream != null)
                {
                    _serialStream.Dispose();

                    _totalIn = _serialStream.TotalIn;
                    _totalOut = _serialStream.TotalOut;

                    _serialStream = null;
                }

                if (_parallelStream != null)
                {
                    _parallelStream.Dispose();

                    _totalIn = _parallelStream.TotalIn;
                    _totalOut = _parallelStream.TotalOut;

                    _parallelStream = null;
                }

                _disposed = true;    
            }

            // Dispose the base class
            base.Dispose(disposing);
        }
        #endregion

        #region Stream Methods and Properties
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_parallelStream != null)
                return _parallelStream.Read(buffer, offset, count);

            if (_serialStream != null)
                return _serialStream.Read(buffer, offset, count);

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        /// <inheritdoc />
#if NETCOREAPP
        public override int Read(Span<byte> span)
#else
        public int Read(Span<byte> span)
#endif
        {
            if (_parallelStream != null)
                return _parallelStream.Read(span);

            if (_serialStream != null)
                return _serialStream.Read(span);

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_parallelStream != null)
            {
                _parallelStream.Write(buffer, offset, count);
                return;
            }

            if (_serialStream != null)
            {
                _serialStream.Write(buffer, offset, count);
                return;
            }

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        /// <inheritdoc />
#if NETCOREAPP
        public override void Write(ReadOnlySpan<byte> span)
#else
        public void Write(ReadOnlySpan<byte> span)
#endif
        {
            if (_parallelStream != null)
            {
                _parallelStream.Write(span);
                return;
            }

            if (_serialStream != null)
            {
                _serialStream.Write(span);
                return;
            }

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        /// <inheritdoc />
        public override void Flush()
        {
            if (_parallelStream != null)
            {
                _parallelStream.Flush();
                return;
            }

            if (_serialStream != null)
            {
                _serialStream.Flush();
                return;
            }

            throw new ObjectDisposedException("This stream had been disposed.");
        }

        /// <inheritdoc />
        public override bool CanRead
        {
            get
            {
                if (_parallelStream != null)
                    return _parallelStream.CanRead;
                else if (_serialStream != null)
                    return _serialStream.CanRead;
                else
                    return false;
            }
        }
        /// <inheritdoc />
        public override bool CanWrite
        {
            get
            {
                if (_parallelStream != null)
                    return _parallelStream.CanWrite;
                else if (_serialStream != null)
                    return _serialStream.CanWrite;
                else
                    return false;
            }
        }
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
                if (_parallelStream != null)
                    return _parallelStream.CompressionRatio;
                if (_serialStream != null)
                    return _serialStream.CompressionRatio;
                throw new ObjectDisposedException("This stream had been disposed.");
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
        #endregion
    }
    #endregion
}
