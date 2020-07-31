/*
    Derived from Zstandard header files (BSD 2-Clause)
    Copyright (c) 2016-present, Yann Collet, Facebook, Inc. All rights reserved.

    C# Wrapper written by Hajin Jang
    Copyright (C) 2020 Hajin Jang

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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Joveler.Compression.Zstd
{
    #region StreamOptions
    /// <summary>
    /// Compress options for LZ4FrameStream
    /// </summary>
    /// <remarks>
    /// Default value is based on default value of lz4 cli
    /// </remarks>
    public class ZstdCompressOptions
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
        /// 1 == always flush, to reduce usage of internal buffers
        /// </summary>
        public bool AutoFlush { get; set; } = false;
        /// <summary>
        /// 1 == parser favors decompression speed vs compression ratio. Only works for high compression modes (>= LZ4CompLevel.Level10)
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
        /// Whether to leave the base stream object open after disposing the lz4 stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
    }

    /// <summary>
    /// Decompress options for LZ4FrameStream
    /// </summary>
    public class LZ4FrameDecompressOptions
    {
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = LZ4FrameStream.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the lz4 stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
    }
    #endregion

    #region ZstdStream
    // ReSharper disable once InconsistentNaming
    public class ZstdStream : Stream
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

        private IntPtr _cctx = IntPtr.Zero;
        private IntPtr _dctx = IntPtr.Zero;

        private readonly int _bufferSize = DefaultBufferSize;
        private readonly byte[] _workBuf;

        // Compression
        private readonly uint _destBufSize;

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
        // https://github.com/lz4/lz4/blob/master/doc/lz4_Frame_format.md
        internal const uint FrameVersion = 100;
        internal const int DefaultBufferSize = 16 * 1024;
        private static readonly byte[] FrameMagicNumber = { 0x04, 0x22, 0x4D, 0x18 }; // 0x184D2204 (LE)
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing LZ4FrameStream.
        /// </summary>
        public unsafe LZ4FrameStream(Stream baseStream, LZ4FrameCompressOptions compOpts)
        {
            LZ4Init.Manager.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Compress;
            _disposed = false;

            // Check and set compress options
            _leaveOpen = compOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(compOpts.BufferSize);

            // Prepare cctx
            UIntPtr ret = LZ4Init.Lib.CreateFrameCompressContext(ref _cctx, FrameVersion);
            LZ4FrameException.CheckReturnValue(ret);

            // Prepare FramePreferences
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

            // Query the minimum required size of compress buffer
            // _bufferSize is the source size, frameSize is the (required) dest size
            UIntPtr frameSizeVal = LZ4Init.Lib.FrameCompressBound((UIntPtr)_bufferSize, prefs);
            Debug.Assert(frameSizeVal.ToUInt64() <= int.MaxValue);
            uint frameSize = frameSizeVal.ToUInt32();
            /*
            if (_bufferSize < frameSize)
                _destBufSize = frameSize;
            */
            _destBufSize = (uint)_bufferSize;
            if (_bufferSize < frameSize)
                _destBufSize = frameSize;
            _workBuf = new byte[_destBufSize];

            // Write the frame header into _workBuf
            UIntPtr headerSizeVal;
            fixed (byte* dest = _workBuf)
            {
                headerSizeVal = LZ4Init.Lib.FrameCompressBegin(_cctx, dest, (UIntPtr)_bufferSize, prefs);
            }
            LZ4FrameException.CheckReturnValue(headerSizeVal);
            Debug.Assert(headerSizeVal.ToUInt64() < int.MaxValue);

            int headerSize = (int)headerSizeVal.ToUInt32();
            BaseStream.Write(_workBuf, 0, headerSize);
            TotalOut += headerSize;
        }

        /// <summary>
        /// Create decompressing LZ4FrameStream.
        /// </summary>
        public unsafe LZ4FrameStream(Stream baseStream, LZ4FrameDecompressOptions compOpts)
        {
            LZ4Init.Manager.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Decompress;
            _disposed = false;

            // Check and set compress options
            _leaveOpen = compOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(compOpts.BufferSize);

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
        ~LZ4FrameStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
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

            int inputSize = span.Length;

            while (0 < span.Length)
            {
                int srcWorkSize = _bufferSize < span.Length ? _bufferSize : span.Length;

                UIntPtr outSizeVal;
                fixed (byte* dest = _workBuf)
                fixed (byte* src = span)
                {
                    outSizeVal = LZ4Init.Lib.FrameCompressUpdate(_cctx, dest, (UIntPtr)_destBufSize, src, (UIntPtr)srcWorkSize, null);
                }

                LZ4FrameException.CheckReturnValue(outSizeVal);

                Debug.Assert(outSizeVal.ToUInt64() < int.MaxValue, "BufferSize should be <2GB");
                int outSize = (int)outSizeVal.ToUInt64();

                BaseStream.Write(_workBuf, 0, outSize);
                TotalOut += outSize;

                span = span.Slice(srcWorkSize);
            }

            TotalIn += inputSize;
        }

        private unsafe void FinishWrite()
        {
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
