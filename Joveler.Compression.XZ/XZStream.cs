/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2019 Hajin Jang

    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

namespace Joveler.Compression.XZ
{
    #region StreamOptions
    public class XZCompressOptions
    {
        public uint Preset { get; set; } = XZStream.DefaultPreset;
        public LzmaCheck Check { get; set; } = LzmaCheck.Crc64;

        internal LzmaMt ToLzmaMt(XZThreadedCompressOptions threadOpts)
        {
            return new LzmaMt()
            {
                BlockSize = threadOpts.BlockSize,
                TimeOut = threadOpts.TimeOut,
                Threads = threadOpts.Threads,
                Preset = Preset,
                Check = Check,
            };
        }
    }

    public class XZThreadedCompressOptions
    {
        public ulong BlockSize { get; set; } = 0;
        public uint TimeOut { get; set; } = 0;
        public uint Threads { get; set; } = 1;
    }

    public class XZDecompressOptions
    {
        public ulong MemLimit { get; set; } = ulong.MaxValue;
        public LzmaDecodingFlag DecodeFlags { get; set; } = XZStream.DefaultDecodingFlag;
    }

    public class XZStreamOptions
    {
        public int BufferSize { get; set; } = XZStream.DefaultBufferSize;
        public bool LeaveOpen { get; set; } = false;
    }
    #endregion

    #region XZStream
    // ReSharper disable once InconsistentNaming
    public class XZStream : Stream
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
        private readonly bool _leaveOpen;
        private bool _disposed = false;

        private LzmaStream _lzmaStream;
        private GCHandle _lzmaStreamPin;
        private readonly int _bufferSize = DefaultBufferSize;

        private int _internalBufPos = 0;
        private const int ReadDone = -1;
        private readonly byte[] _internalBuf;

        // Property
        public Stream BaseStream { get; private set; }
        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;
        /// <summary>
        /// Only valid in Compress mode
        /// </summary>
        public ulong MaxMemUsage { get; private set; } = ulong.MaxValue;

        // Const
        internal const int DefaultBufferSize = 64 * 1024;
        internal const LzmaDecodingFlag DefaultDecodingFlag = LzmaDecodingFlag.Concatenated;
        public const uint MinimumPreset = 0;
        public const uint DefaultPreset = 6;
        public const uint MaximumPreset = 9;
        public const uint ExtremeFlag = 1u << 31;
        #endregion

        #region Constructor
        /// <summary>
        /// Create single-threaded compressing XZStream
        /// </summary>
        public unsafe XZStream(Stream baseStream, XZCompressOptions compOpts) 
            : this(baseStream, compOpts, new XZStreamOptions()) { }

        /// <summary>
        /// Create single-threaded compressing XZStream
        /// </summary>
        public unsafe XZStream(Stream baseStream, XZCompressOptions compOpts, XZStreamOptions advOpts)
        {
            NativeMethods.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Compress;
            _disposed = false;

            // Check and set XZStreamOptions
            _leaveOpen = advOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(advOpts.BufferSize);
            _internalBuf = new byte[_bufferSize];

            // Prepare LzmaStream and buffers
            _lzmaStream = new LzmaStream();
            _lzmaStreamPin = GCHandle.Alloc(_lzmaStream, GCHandleType.Pinned);

            // Check LzmaMt instance
            CheckPreset(compOpts.Preset);

            // Initialize the encoder
            LzmaRet ret = NativeMethods.LzmaEasyEncoder(_lzmaStream, compOpts.Preset, compOpts.Check);
            XZException.CheckReturnValue(ret);

            // Set possible max memory usage.
            MaxMemUsage = NativeMethods.LzmaEasyEncoderMemUsage(compOpts.Preset);
        }

        /// <summary>
        /// Create multi-threaded compressing XZStream. Requires more memory than single-threaded mode.
        /// </summary>
        public unsafe XZStream(Stream baseStream, XZCompressOptions compOpts, XZThreadedCompressOptions threadOpts)
            : this(baseStream, compOpts, threadOpts, new XZStreamOptions()) { }

        /// <summary>
        /// Create multi-threaded compressing XZStream. Requires more memory than single-threaded mode.
        /// </summary>
        public unsafe XZStream(Stream baseStream, XZCompressOptions compOpts, XZThreadedCompressOptions threadOpts, XZStreamOptions advOpts)
        {
            NativeMethods.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Compress;
            _disposed = false;

            // Check and set XZStreamOptions
            _leaveOpen = advOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(advOpts.BufferSize);
            _internalBuf = new byte[_bufferSize];

            // Prepare LzmaStream and buffers
            _lzmaStream = new LzmaStream
            {
                NextIn = (byte*)0,
                AvailIn = 0
            };
            _lzmaStreamPin = GCHandle.Alloc(_lzmaStream, GCHandleType.Pinned);

            // Check LzmaMt instance
            LzmaMt mt = compOpts.ToLzmaMt(threadOpts);
            CheckPreset(mt.Preset);
            mt.Threads = CheckThreadCount(mt.Threads);

            // Initialize the encoder
            LzmaRet ret = NativeMethods.LzmaStreamEncoderMt(_lzmaStream, mt);
            XZException.CheckReturnValue(ret);

            // Set possible max memory usage.
            MaxMemUsage = NativeMethods.LzmaStreamEncoderMtMemUsage(mt);
        }

        /// <summary>
        /// Create decompressing XZStream
        /// </summary>
        public unsafe XZStream(Stream baseStream, XZDecompressOptions decompOpts)
            : this(baseStream, decompOpts, new XZStreamOptions()) { }

        /// <summary>
        /// Create decompressing XZStream
        /// </summary>
        public unsafe XZStream(Stream baseStream, XZDecompressOptions decompOpts, XZStreamOptions advOpts)
        {
            NativeMethods.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Decompress;
            _disposed = false;

            // Check and set XZStreamOptions
            _leaveOpen = advOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(advOpts.BufferSize);
            _internalBuf = new byte[_bufferSize];

            // Prepare LzmaStream and buffers
            _lzmaStream = new LzmaStream();
            _lzmaStreamPin = GCHandle.Alloc(_lzmaStream, GCHandleType.Pinned);

            // Initialize the decoder
            LzmaRet ret = NativeMethods.LzmaStreamDecoder(_lzmaStream, decompOpts.MemLimit, decompOpts.DecodeFlags);
            XZException.CheckReturnValue(ret);
        }
        #endregion

        #region Disposable Pattern
        ~XZStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (_lzmaStream != null)
                {
                    if (_mode == Mode.Compress)
                    {
                        Flush();
                        FinishWrite();
                    }
                    else
                    {
                        _internalBufPos = ReadDone;
                    }

                    NativeMethods.LzmaEnd(_lzmaStream);
                    _lzmaStreamPin.Free();
                    _lzmaStream = null;
                }

                if (BaseStream != null)
                {
                    if (!_leaveOpen)
                        BaseStream.Dispose();
                    BaseStream = null;
                }

                _disposed = true;
            }
        }
        #endregion

        #region Stream Methods and Properties
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        { // For Decompress
            if (_mode != Mode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");
            CheckReadWriteArgs(buffer, offset, count);
            if (count == 0)
                return 0;

            Span<byte> span = buffer.AsSpan(offset, count);
            return Read(span);
        }

        /// <inheritdoc />
        public unsafe int Read(Span<byte> span)
        { // For Decompress
            if (_mode != Mode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");
            if (_internalBufPos == ReadDone)
                return 0;

            int readSize = 0;
            LzmaAction action = LzmaAction.Run;

            fixed (byte* readPtr = _internalBuf)
            fixed (byte* writePtr = span)
            {
                _lzmaStream.NextIn = readPtr + _internalBufPos;
                _lzmaStream.NextOut = writePtr;
                _lzmaStream.AvailOut = (uint)span.Length;

                while (_lzmaStream.AvailOut != 0)
                {
                    if (_lzmaStream.AvailIn == 0)
                    {
                        // Read from _baseStream
                        int baseReadSize = BaseStream.Read(_internalBuf, 0, _internalBuf.Length);
                        TotalIn += baseReadSize;

                        _internalBufPos = 0;
                        _lzmaStream.NextIn = readPtr;
                        _lzmaStream.AvailIn = (uint)baseReadSize;

                        if (baseReadSize == 0) // End of stream
                            action = LzmaAction.Finish;
                    }

                    ulong bakAvailIn = _lzmaStream.AvailIn;
                    ulong bakAvailOut = _lzmaStream.AvailOut;

                    LzmaRet ret = NativeMethods.LzmaCode(_lzmaStream, action);

                    _internalBufPos += (int)(bakAvailIn - _lzmaStream.AvailIn);
                    readSize += (int)(bakAvailOut - _lzmaStream.AvailOut);

                    // Once everything has been decoded successfully, the return value of lzma_code() will be LZMA_STREAM_END.
                    if (ret == LzmaRet.StreamEnd)
                    {
                        _internalBufPos = ReadDone;
                        break;
                    }

                    // Normally the return value of lzma_code() will be LZMA_OK until everything has been encoded.
                    XZException.CheckReturnValue(ret);
                }
            }

            TotalOut += readSize;
            return readSize;
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        { // For Compress
            if (_mode != Mode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");
            CheckReadWriteArgs(buffer, offset, count);
            if (count == 0)
                return;

            ReadOnlySpan<byte> span = buffer.AsSpan(offset, count);
            Write(span);
        }

        /// <inheritdoc />
        public unsafe void Write(ReadOnlySpan<byte> span)
        { // For Compress
            if (_mode != Mode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");

            TotalIn += span.Length;

            fixed (byte* readPtr = span)
            fixed (byte* writePtr = _internalBuf)
            {
                _lzmaStream.NextIn = readPtr;
                _lzmaStream.AvailIn = (uint)span.Length;
                _lzmaStream.NextOut = writePtr + _internalBufPos;
                _lzmaStream.AvailOut = (uint)(_internalBuf.Length - _internalBufPos);

                // Return condition : _lzmaStream.AvailIn == 0
                while (_lzmaStream.AvailIn != 0)
                {
                    LzmaRet ret = NativeMethods.LzmaCode(_lzmaStream, LzmaAction.Run);
                    _internalBufPos = (int)((ulong)_internalBuf.Length - _lzmaStream.AvailOut);

                    // If the output buffer is full, write the data from the output bufffer to the output file.
                    if (_lzmaStream.AvailOut == 0)
                    {
                        // Write to _baseStream
                        BaseStream.Write(_internalBuf, 0, _internalBuf.Length);
                        TotalOut += _internalBuf.Length;

                        // Reset NextOut and AvailOut
                        _internalBufPos = 0;
                        _lzmaStream.NextOut = writePtr;
                        _lzmaStream.AvailOut = (uint)_internalBuf.Length;
                    }

                    // Normally the return value of lzma_code() will be LZMA_OK until everything has been encoded.
                    XZException.CheckReturnValue(ret);
                }
            }
        }

        private unsafe void FinishWrite()
        {
            Debug.Assert(_mode == Mode.Compress, "FinishWrite() must not be called in decompression");

            fixed (byte* writePtr = _internalBuf)
            {
                _lzmaStream.NextIn = (byte*)0;
                _lzmaStream.AvailIn = 0;
                _lzmaStream.NextOut = writePtr + _internalBufPos;
                _lzmaStream.AvailOut = (uint)(_internalBuf.Length - _internalBufPos);

                LzmaRet ret = LzmaRet.Ok;
                while (ret != LzmaRet.StreamEnd)
                {
                    ulong bakAvailOut = _lzmaStream.AvailOut;
                    ret = NativeMethods.LzmaCode(_lzmaStream, LzmaAction.Finish);
                    _internalBufPos = (int)(bakAvailOut - _lzmaStream.AvailOut);

                    // If the compression finished successfully,
                    // write the data from the output buffer to the output file.
                    if (_lzmaStream.AvailOut == 0 || ret == LzmaRet.StreamEnd)
                    { // Write to _baseStream
                        // When lzma_code() has returned LZMA_STREAM_END, the output buffer is likely to be only partially
                        // full. Calculate how much new data there is to be written to the output file.
                        BaseStream.Write(_internalBuf, 0, _internalBufPos);
                        TotalOut += _internalBufPos;

                        // Reset NextOut and AvailOut
                        _internalBufPos = 0;
                        _lzmaStream.NextOut = writePtr;
                        _lzmaStream.AvailOut = (uint)_internalBuf.Length;
                    }
                    else
                    { // Once everything has been encoded successfully, the return value of lzma_code() will be LZMA_STREAM_END.
                        XZException.CheckReturnValue(ret);
                    }
                }
            }
        }

        public override unsafe void Flush()
        {
            if (_mode == Mode.Decompress)
            {
                BaseStream.Flush();
                return;
            }

            fixed (byte* writePtr = _internalBuf)
            {
                _lzmaStream.NextIn = (byte*)0;
                _lzmaStream.AvailIn = 0;
                _lzmaStream.NextOut = writePtr + _internalBufPos;
                _lzmaStream.AvailOut = (uint)(_internalBuf.Length - _internalBufPos);

                LzmaRet ret = LzmaRet.Ok;
                while (ret != LzmaRet.StreamEnd)
                {
                    int writeSize = 0;
                    if (_lzmaStream.AvailOut != 0)
                    {
                        ulong bakAvailOut = _lzmaStream.AvailOut;
                        ret = NativeMethods.LzmaCode(_lzmaStream, LzmaAction.FullFlush);
                        writeSize += (int)(bakAvailOut - _lzmaStream.AvailOut);
                    }
                    _internalBufPos += writeSize;

                    BaseStream.Write(_internalBuf, 0, _internalBufPos);
                    TotalOut += _internalBufPos;

                    // Reset NextOut and AvailOut
                    _internalBufPos = 0;
                    _lzmaStream.NextOut = writePtr;
                    _lzmaStream.AvailOut = (uint)_internalBuf.Length;

                    // Once everything has been encoded successfully, the return value of lzma_code() will be LZMA_STREAM_END.
                    if (ret != LzmaRet.Ok && ret != LzmaRet.StreamEnd)
                        throw new XZException(ret);
                }
            }

            BaseStream.Flush();
        }

        public override bool CanRead => _mode == Mode.Decompress && BaseStream.CanRead;
        public override bool CanWrite => _mode == Mode.Compress && BaseStream.CanWrite;
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
                if (_mode == Mode.Compress)
                {
                    if (TotalIn == 0)
                        return 0;
                    return 100 - TotalOut * 100.0 / TotalIn;
                }
                else
                {
                    if (TotalOut == 0)
                        return 0;
                    return 100 - TotalIn * 100.0 / TotalOut;
                }
            }
        }
        #endregion

        #region GetProgress
        /// <summary>
        /// Get progress information
        /// </summary>
        /// <remarks>
        /// In single-threaded mode, applications can get progress information from 
        /// strm->total_in and strm->total_out.In multi-threaded mode this is less
        /// useful because a significant amount of both input and output data gets
        /// buffered internally by liblzma.This makes total_in and total_out give
        /// misleading information and also makes the progress indicator updates
        /// non-smooth.
        /// 
        /// This function gives realistic progress information also in multi-threaded
        /// mode by taking into account the progress made by each thread. In
        /// single-threaded mode *progress_in and *progress_out are set to
        /// strm->total_in and strm->total_out, respectively.
        /// </remarks>
        public void GetProgress(out ulong progressIn, out ulong progressOut)
        {
            progressIn = 0;
            progressOut = 0;
            NativeMethods.LzmaGetProgress(_lzmaStream, ref progressIn, ref progressOut);
        }
        #endregion

        #region (internal) Check Arguments
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
        internal static int CheckThreadCount(int threads)
        {
            if (threads < 0)
                throw new ArgumentOutOfRangeException(nameof(threads));
            if (threads == 0) // Use system's thread number by default
                threads = Environment.ProcessorCount;
            else if (Environment.ProcessorCount < threads) // If the number of CPU cores/threads exceeds system thread number,
                threads = Environment.ProcessorCount; // Limit the number of threads to keep memory usage lower.
            return threads;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint CheckThreadCount(uint threads)
        {
            if (threads == 0) // Use system's thread number by default
                threads = (uint)Environment.ProcessorCount;
            else if (Environment.ProcessorCount < threads) // If the number of CPU cores/threads exceeds system thread number,
                threads = (uint)Environment.ProcessorCount; // Limit the number of threads to keep memory usage lower.
            return threads;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckPreset(uint preset)
        {
            if (!(MinimumPreset <= preset && preset <= MaximumPreset) &&
                !((MinimumPreset | ExtremeFlag) <= preset && preset <= (MaximumPreset | ExtremeFlag)))
                throw new ArgumentOutOfRangeException(nameof(preset));
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
