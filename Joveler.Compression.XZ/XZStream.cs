/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2020 Hajin Jang

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
        /// <summary>
        /// Select a compression preset level. 
        /// The default is 6. If multiple preset levels are specified, the last one takes effect. 
        /// </summary>
        public LzmaCompLevel Level { get; set; } = LzmaCompLevel.Default;
        /// <summary>
        /// Use a slower variant of the selected compression preset level (−0 ... −9) to hopefully
        /// get a little bit better compression ratio, but with bad luck this can also make it worse.
        /// Decompressor memory usage is not affected, but compressor memory usage increases a little at preset levels −0 ... −3.
        /// </summary>
        /// <remarks>
        /// The differences between the presets are more significant than with gzip(1) and bzip2(1). 
        /// The selected compression settings determine the memory requirements of the decompressor, 
        /// thus using a too high preset level might make it painful to decompress the file on an old system with little RAM. 
        /// Specifically, it’s not a good idea to blindly use −9 for everything like it often is with gzip(1) and bzip2(1).
        /// </remarks>
        public bool ExtremeFlag { get; set; } = false;
        public uint Preset => ToPreset(Level, ExtremeFlag);

        /// <summary>
        /// Specify the type of the integrity check. 
        /// The check is calculated from the uncompressed data and stored in the .xz file.
        /// </summary>
        public LzmaCheck Check { get; set; } = LzmaCheck.Crc64;
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = XZStream.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the xz stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;

        internal static uint ToPreset(LzmaCompLevel level, bool extremeFlag)
        {
            uint preset = (uint)level;
            uint extreme = extremeFlag ? (1u << 31) : 0u;
            return preset | extreme;
        }

        internal LzmaMt ToLzmaMt(XZThreadedCompressOptions threadOpts)
        {
            return new LzmaMt()
            {
                BlockSize = threadOpts.BlockSize,
                Threads = XZThreadedCompressOptions.CheckThreadCount(threadOpts.Threads),
                Preset = Preset,
                Check = Check,
            };
        }
    }

    public class XZThreadedCompressOptions
    {
        /// <summary>
        /// Maximum uncompressed size of a Block.
        /// When compressing to the .xz format, split the input data into blocks of size bytes.
        /// The blocks are compressed independently from each other, which helps with multithreading and makes limited random-access decompression possible.
        /// </summary>
        /// <remarks>
        /// In multi-threaded mode about three times size bytes will be allocated in each thread for buffering input and output.
        /// The default size is three times the LZMA2 dictionary size or 1 MiB, whichever is more.
        /// Typically a good value is 2−4 times the size of the LZMA2 dictionary or at least 1 MiB.
        /// Using size less than the LZMA2 dictionary size is waste of RAM because then the LZMA2 dictionary buffer will never get fully used. 
        /// The sizes of the blocks are stored in the block headers, which a future version of xz will use for multi-threaded decompression.
        /// </remarks>
        public ulong BlockSize { get; set; } = 0;
        /// <summary>
        /// Number of worker threads to use.
        /// </summary>
        public int Threads { get; set; } = 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint CheckThreadCount(int threads)
        {
            if (threads < 0)
                throw new ArgumentOutOfRangeException(nameof(threads));
            if (threads == 0) // Use system's thread number by default
                threads = Environment.ProcessorCount;
            else if (Environment.ProcessorCount < threads) // If the number of CPU cores/threads exceeds system thread number,
                threads = Environment.ProcessorCount; // Limit the number of threads to keep memory usage lower.
            return (uint)threads;
        }
    }

    public class XZDecompressOptions
    {
        public ulong MemLimit { get; set; } = ulong.MaxValue;
        public LzmaDecodingFlag DecodeFlags { get; set; } = XZStream.DefaultDecodingFlags;
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = XZStream.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the xz stream object.
        /// </summary>
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
        private int _workBufPos = 0;
        private readonly byte[] _workBuf;

        // Property
        public Stream BaseStream { get; private set; }
        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;
        /// <summary>
        /// Only valid in Compress mode
        /// </summary>
        public ulong MaxMemUsage { get; private set; } = ulong.MaxValue;

        // Const
        private const int ReadDone = -1;
        internal const LzmaDecodingFlag DefaultDecodingFlags = LzmaDecodingFlag.Concatenated;

        // Readonly
        internal static readonly uint MinPreset = XZCompressOptions.ToPreset(LzmaCompLevel.Level0, false);
        internal static readonly uint MaxPreset = XZCompressOptions.ToPreset(LzmaCompLevel.Level9, false);
        internal static readonly uint MinExtremePreset = XZCompressOptions.ToPreset(LzmaCompLevel.Level0, true);
        internal static readonly uint MaxExtremePreset = XZCompressOptions.ToPreset(LzmaCompLevel.Level9, true);

        // Default Buffer Size
        /* Benchmark - Performance of each buffer size is within error range.
           LZMA2 is a slow algorithm, so pinvoke overhead impact is minimal.
        AMD Ryzen 5 3600 / .NET Core 3.1.13 / Windows 10.0.19042 x64 / xz-utils 5.2.5
        | Method | BufferSize |     Mean |    Error |   StdDev |
        |------- |----------- |---------:|---------:|---------:|
        |     XZ |      16384 | 37.06 ms | 0.499 ms | 0.467 ms |
        |     XZ |      32768 | 36.79 ms | 0.152 ms | 0.127 ms |
        |     XZ |      65536 | 36.85 ms | 0.497 ms | 0.464 ms |
        |     XZ |     131072 | 37.03 ms | 0.393 ms | 0.349 ms |
        |     XZ |     262144 | 37.23 ms | 0.486 ms | 0.454 ms |
        |     XZ |     524288 | 37.67 ms | 0.531 ms | 0.497 ms |
        |     XZ |    1048576 | 36.87 ms | 0.268 ms | 0.251 ms |
        |     XZ |    2097152 | 37.14 ms | 0.246 ms | 0.218 ms |
        |     XZ |    4194304 | 37.29 ms | 0.354 ms | 0.331 ms |
         */
        internal const int DefaultBufferSize = 1024 * 1024;
        #endregion

        #region Constructor
        /// <summary>
        /// Create single-threaded compressing XZStream.
        /// </summary>
        public unsafe XZStream(Stream baseStream, XZCompressOptions compOpts)
        {
            XZInit.Manager.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Compress;
            _disposed = false;

            // Check and set compress options
            _leaveOpen = compOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(compOpts.BufferSize);
            _workBuf = new byte[_bufferSize];

            // Prepare LzmaStream
            _lzmaStream = new LzmaStream();
            _lzmaStreamPin = GCHandle.Alloc(_lzmaStream, GCHandleType.Pinned);

            // Check preset
            uint preset = compOpts.Preset;
            CheckPreset(preset);

            // Initialize the encoder
            LzmaRet ret = XZInit.Lib.LzmaEasyEncoder(_lzmaStream, preset, compOpts.Check);
            XZException.CheckReturnValue(ret);

            // Set possible max memory usage.
            MaxMemUsage = XZInit.Lib.LzmaEasyEncoderMemUsage(preset);
        }

        /// <summary>
        /// Create multi-threaded compressing XZStream. Requires more memory than single-threaded mode.
        /// </summary>
        public unsafe XZStream(Stream baseStream, XZCompressOptions compOpts, XZThreadedCompressOptions threadOpts)
        {
            XZInit.Manager.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Compress;
            _disposed = false;

            // Check and set XZStreamOptions
            _leaveOpen = compOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(compOpts.BufferSize);
            _workBuf = new byte[_bufferSize];

            // Prepare LzmaStream and buffers
            _lzmaStream = new LzmaStream();
            _lzmaStreamPin = GCHandle.Alloc(_lzmaStream, GCHandleType.Pinned);

            // Check LzmaMt instance
            LzmaMt mt = compOpts.ToLzmaMt(threadOpts);
            CheckPreset(mt.Preset);

            // Initialize the encoder
            LzmaRet ret = XZInit.Lib.LzmaStreamEncoderMt(_lzmaStream, mt);
            XZException.CheckReturnValue(ret);

            // Set possible max memory usage.
            MaxMemUsage = XZInit.Lib.LzmaStreamEncoderMtMemUsage(mt);
        }

        /// <summary>
        /// Create decompressing XZStream
        /// </summary>
        public unsafe XZStream(Stream baseStream, XZDecompressOptions decompOpts)
        {
            XZInit.Manager.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Decompress;
            _disposed = false;

            // Check and set decompress options
            _leaveOpen = decompOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(decompOpts.BufferSize);
            _workBuf = new byte[_bufferSize];

            // Prepare LzmaStream and buffers
            _lzmaStream = new LzmaStream();
            _lzmaStreamPin = GCHandle.Alloc(_lzmaStream, GCHandleType.Pinned);

            // Initialize the decoder
            LzmaRet ret = XZInit.Lib.LzmaStreamDecoder(_lzmaStream, decompOpts.MemLimit, decompOpts.DecodeFlags);
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
                        _workBufPos = ReadDone;
                    }

                    XZInit.Lib.LzmaEnd(_lzmaStream);
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
#if NETSTANDARD2_1
        public override unsafe int Read(Span<byte> span)
#else
        public unsafe int Read(Span<byte> span)
#endif
        { // For Decompress
            if (_mode != Mode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");
            if (_workBufPos == ReadDone)
                return 0;

            int readSize = 0;
            LzmaAction action = LzmaAction.Run;

            fixed (byte* readPtr = _workBuf)
            fixed (byte* writePtr = span)
            {
                _lzmaStream.NextIn = readPtr + _workBufPos;
                _lzmaStream.NextOut = writePtr;
                _lzmaStream.AvailOut = (uint)span.Length;

                while (_lzmaStream.AvailOut != 0)
                {
                    if (_lzmaStream.AvailIn == 0)
                    {
                        // Read from _baseStream
                        int baseReadSize = BaseStream.Read(_workBuf, 0, _workBuf.Length);
                        TotalIn += baseReadSize;

                        _workBufPos = 0;
                        _lzmaStream.NextIn = readPtr;
                        _lzmaStream.AvailIn = (uint)baseReadSize;

                        if (baseReadSize == 0) // End of stream
                            action = LzmaAction.Finish;
                    }

                    ulong bakAvailIn = _lzmaStream.AvailIn;
                    ulong bakAvailOut = _lzmaStream.AvailOut;

                    LzmaRet ret = XZInit.Lib.LzmaCode(_lzmaStream, action);

                    _workBufPos += (int)(bakAvailIn - _lzmaStream.AvailIn);
                    readSize += (int)(bakAvailOut - _lzmaStream.AvailOut);

                    // Once everything has been decoded successfully, the return value of lzma_code() will be LZMA_STREAM_END.
                    if (ret == LzmaRet.StreamEnd)
                    {
                        _workBufPos = ReadDone;
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
#if NETSTANDARD2_1
        public override unsafe void Write(ReadOnlySpan<byte> span)
#else
        public unsafe void Write(ReadOnlySpan<byte> span)
#endif
        { // For Compress
            if (_mode != Mode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");

            TotalIn += span.Length;

            fixed (byte* readPtr = span)
            fixed (byte* writePtr = _workBuf)
            {
                _lzmaStream.NextIn = readPtr;
                _lzmaStream.AvailIn = (uint)span.Length;
                _lzmaStream.NextOut = writePtr + _workBufPos;
                _lzmaStream.AvailOut = (uint)(_workBuf.Length - _workBufPos);

                // Return condition : _lzmaStream.AvailIn == 0
                while (_lzmaStream.AvailIn != 0)
                {
                    LzmaRet ret = XZInit.Lib.LzmaCode(_lzmaStream, LzmaAction.Run);
                    _workBufPos = (int)((ulong)_workBuf.Length - _lzmaStream.AvailOut);

                    // If the output buffer is full, write the data from the output bufffer to the output file.
                    if (_lzmaStream.AvailOut == 0)
                    {
                        // Write to _baseStream
                        BaseStream.Write(_workBuf, 0, _workBuf.Length);
                        TotalOut += _workBuf.Length;

                        // Reset NextOut and AvailOut
                        _workBufPos = 0;
                        _lzmaStream.NextOut = writePtr;
                        _lzmaStream.AvailOut = (uint)_workBuf.Length;
                    }

                    // Normally the return value of lzma_code() will be LZMA_OK until everything has been encoded.
                    XZException.CheckReturnValue(ret);
                }
            }
        }

        private unsafe void FinishWrite()
        {
            Debug.Assert(_mode == Mode.Compress, "FinishWrite() must not be called in decompression");

            fixed (byte* writePtr = _workBuf)
            {
                _lzmaStream.NextIn = (byte*)0;
                _lzmaStream.AvailIn = 0;
                _lzmaStream.NextOut = writePtr + _workBufPos;
                _lzmaStream.AvailOut = (uint)(_workBuf.Length - _workBufPos);

                LzmaRet ret = LzmaRet.Ok;
                while (ret != LzmaRet.StreamEnd)
                {
                    ulong bakAvailOut = _lzmaStream.AvailOut;
                    ret = XZInit.Lib.LzmaCode(_lzmaStream, LzmaAction.Finish);
                    _workBufPos = (int)(bakAvailOut - _lzmaStream.AvailOut);

                    // If the compression finished successfully,
                    // write the data from the output buffer to the output file.
                    if (_lzmaStream.AvailOut == 0 || ret == LzmaRet.StreamEnd)
                    { // Write to _baseStream
                        // When lzma_code() has returned LZMA_STREAM_END, the output buffer is likely to be only partially
                        // full. Calculate how much new data there is to be written to the output file.
                        BaseStream.Write(_workBuf, 0, _workBufPos);
                        TotalOut += _workBufPos;

                        // Reset NextOut and AvailOut
                        _workBufPos = 0;
                        _lzmaStream.NextOut = writePtr;
                        _lzmaStream.AvailOut = (uint)_workBuf.Length;
                    }
                    else
                    { // Once everything has been encoded successfully, the return value of lzma_code() will be LZMA_STREAM_END.
                        XZException.CheckReturnValue(ret);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override unsafe void Flush()
        {
            if (_mode == Mode.Decompress)
            {
                BaseStream.Flush();
                return;
            }

            fixed (byte* writePtr = _workBuf)
            {
                _lzmaStream.NextIn = (byte*)0;
                _lzmaStream.AvailIn = 0;
                _lzmaStream.NextOut = writePtr + _workBufPos;
                _lzmaStream.AvailOut = (uint)(_workBuf.Length - _workBufPos);

                LzmaRet ret = LzmaRet.Ok;
                while (ret != LzmaRet.StreamEnd)
                {
                    int writeSize = 0;
                    if (_lzmaStream.AvailOut != 0)
                    {
                        ulong bakAvailOut = _lzmaStream.AvailOut;
                        ret = XZInit.Lib.LzmaCode(_lzmaStream, LzmaAction.FullFlush);
                        writeSize += (int)(bakAvailOut - _lzmaStream.AvailOut);
                    }
                    _workBufPos += writeSize;

                    BaseStream.Write(_workBuf, 0, _workBufPos);
                    TotalOut += _workBufPos;

                    // Reset NextOut and AvailOut
                    _workBufPos = 0;
                    _lzmaStream.NextOut = writePtr;
                    _lzmaStream.AvailOut = (uint)_workBuf.Length;

                    // Once everything has been encoded successfully, the return value of lzma_code() will be LZMA_STREAM_END.
                    if (ret != LzmaRet.Ok && ret != LzmaRet.StreamEnd)
                        throw new XZException(ret);
                }
            }

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
            XZInit.Lib.LzmaGetProgress(_lzmaStream, ref progressIn, ref progressOut);
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
        private static void CheckPreset(uint preset)
        {
            if (!(MinPreset <= preset && preset <= MaxPreset) &&
                !(MinExtremePreset <= preset && preset <= MaxExtremePreset))
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
