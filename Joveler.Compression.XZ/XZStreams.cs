/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2023 Hajin Jang

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
using System.Xml.Linq;

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
                Threads = XZHardware.CheckThreadCount(threadOpts.Threads),
                TimeOut = threadOpts.TimeOut,
                Preset = Preset,
                Check = Check,
            };
        }
    }

    /// <summary>
    /// Options to control threaded XZ compresion.
    /// </summary>
    /// <remarks>
    /// IT IS HIGHLY RECOMMENDED TO SET memlimitThreading AND memlimitStop YOURSELF.
    /// </remarks>
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
        /// <summary>
        /// Timeout (millisecond) to allow lzma_code() to return early.
        /// </summary>
        /// <remarks>
        /// <para>Multithreading can make liblzma to consume input and produce
        /// output in a very bursty way: it may first read a lot of input
        /// to fill internal buffers, then no input or output occurs for
        /// a while.</para>
        ///
        /// <para>In single-threaded mode, lzma_code() won't return until it has
        /// either consumed all the input or filled the output buffer. If
        /// this is done in multithreaded mode, it may cause a call
        /// lzma_code() to take even tens of seconds, which isn't acceptable
        /// in all applications.</para>
        ///
        /// <para>To avoid very long blocking times in lzma_code(), a timeout
        /// (in milliseconds) may be set here. If lzma_code() would block
        /// longer than this number of milliseconds, it will return with
        /// LZMA_OK. Reasonable values are 100 ms or more. The xz command
        /// line tool uses 300 ms.</para>
        ///
        /// <para>If long blocking times are fine for you, set timeout to a special
        /// value of 0, which will disable the timeout mechanism and will make
        /// lzma_code() block until all the input is consumed or the output
        /// buffer has been filled.</para>
        ///
        /// <para>NOTE: Even with a timeout, lzma_code() might sometimes take
        ///             somewhat long time to return. No timing guarantees
        ///             are made.</para>
        /// </remarks>
        // Bench: Does not affect performance in a meaningful way.
        public uint TimeOut = 0;
    }

    public class XZDecompressOptions
    {
        /// <summary>
        /// Memory usage hard limit as bytes, that should never be exceeded.
        /// </summary>
        /// <remarks>
        /// Decoder: If decompressing will need more than this amount of
        /// memory even in the single-threaded mode, then lzma_code() will
        /// return LZMA_MEMLIMIT_ERROR.
        /// </remarks>
        public ulong MemLimit { get; set; } = ulong.MaxValue;
        /// <summary>
        /// 
        /// </summary>
        public LzmaDecodingFlag DecodeFlags { get; set; } = XZStream.DefaultDecodingFlags;
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = XZStream.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the xz stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;

        internal LzmaMt ToLzmaMt(XZThreadedDecompressOptions threadOpts)
        {
            // Default soft limit values used within xz-utils are
            // - 64bit systems: TOTAL_RAM / 4.
            // - 32bit systems: + ceiling of 1.4GB (1400U << 20)
            // xz-utils author suggests to use MemAvailable value of the OS, though.
            // Default hard limit values used in liblzma/xz are 
            // - UINT64_MAX

            // Soft limit
            ulong memSoftLimit = threadOpts.MemlimitThreading;

            // Use of XZThreadedDecompressionOptions means the caller explicitly wants to use multi-threaded decompression.
            // liblzma requires memlimit{Threading,Stop} to be set with actual numbers, not 0.
            if (memSoftLimit == 0)
            { // Convert 0 to a default value
                switch (XZInit.Lib.PlatformBitness)
                {
                    case DynLoader.PlatformBitness.Bit32:
                        memSoftLimit = Math.Min(XZHardware.PhysMem() / 4, 1400U << 20);
                        break;
                    case DynLoader.PlatformBitness.Bit64:
                        memSoftLimit = XZHardware.PhysMem() / 4;
                        break;
                }
            }

            // Return LzmaMt
            return new LzmaMt()
            {
                Flags = DecodeFlags,
                Threads = XZHardware.CheckThreadCount(threadOpts.Threads),
                TimeOut = threadOpts.TimeOut,
                MemlimitThreading = memSoftLimit,
                MemlimitStop = MemLimit,
            };
        }
    }

    /// <summary>
    /// Options to control threaded XZ decompresion.
    /// </summary>
    /// <remarks>
    /// IT IS HIGHLY RECOMMENDED TO EXPLICITLY SET memlimitThreading WITH PROPER VALUE.
    /// </remarks>
    public class XZThreadedDecompressOptions
    {
        /// <summary>
        /// Number of worker threads to use.
        /// </summary>
        public int Threads { get; set; } = 1;
        /// <summary>
        /// Timeout to allow lzma_code() to return early
        /// </summary>
        /// <remarks>
        /// <para>
        /// Multithreading can make liblzma to consume input and produce
        /// output in a very bursty way: it may first read a lot of input
        /// to fill internal buffers, then no input or output occurs for
        /// a while.
        /// </para>
        /// <para>
        /// In single-threaded mode, lzma_code() won't return until it has
        /// either consumed all the input or filled the output buffer. If
        /// this is done in multithreaded mode, it may cause a call
        /// lzma_code() to take even tens of seconds, which isn't acceptable
        /// in all applications.
        /// </para>
        /// <para>
        /// To avoid very long blocking times in lzma_code(), a timeout
        /// (in milliseconds) may be set here. If lzma_code() would block
        /// longer than this number of milliseconds, it will return with
        /// LZMA_OK. Reasonable values are 100 ms or more. The xz command
        /// line tool uses 300 ms.
        /// </para>
        /// <para>
        /// If long blocking times are fine for you, set timeout to a special
        /// value of 0, which will disable the timeout mechanism and will make
        /// lzma_code() block until all the input is consumed or the output
        /// buffer has been filled.
        /// </para>
        /// <para>
        /// NOTE: Even with a timeout, lzma_code() might sometimes take somewhat long time to return.
        ///       No timing guarantees are made.
        /// </para>
        /// </remarks>
        // Bench: Does not affect performance in a meaningful way.
        public uint TimeOut = 0;
        /// <summary>
        /// <para>Memory usage soft limit to reduce the number of threads.</para>
        /// <para>Joveler.Compression.XZ specific: Set to 0 to use default value (TotalMem / 4).</para>
        /// </summary>
        /// <remarks>
        /// <para>If the number of threads has been set so high that more than
        /// memlimit_threading bytes of memory would be needed, the number
        /// of threads will be reduced so that the memory usage will not exceed
        /// memlimit_threading bytes. However, if memlimit_threading cannot
        /// be met even in single-threaded mode, then decoding will continue
        /// in single-threaded mode and memlimit_threading may be exceeded
        /// even by a large amount. That is, memlimit_threading will never make
        /// lzma_code() return LZMA_MEMLIMIT_ERROR. To truly cap the memory
        /// usage, see memlimit_stop below.</para>
        /// 
        /// <para>Setting memlimit_threading to UINT64_MAX or a similar huge value
        /// means that liblzma is allowed to keep the whole compressed file
        /// and the whole uncompressed file in memory in addition to the memory
        /// needed by the decompressor data structures used by each thread!
        /// In other words, a reasonable value limit must be set here or it
        /// will cause problems sooner or later. If you have no idea what
        /// a reasonable value could be, try lzma_physmem() / 4 as a starting
        /// point. Setting this limit will never prevent decompression of
        /// a file; this will only reduce the number of threads.</para>
        /// 
        /// <para>If memlimit_threading is greater than memlimit_stop, then the value
        /// of memlimit_stop will be used for both.</para>
        /// </remarks>
        public ulong MemlimitThreading = 0;
    }
    #endregion

    #region XZStreamBase
    /// <inheritdoc />
    /// <summary>
    /// The stream to handle .xz file format.
    /// </summary>
    public abstract class XZStreamBase : Stream
    {
        #region enum Mode
        private enum Mode
        {
            Compress,
            Decompress,
        }
        #endregion

        #region enum CoderFormat
        /// <summary>
        /// Determine which encoder/decoder to use.
        /// <para>Single-threaded decompression only.</para>
        /// </summary>
        protected enum CoderFormat
        {
            Auto = 0,
            XZ = 1,
            LegacyLzma = 2,
            LZip = 3,
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

        #region Constructor (Compression)
        /// <summary>
        /// Create single-threaded compressing XZStream instance.
        /// </summary>
        /// <param name="baseStream">
        /// A stream of XZ container to compress.
        /// </param>
        /// <param name="compOpts">
        /// Options to control general compression.
        /// </param>
        protected unsafe XZStreamBase(Stream baseStream, XZCompressOptions compOpts)
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
            XZException.CheckReturnValueNormal(ret);

            // Set possible max memory usage.
            MaxMemUsage = XZInit.Lib.LzmaEasyEncoderMemUsage(preset);
        }

        /// <summary>
        /// Create multi-threaded compressing XZStream instance.
        /// Requires more memory than single-threaded mode.
        /// </summary>
        /// <param name="baseStream">
        /// A stream of XZ container to compress.
        /// </param>
        /// <param name="compOpts">
        /// Options to control general compression.
        /// </param>
        /// <param name="threadOpts">
        /// Options to control threaded compression.
        /// </param>
        protected unsafe XZStreamBase(Stream baseStream, XZCompressOptions compOpts, XZThreadedCompressOptions threadOpts)
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
            XZException.CheckReturnValueNormal(ret);

            // Set possible max memory usage.
            MaxMemUsage = XZInit.Lib.LzmaStreamEncoderMtMemUsage(mt);
        }
        #endregion

        #region Constructors (Decompression)
        /// <summary>
        /// (Not Public) Create decompressing XZStream instance with <see cref="CoderFormat"/>.
        /// </summary>
        /// <param name="baseStream">
        /// <para>A stream of XZ container to decompress.</para>
        /// </param>
        /// <param name="decompOpts">
        /// Options to control general decompression.
        /// </param>
        /// <param name="fileFormat">
        /// 
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        protected unsafe XZStreamBase(Stream baseStream, XZDecompressOptions decompOpts, CoderFormat fileFormat)
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
            LzmaRet ret = LzmaRet.Ok;
            switch (fileFormat)
            {
                case CoderFormat.XZ:
                    ret = XZInit.Lib.LzmaStreamDecoder(_lzmaStream, decompOpts.MemLimit, decompOpts.DecodeFlags);
                    break;
                case CoderFormat.Auto:
                    ret = XZInit.Lib.LzmaAutoDecoder(_lzmaStream, decompOpts.MemLimit, decompOpts.DecodeFlags);
                    break;
                case CoderFormat.LegacyLzma:
                    ret = XZInit.Lib.LzmaAloneDecoder(_lzmaStream, decompOpts.MemLimit);
                    break;
                case CoderFormat.LZip:
                    ret = XZInit.Lib.LzmaLZipDecoder(_lzmaStream, decompOpts.MemLimit, decompOpts.DecodeFlags);
                    break;
            }
            XZException.CheckReturnValueNormal(ret);
        }

        /// <summary>
        /// Create multi-threaded decompressing XZStream instance.
        /// Requires more memory than single-threaded mode.
        /// </summary>
        /// <param name="baseStream">
        /// <para>A stream of XZ container to decompress.</para>
        /// </param>
        /// <param name="decompOpts">
        /// Options to control general decompression.
        /// </param>
        /// <param name="threadOpts">
        /// Options to control threaded decompression.
        /// <para>It is highly recommended to explicitly set <see cref="XZThreadedDecompressOptions.MemlimitThreading"/> value.</para>
        /// </param>
        protected unsafe XZStreamBase(Stream baseStream, XZDecompressOptions decompOpts, XZThreadedDecompressOptions threadOpts)
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

            // Check LzmaMt instance
            LzmaMt mt = decompOpts.ToLzmaMt(threadOpts);

            // Initialize the decoder
            LzmaRet ret = XZInit.Lib.LzmaStreamDecoderMt(_lzmaStream, mt);
            XZException.CheckReturnValueNormal(ret);
        }
        #endregion

        #region Disposable Pattern
        ~XZStreamBase()
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
                throw new NotSupportedException($"{nameof(Read)}() not supported on compression.");
            CheckReadWriteArgs(buffer, offset, count);
            if (count == 0)
                return 0;

            Span<byte> span = buffer.AsSpan(offset, count);
            return Read(span);
        }

        /// <inheritdoc />
#if NETCOREAPP3_1
        public override unsafe int Read(Span<byte> span)
#else
        public unsafe int Read(Span<byte> span)
#endif
        { // For Decompress
            if (_mode != Mode.Decompress)
                throw new NotSupportedException($"{nameof(Read)}() not supported on compression.");
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


                    if (ret == LzmaRet.StreamEnd)
                    { // Once everything has been decoded successfully, the return value of lzma_code() will be LZMA_STREAM_END.
                        _workBufPos = ReadDone;
                        break;
                    }
                    else if (ret == LzmaRet.SeekNeeded)
                    { // Request to change the input file position -> Some coders can do random access in the input file.
                        // When this value is returned, the application must seek to the file position given in lzma_stream.seek_pos.
                        // This value is guaranteed to never exceed the file size that was specified at the coder initialization.
                        // After seeking the application should read new input and pass it normally via lzma_stream.next_in and .avail_in.

                        // Seek BaseStream. If Seek() fails, it will throw a NotSupportedException
                        // v5.4.0 -> only decoder uses random seek.
                        BaseStream.Seek((long)_lzmaStream.SeekPos, SeekOrigin.Begin);
                        _lzmaStream.AvailIn = 0;
                    }

                    // Normally the return value of lzma_code() will be LZMA_OK until everything has been encoded.
                    XZException.CheckReturnValueDecompress(ret);
                }
            }

            TotalOut += readSize;
            return readSize;
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        { // For Compress
            if (_mode != Mode.Compress)
                throw new NotSupportedException($"{nameof(Write)}() not supported on decompression.");
            CheckReadWriteArgs(buffer, offset, count);
            if (count == 0)
                return;

            ReadOnlySpan<byte> span = buffer.AsSpan(offset, count);
            Write(span);
        }

        /// <inheritdoc />
#if NETCOREAPP3_1
        public override unsafe void Write(ReadOnlySpan<byte> span)
#else
        public unsafe void Write(ReadOnlySpan<byte> span)
#endif
        { // For Compress
            if (_mode != Mode.Compress)
                throw new NotSupportedException($"{nameof(Write)}() not supported on decompression.");

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
                    XZException.CheckReturnValueNormal(ret);
                }
            }
        }

        private unsafe void FinishWrite()
        {
            Debug.Assert(_mode == Mode.Compress, $"{nameof(FinishWrite)}() must not be called in decompression.");

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
                        // When lzma_code() has returned LZMA_STREAM_END, the output buffer is likely to be only partially full.
                        // Calculate how much new data there is to be written to the output file.
                        BaseStream.Write(_workBuf, 0, _workBufPos);
                        TotalOut += _workBufPos;

                        // Reset NextOut and AvailOut
                        _workBufPos = 0;
                        _lzmaStream.NextOut = writePtr;
                        _lzmaStream.AvailOut = (uint)_workBuf.Length;
                    }
                    else
                    { // Once everything has been encoded successfully, the return value of lzma_code() will be LZMA_STREAM_END.
                        XZException.CheckReturnValueNormal(ret);
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
            throw new NotSupportedException($"{nameof(Seek)} not supported.");
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
        /// Get progress information of XZ stream.
        /// </summary>
        /// <remarks>
        /// <para>In single-threaded mode, applications can get progress information from 
        /// strm->total_in and strm->total_out.</para>
        /// 
        /// <para>In multi-threaded mode this is less
        /// useful because a significant amount of both input and output data gets
        /// buffered internally by liblzma. This makes total_in and total_out give
        /// misleading information and also makes the progress indicator updates
        /// non-smooth.
        /// </para>
        /// <para>
        /// This function gives realistic progress information also in multi-threaded
        /// mode by taking into account the progress made by each thread. In
        /// single-threaded mode *progress_in and *progress_out are set to
        /// strm->total_in and strm->total_out, respectively.
        /// </para>
        /// </remarks>
        public void GetProgress(out ulong progressIn, out ulong progressOut)
        {
            progressIn = 0;
            progressOut = 0;
            XZInit.Lib.LzmaGetProgress(_lzmaStream, ref progressIn, ref progressOut);
        }
        #endregion

        #region Memory Usage (Decompression Only) - DISABLED
        // lzma_memusage() only works on per-thread basis.
        // It would not help users to perceive how many memory cap would needed on multi-threaded decompression.
#if LZMA_MEM_ENABLE
        /// <summary>
        /// Get the memory usage of decompression setup.
        /// <para>Must be called after calling Read() at least once.</para>
        /// </summary>
        /// <remarks>
        /// <para>This function is useful e.g. after LZMA_MEMLIMIT_ERROR to find out how big
        /// the memory usage limit should have been to decode the input. Note that
        /// this may give misleading information if decoding .xz Streams that have
        /// multiple Blocks, because each Block can have different memory requirements.</para>
        /// </remarks>
        /// <returns>
        /// How much memory is currently allocated for the filter decoders.
        /// If no filter chain is currently allocated, some non-zero value is still returned,
        /// which is less than or equal to what any filter chain would indicate as its  memory requirement.
        ///
        /// If this function isn't supported by *strm or some other error occurs, zero is returned.
        /// </returns>
        public ulong GetDecompresMemUsage()
        {
            if (_mode != Mode.Decompress)
                throw new NotSupportedException($"{nameof(GetDecompresMemUsage)}() not supported on compression.");
            return XZInit.Lib.LzmaMemusage(_lzmaStream);
        }
#endif
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

    #region XZStream
    public sealed class XZStream : XZStreamBase
    {
        #region Constructor (Compression)
        /// <summary>
        /// Create single-threaded compressing XZStream instance.
        /// </summary>
        /// <param name="baseStream">
        /// A stream of XZ container to compress.
        /// </param>
        /// <param name="compOpts">
        /// Options to control general compression.
        /// </param>
        public XZStream(Stream baseStream, XZCompressOptions compOpts) :
            base(baseStream, compOpts)
        {
        }

        /// <summary>
        /// Create multi-threaded compressing XZStream instance.
        /// Requires more memory than single-threaded mode.
        /// </summary>
        /// <param name="baseStream">
        /// A stream of XZ container to compress.
        /// </param>
        /// <param name="compOpts">
        /// Options to control general compression.
        /// </param>
        /// <param name="threadOpts">
        /// Options to control threaded compression.
        /// </param>
        public unsafe XZStream(Stream baseStream, XZCompressOptions compOpts, XZThreadedCompressOptions threadOpts) :
            base(baseStream, compOpts, threadOpts)
        {
        }
        #endregion

        #region Constructors (Decompression)
        /// <summary>
        /// Create decompressing XZStream instance.
        /// </summary>
        /// <param name="baseStream">
        /// <para>A stream of XZ container to decompress.</para>
        /// </param>
        /// <param name="decompOpts">
        /// Options to control general decompression.
        /// </param>
        public XZStream(Stream baseStream, XZDecompressOptions decompOpts)
            : base(baseStream, decompOpts, CoderFormat.XZ)
        {
        }

        /// <summary>
        /// Create multi-threaded decompressing XZStream instance.
        /// Requires more memory than single-threaded mode.
        /// </summary>
        /// <param name="baseStream">
        /// <para>A stream of XZ container to decompress.</para>
        /// </param>
        /// <param name="decompOpts">
        /// Options to control general decompression.
        /// </param>
        /// <param name="threadOpts">
        /// Options to control threaded decompression.
        /// <para>It is highly recommended to explicitly set <see cref="XZThreadedDecompressOptions.MemlimitThreading"/> value.</para>
        /// </param>
        public unsafe XZStream(Stream baseStream, XZDecompressOptions decompOpts, XZThreadedDecompressOptions threadOpts) :
            base(baseStream, decompOpts, threadOpts)
        {
        }
        #endregion
    }
    #endregion

    #region LzmaAutoStream (Decompress Only)
    /// <inheritdoc />
    /// <summary>
    /// The stream to handle .xz, .lzma, and .lz (lzip) files with autodetection. (Decompression Only)
    /// </summary>
    /// <remarks>
    /// Does not support multi-threaded xz decompression.
    /// </remarks>
    public sealed class LzmaAutoStream : XZStreamBase
    {
        /// <summary>
        /// Create decompressing LzmaAutoStream instance.
        /// <para>Auto detects .xz, .lzma and .lz file format.</para>
        /// </summary>
        /// <remarks>
        /// Does not support multi-threaded xz decompression.
        /// </remarks>
        /// <param name="baseStream">
        /// <para>A stream of xz/lzma/lz container to decompress.</para>
        /// </param>
        /// <param name="decompOpts">
        /// Options to control general decompression.
        /// </param>
        public LzmaAutoStream(Stream baseStream, XZDecompressOptions decompOpts)
            : base(baseStream, decompOpts, CoderFormat.Auto)
        {
        }
    }
    #endregion

    #region LzmaAloneStream (Decompress Only)
    /// <summary>
    /// The stream to handle legacy .lzma file format. (Decompression Only)
    /// </summary>
    // TODO: liblzma supports .lzma compression. Since the .lzma format is the legacy one and is almost dead,
    //       Do we really need it on Joveler.Compression.XZ?
    //       To support it, lzma_options_lzma also needs to be p/invoked.
    public sealed class LzmaAloneStream : XZStreamBase
    {
        /// <inheritdoc />
        /// <summary>
        /// Create decompressing LzmaAloneStream instance.
        /// </summary>
        /// <param name="baseStream">
        /// <para>A stream of .lz (lzip) container to decompress.</para>
        /// </param>
        /// <param name="decompOpts">
        /// Options to control general decompression.
        /// </param>
        public LzmaAloneStream(Stream baseStream, XZDecompressOptions decompOpts)
            : base(baseStream, decompOpts, CoderFormat.LegacyLzma)
        {
        }
    }
    #endregion

    #region LZipStream (Decompress Only)
    /// <summary>
    /// The stream to handle .lz (lzip) file format. (Decompression Only)
    /// </summary>
    public sealed class LZipStream : XZStreamBase
    {
        /// <inheritdoc />
        /// <summary>
        /// Create decompressing LZipStream instance.
        /// </summary>
        /// <param name="baseStream">
        /// <para>A stream of .lz (lzip) container to decompress.</para>
        /// </param>
        /// <param name="decompOpts">
        /// Options to control general decompression.
        /// </param>
        public LZipStream(Stream baseStream, XZDecompressOptions decompOpts)
            : base(baseStream, decompOpts, CoderFormat.LZip)
        {
        }
    }
    #endregion
}
