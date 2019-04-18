/*
    Derived from LZ4 header files (BSD 2-Clause)
    Copyright (c) 2011-2016, Yann Collet

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2019 Hajin Jang

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
using System.Diagnostics;
using System.IO;
using System.Linq;
// ReSharper disable UnusedMember.Global

namespace Joveler.Compression.LZ4
{
    // ReSharper disable once InconsistentNaming
    public class LZ4FrameStream : Stream
    {
        #region Fields and Properties
        // Field
        private readonly LZ4Mode _mode;
        private readonly bool _leaveOpen;
        private bool _disposed = false;

        private IntPtr _cctx = IntPtr.Zero;
        private IntPtr _dctx = IntPtr.Zero;

        private readonly byte[] _workBuf;

        // Compression
        internal static int BufferSize = 16 * 1024; // 16K
        private readonly uint _destBufSize;

        // Decompression
        private const int DecompressComplete = -1;
        private bool _firstRead = true;
        private int _decompSrcIdx = 0;
        private int _decompSrcCount = 0;

        // Property
        public Stream BaseStream { get; private set; }

        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;

        // Const
        // https://github.com/lz4/lz4/blob/master/doc/lz4_Frame_format.md
        internal static uint FrameVersion;
        private static readonly byte[] FrameMagicNumber = { 0x04, 0x22, 0x4D, 0x18 }; // 0x184D2204 (LE)
        #endregion

        #region Constructor
        public LZ4FrameStream(Stream stream, LZ4Mode mode)
            : this(stream, mode, LZ4CompLevel.Default, false) { }

        public LZ4FrameStream(Stream stream, LZ4Mode mode, LZ4CompLevel compressionLevel)
            : this(stream, mode, compressionLevel, false) { }

        public LZ4FrameStream(Stream stream, LZ4Mode mode, bool leaveOpen)
            : this(stream, mode, LZ4CompLevel.Default, leaveOpen) { }

        public unsafe LZ4FrameStream(Stream stream, LZ4Mode mode, LZ4CompLevel compressionLevel, bool leaveOpen)
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));
            _mode = mode;
            _leaveOpen = leaveOpen;
            _disposed = false;

            switch (mode)
            {
                case LZ4Mode.Compress:
                    {
                        UIntPtr ret = NativeMethods.CreateFrameCompressionContext(ref _cctx, FrameVersion);
                        LZ4FrameException.CheckReturnValue(ret);

                        FramePreferences prefs = new FramePreferences
                        {
                            // Use default value of lz4 cli
                            FrameInfo = new FrameInfo
                            {
                                BlockSizeId = FrameBlockSizeId.Max4MB,
                                BlockMode = FrameBlockMode.BlockLinked,
                                ContentChecksumFlag = FrameContentChecksum.ContentChecksumEnabled,
                                FrameType = FrameType.Frame,
                                ContentSize = 0,
                                DictId = 0,
                                BlockChecksumFlag = FrameBlockChecksum.NoBlockChecksum,
                            },
                            CompressionLevel = (int)compressionLevel,
                            AutoFlush = 1,
                        };

                        UIntPtr frameSizeVal = NativeMethods.FrameCompressionBound((UIntPtr)BufferSize, prefs);
                        Debug.Assert(frameSizeVal.ToUInt64() <= int.MaxValue);

                        uint frameSize = frameSizeVal.ToUInt32();
                        if (BufferSize < frameSize)
                            _destBufSize = frameSize;

                        _workBuf = new byte[_destBufSize];

                        UIntPtr headerSizeVal;
                        fixed (byte* dest = _workBuf)
                        {
                            headerSizeVal = NativeMethods.FrameCompressionBegin(_cctx, dest, (UIntPtr)BufferSize, prefs);
                        }
                        LZ4FrameException.CheckReturnValue(headerSizeVal);
                        Debug.Assert(headerSizeVal.ToUInt64() < int.MaxValue);

                        int headerSize = (int)headerSizeVal.ToUInt32();
                        BaseStream.Write(_workBuf, 0, headerSize);
                        TotalOut += headerSize;

                        break;
                    }
                case LZ4Mode.Decompress:
                    {
                        UIntPtr ret = NativeMethods.CreateFrameDecompressionContext(ref _dctx, FrameVersion);
                        LZ4FrameException.CheckReturnValue(ret);

                        byte[] headerBuf = new byte[4];
                        int readHeaderSize = BaseStream.Read(headerBuf, 0, 4);
                        TotalIn += 4;

                        if (readHeaderSize != 4 || !headerBuf.SequenceEqual(FrameMagicNumber))
                            throw new InvalidDataException("BaseStream is not a valid LZ4 Frame Format");

                        _workBuf = new byte[BufferSize];

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }
        #endregion

        #region Disposable Pattern
        ~LZ4FrameStream()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (_cctx != IntPtr.Zero)
                { // Compress
                    FinishWrite();

                    UIntPtr ret = NativeMethods.FreeFrameCompressionContext(_cctx);
                    LZ4FrameException.CheckReturnValue(ret);

                    _cctx = IntPtr.Zero;
                }

                if (_dctx != IntPtr.Zero)
                {
                    UIntPtr ret = NativeMethods.FreeFrameDecompressionContext(_dctx);
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
        /// <summary>
        /// For Decompress
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_mode != LZ4Mode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || buffer.Length < offset + count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return 0;

            Span<byte> span = buffer.AsSpan(offset, count);
            return Read(span);
        }

        /// <summary>
        /// For Decompress
        /// </summary>
        public unsafe int Read(Span<byte> span)
        {
            if (_mode != LZ4Mode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");

            int readSize = 0;

            int destSize = span.Length;
            int destLeftBytes = span.Length;

            // Reached end of stream
            if (_decompSrcIdx == DecompressComplete)
                return 0;

            if (_firstRead)
            {
                // Write FrameMagicNumber into LZ4F_decompress
                UIntPtr headerSizeVal = (UIntPtr)4;
                UIntPtr destSizeVal = (UIntPtr)destSize;

                UIntPtr ret;
                fixed (byte* header = FrameMagicNumber)
                fixed (byte* dest = span)
                {
                    ret = NativeMethods.FrameDecompress(_dctx, dest, ref destSizeVal, header, ref headerSizeVal, null);
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
                    ret = NativeMethods.FrameDecompress(_dctx, dest, ref destSizeVal, src, ref srcSizeVal, null);
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
        /// <summary>
        /// For Compress
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_mode != LZ4Mode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || buffer.Length < offset + count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;

            ReadOnlySpan<byte> span = buffer.AsSpan(offset, count);
            Write(span);
        }

        /// <summary>
        /// For Compress
        /// </summary>
        public unsafe void Write(ReadOnlySpan<byte> span)
        {
            if (_mode != LZ4Mode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");

            int inputSize = span.Length;

            while (0 < span.Length)
            {
                int srcWorkSize = BufferSize < span.Length ? BufferSize : span.Length;

                UIntPtr outSizeVal;
                fixed (byte* dest = _workBuf)
                fixed (byte* src = span)
                {
                    outSizeVal = NativeMethods.FrameCompressionUpdate(_cctx, dest, (UIntPtr)_destBufSize, src, (UIntPtr)srcWorkSize, null);
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
            Debug.Assert(_mode == LZ4Mode.Compress, "FinishWrite() must not be called in decompression");

            UIntPtr outSizeVal;
            fixed (byte* dest = _workBuf)
            {
                outSizeVal = NativeMethods.FrameCompressionEnd(_cctx, dest, (UIntPtr)_destBufSize, null);
            }
            LZ4FrameException.CheckReturnValue(outSizeVal);

            Debug.Assert(outSizeVal.ToUInt64() < int.MaxValue, "BufferSize should be <2GB");
            int outSize = (int)outSizeVal.ToUInt64();

            BaseStream.Write(_workBuf, 0, outSize);
            TotalOut += outSize;
        }

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override bool CanRead => _mode == LZ4Mode.Decompress && BaseStream.CanRead;
        public override bool CanWrite => _mode == LZ4Mode.Compress && BaseStream.CanWrite;
        public override bool CanSeek => false;

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seek() not supported");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("SetLength not supported");
        }

        public override long Length => throw new NotSupportedException("Length not supported");

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
                    case LZ4Mode.Compress:
                        if (TotalIn == 0)
                            return 0;
                        return 100 - TotalOut * 100.0 / TotalIn;
                    case LZ4Mode.Decompress:
                        if (TotalOut == 0)
                            return 0;
                        return 100 - TotalIn * 100.0 / TotalOut;
                    default:
                        throw new InvalidOperationException("Internal Logic Error at LZ4Stream.CompressionRatio()");
                }
            }
        }
        #endregion
    }
}
