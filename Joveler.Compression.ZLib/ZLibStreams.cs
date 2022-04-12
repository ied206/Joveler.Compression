/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

    C# Wrapper based on zlibnet v1.3.3 (https://zlibnet.codeplex.com/)
    Copyright (C) @hardon (https://www.codeplex.com/site/users/view/hardon)
    
    Maintained by Hajin Jang
    Copyright (C) 2017-2020 Hajin Jang

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

using Joveler.DynLoader;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

namespace Joveler.Compression.ZLib
{
    #region StreamOptions
    public class ZLibCompressOptions
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
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = DeflateStream.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the zlib stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
    }

    public class ZLibDecompressOptions
    {
        /// <summary>
        /// The base two logarithm of the window size (the size of the history buffer).  
        /// It should be in the range from 9 to 15. The default value is 15.
        /// WindowBits must be greater than or equal to the value provided when the stream was compressed, or the decompress will fail.
        /// </summary>
        /// <remarks>
        /// For maximum compatibility, using ZLibWindowBits.Default (15) is recommended.
        /// </remarks>
        public ZLibWindowBits WindowBits { get; set; } = ZLibWindowBits.Default;
        public int BufferSize { get; set; } = DeflateStream.DefaultBufferSize;
        public bool LeaveOpen { get; set; } = false;
    }
    #endregion

    #region DeflateStream
    /// <summary>
    /// The stream which compress or decompress deflate stream format.
    /// </summary>
    public class DeflateStream : Stream
    {
        #region enum Mode, Format
        internal enum Mode
        {
            Compress,
            Decompress,
        }

        protected enum Format
        {
            Deflate,
            ZLib,
            GZip,
            BothZLibGzip, // Valid only in Decompress mode
        }
        #endregion

        #region Fields and Properties
        private readonly Mode _mode;
        private readonly bool _leaveOpen;
        private bool _disposed = false;

        private ZStreamL32 _zs32;
        private ZStreamL64 _zs64;
        private GCHandle _zsPin;

        private readonly int _bufferSize = DefaultBufferSize;
        private int _workBufPos = 0;
        private readonly byte[] _workBuf;

        public Stream BaseStream { get; private set; }
        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;

        // Const
        private const int ReadDone = -1;


        // Default Buffer Size
        /* Benchmark - 256K is the fatest.
        AMD Ryzen 5 3600 / .NET Core 3.1.13 / Windows 10.0.19042 x64 / zlib 1.2.11
        | Method | BufferSize |        Mean |     Error |    StdDev |
        |------- |----------- |------------:|----------:|----------:|
        |   ZLib |       4096 |  3,215.4 us |   5.49 us |   4.87 us |
        |   ZLib |      16384 |  3,214.9 us |  15.69 us |  14.68 us |
        |   ZLib |      65536 |  3,219.9 us |   8.46 us |   7.91 us |
        |   ZLib |     262144 |  3,161.8 us |   8.99 us |   7.51 us |
        |   ZLib |    1048576 |  3,376.9 us |  13.43 us |  11.90 us |
        |   ZLib |    4194304 |  3,532.8 us |  10.05 us |   8.91 us |
         */
        internal const int DefaultBufferSize = 256 * 1024;
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing DeflateStream.
        /// </summary>
        public DeflateStream(Stream baseStream, ZLibCompressOptions compOpts)
            : this(baseStream, compOpts, Format.Deflate) { }

        protected DeflateStream(Stream baseStream, ZLibCompressOptions compOpts, Format format)
        {
            ZLibInit.Manager.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Compress;
            _disposed = false;

            // Check and set compress options
            _leaveOpen = compOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(compOpts.BufferSize);
            _workBuf = new byte[_bufferSize];
            int formatWindowBits = CheckFormatWindowBits(compOpts.WindowBits, _mode, format);
            CheckMemLevel(compOpts.MemLevel);

            // Prepare and init ZStream
            switch (ZLibInit.Lib.PlatformLongSize)
            {
                case PlatformLongSize.Long32:
                    {
                        _zs32 = new ZStreamL32();
                        _zsPin = GCHandle.Alloc(_zs32, GCHandleType.Pinned);

                        ZLibRet ret = ZLibInit.Lib.L32.DeflateInit(_zs32, compOpts.Level, formatWindowBits, compOpts.MemLevel);
                        ZLibException.CheckReturnValue(ret, _zs32);
                        break;
                    }
                case PlatformLongSize.Long64:
                    {
                        _zs64 = new ZStreamL64();
                        _zsPin = GCHandle.Alloc(_zs64, GCHandleType.Pinned);

                        ZLibRet ret = ZLibInit.Lib.L64.DeflateInit(_zs64, compOpts.Level, formatWindowBits, compOpts.MemLevel);
                        ZLibException.CheckReturnValue(ret, _zs64);
                        break;
                    }
                default:
                    throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Create decompressing DeflateStream.
        /// </summary>
        public DeflateStream(Stream baseStream, ZLibDecompressOptions decompOpts)
            : this(baseStream, decompOpts, Format.Deflate) { }

        protected DeflateStream(Stream baseStream, ZLibDecompressOptions decompOpts, Format format)
        {
            ZLibInit.Manager.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Decompress;
            _disposed = false;

            // Check and set decompress options
            _leaveOpen = decompOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(decompOpts.BufferSize);
            _workBuf = new byte[_bufferSize];
            int windowBits = CheckFormatWindowBits(decompOpts.WindowBits, _mode, format);

            // Prepare and init ZStream
            switch (ZLibInit.Lib.PlatformLongSize)
            {
                case PlatformLongSize.Long32:
                    {
                        _zs32 = new ZStreamL32();
                        _zsPin = GCHandle.Alloc(_zs32, GCHandleType.Pinned);

                        ZLibRet ret = ZLibInit.Lib.L32.InflateInit(_zs32, windowBits);
                        ZLibException.CheckReturnValue(ret, _zs32);
                        break;
                    }
                case PlatformLongSize.Long64:
                    {
                        _zs64 = new ZStreamL64();
                        _zsPin = GCHandle.Alloc(_zs64, GCHandleType.Pinned);

                        ZLibRet ret = ZLibInit.Lib.L64.InflateInit(_zs64, windowBits);
                        ZLibException.CheckReturnValue(ret, _zs64);
                        break;
                    }
                default:
                    throw new PlatformNotSupportedException();
            }
        }
        #endregion

        #region Disposable Pattern
        ~DeflateStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (BaseStream != null)
                {
                    if (_mode == Mode.Compress)
                        Flush();
                    if (!_leaveOpen)
                        BaseStream.Dispose();
                    BaseStream = null;
                }

                switch (ZLibInit.Lib.PlatformLongSize)
                {
                    case PlatformLongSize.Long32:
                        {
                            if (_zs32 != null)
                            {
                                if (_mode == Mode.Compress)
                                    ZLibInit.Lib.L32.DeflateEnd(_zs32);
                                else
                                    ZLibInit.Lib.L32.InflateEnd(_zs32);
                                _zsPin.Free();
                                _zs32 = null;
                            }
                            break;
                        }
                    case PlatformLongSize.Long64:
                        {
                            if (_zs64 != null)
                            {
                                if (_mode == Mode.Compress)
                                    ZLibInit.Lib.L64.DeflateEnd(_zs64);
                                else
                                    ZLibInit.Lib.L64.InflateEnd(_zs64);
                                _zsPin.Free();
                                _zs64 = null;
                            }
                            break;
                        }
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
#if NETCOREAPP3_1
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
            fixed (byte* readPtr = _workBuf) // [In] Compressed
            fixed (byte* writePtr = span) // [Out] Will-be-decompressed
            {
                switch (ZLibInit.Lib.PlatformLongSize)
                {
                    case PlatformLongSize.Long32:
                        {
                            _zs32.NextIn = readPtr + _workBufPos;
                            _zs32.NextOut = writePtr;
                            _zs32.AvailOut = (uint)span.Length;

                            while (0 < _zs32.AvailOut)
                            {
                                if (_zs32.AvailIn == 0)
                                { // Compressed Data is no longer available in array, so read more from _stream
                                    int baseReadSize = BaseStream.Read(_workBuf, 0, _workBuf.Length);

                                    _workBufPos = 0;
                                    _zs32.NextIn = readPtr;
                                    _zs32.AvailIn = (uint)baseReadSize;
                                    TotalIn += baseReadSize;
                                }

                                uint inCount = _zs32.AvailIn;
                                uint outCount = _zs32.AvailOut;

                                // flush method for inflate has no effect
                                ZLibRet ret = ZLibInit.Lib.L32.Inflate(_zs32, ZLibFlush.NoFlush);

                                _workBufPos += (int)(inCount - _zs32.AvailIn);
                                readSize += (int)(outCount - _zs32.AvailOut);

                                if (ret == ZLibRet.StreamEnd)
                                {
                                    _workBufPos = ReadDone; // magic for StreamEnd
                                    break;
                                }

                                ZLibException.CheckReturnValue(ret, _zs32);
                            }
                        }
                        break;
                    case PlatformLongSize.Long64:
                        {
                            _zs64.NextIn = readPtr + _workBufPos;
                            _zs64.NextOut = writePtr;
                            _zs64.AvailOut = (uint)span.Length;

                            while (0 < _zs64.AvailOut)
                            {
                                if (_zs64.AvailIn == 0)
                                { // Compressed Data is no longer available in array, so read more from _stream
                                    int baseReadSize = BaseStream.Read(_workBuf, 0, _workBuf.Length);

                                    _workBufPos = 0;
                                    _zs64.NextIn = readPtr;
                                    _zs64.AvailIn = (uint)baseReadSize;
                                    TotalIn += baseReadSize;
                                }

                                uint inCount = _zs64.AvailIn;
                                uint outCount = _zs64.AvailOut;

                                // flush method for inflate has no effect
                                ZLibRet ret = ZLibInit.Lib.L64.Inflate(_zs64, ZLibFlush.NoFlush);

                                _workBufPos += (int)(inCount - _zs64.AvailIn);
                                readSize += (int)(outCount - _zs64.AvailOut);

                                if (ret == ZLibRet.StreamEnd)
                                {
                                    _workBufPos = ReadDone; // magic for StreamEnd
                                    break;
                                }

                                ZLibException.CheckReturnValue(ret, _zs64);
                            }
                        }
                        break;
                }
            }

            TotalOut += readSize;
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
#if NETCOREAPP3_1
        public override unsafe void Write(ReadOnlySpan<byte> span)
#else
        public unsafe void Write(ReadOnlySpan<byte> span)
#endif
        {
            if (_mode != Mode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");

            TotalIn += span.Length;

            fixed (byte* readPtr = span) // [In] Compressed
            fixed (byte* writePtr = _workBuf) // [Out] Will-be-decompressed
            {
                switch (ZLibInit.Lib.PlatformLongSize)
                {
                    case PlatformLongSize.Long32:
                        {
                            _zs32.NextIn = readPtr;
                            _zs32.AvailIn = (uint)span.Length;
                            _zs32.NextOut = writePtr + _workBufPos;
                            _zs32.AvailOut = (uint)(_workBuf.Length - _workBufPos);

                            while (_zs32.AvailIn != 0)
                            {
                                uint outCount = _zs32.AvailOut;
                                ZLibRet ret = ZLibInit.Lib.L32.Deflate(_zs32, ZLibFlush.NoFlush);
                                _workBufPos += (int)(outCount - _zs32.AvailOut);

                                if (_zs32.AvailOut == 0)
                                {
                                    BaseStream.Write(_workBuf, 0, _workBuf.Length);
                                    TotalOut += _workBuf.Length;

                                    _workBufPos = 0;
                                    _zs32.NextOut = writePtr;
                                    _zs32.AvailOut = (uint)_workBuf.Length;
                                }

                                ZLibException.CheckReturnValue(ret, _zs32);
                            }
                            break;
                        }
                    case PlatformLongSize.Long64:
                        {
                            _zs64.NextIn = readPtr;
                            _zs64.AvailIn = (uint)span.Length;
                            _zs64.NextOut = writePtr + _workBufPos;
                            _zs64.AvailOut = (uint)(_workBuf.Length - _workBufPos);

                            while (_zs64.AvailIn != 0)
                            {
                                uint outCount = _zs64.AvailOut;
                                ZLibRet ret = ZLibInit.Lib.L64.Deflate(_zs64, ZLibFlush.NoFlush);
                                _workBufPos += (int)(outCount - _zs64.AvailOut);

                                if (_zs64.AvailOut == 0)
                                {
                                    BaseStream.Write(_workBuf, 0, _workBuf.Length);
                                    TotalOut += _workBuf.Length;

                                    _workBufPos = 0;
                                    _zs64.NextOut = writePtr;
                                    _zs64.AvailOut = (uint)_workBuf.Length;
                                }

                                ZLibException.CheckReturnValue(ret, _zs64);
                            }
                            break;
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
                switch (ZLibInit.Lib.PlatformLongSize)
                {
                    case PlatformLongSize.Long32:
                        {
                            _zs32.NextIn = (byte*)0;
                            _zs32.AvailIn = 0;
                            _zs32.NextOut = writePtr + _workBufPos;
                            _zs32.AvailOut = (uint)(_workBuf.Length - _workBufPos);

                            ZLibRet ret = ZLibRet.Ok;
                            while (ret != ZLibRet.StreamEnd)
                            {
                                if (_zs32.AvailOut != 0)
                                {
                                    uint outCount = _zs32.AvailOut;
                                    ret = ZLibInit.Lib.L32.Deflate(_zs32, ZLibFlush.Finish);

                                    _workBufPos += (int)(outCount - _zs32.AvailOut);

                                    if (ret != ZLibRet.StreamEnd && ret != ZLibRet.Ok)
                                        throw new ZLibException(ret, _zs32.LastErrorMsg);
                                }

                                BaseStream.Write(_workBuf, 0, _workBufPos);
                                TotalOut += _workBufPos;

                                _workBufPos = 0;
                                _zs32.NextOut = writePtr;
                                _zs32.AvailOut = (uint)_workBuf.Length;
                            }

                            break;
                        }
                    case PlatformLongSize.Long64:
                        {
                            _zs64.NextIn = (byte*)0;
                            _zs64.AvailIn = 0;
                            _zs64.NextOut = writePtr + _workBufPos;
                            _zs64.AvailOut = (uint)(_workBuf.Length - _workBufPos);

                            ZLibRet ret = ZLibRet.Ok;
                            while (ret != ZLibRet.StreamEnd)
                            {
                                if (_zs64.AvailOut != 0)
                                {
                                    uint outCount = _zs64.AvailOut;
                                    ret = ZLibInit.Lib.L64.Deflate(_zs64, ZLibFlush.Finish);

                                    _workBufPos += (int)(outCount - _zs64.AvailOut);

                                    if (ret != ZLibRet.StreamEnd && ret != ZLibRet.Ok)
                                        throw new ZLibException(ret, _zs64.LastErrorMsg);
                                }

                                BaseStream.Write(_workBuf, 0, _workBufPos);
                                TotalOut += _workBufPos;

                                _workBufPos = 0;
                                _zs64.NextOut = writePtr;
                                _zs64.AvailOut = (uint)_workBuf.Length;
                            }

                            break;
                        }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CheckFormatWindowBits(ZLibWindowBits windowBits, Mode mode, Format format)
        {
            if (!Enum.IsDefined(typeof(ZLibWindowBits), windowBits))
                throw new ArgumentOutOfRangeException(nameof(windowBits));

            int bits = (int)windowBits;
            switch (format)
            {
                case Format.Deflate:
                    return bits * -1;
                case Format.GZip:
                    return bits += 16;
                case Format.ZLib:
                    return bits;
                case Format.BothZLibGzip:
                    if (mode == Mode.Decompress)
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
    #endregion

    #region ZLibStream
    /// <inheritdoc />
    /// /// <summary>
    /// The stream which compress or decompress zlib stream format.
    /// </summary>
    public class ZLibStream : DeflateStream
    {
        /// <summary>
        /// Create compressing ZLibStream.
        /// </summary>
        public ZLibStream(Stream baseStream, ZLibCompressOptions compOpts)
            : base(baseStream, compOpts, Format.ZLib) { }

        /// <summary>
        /// Create decompressing ZLibStream.
        /// </summary>
        public ZLibStream(Stream baseStream, ZLibDecompressOptions decompOpts)
            : base(baseStream, decompOpts, Format.ZLib) { }
    }
    #endregion

    #region GZipStream
    /// <inheritdoc />
    /// /// <summary>
    /// The stream which compress or decompress gzip stream format.
    /// </summary>
    public class GZipStream : DeflateStream
    {
        /// <summary>
        /// Create compressing GZipStream.
        /// </summary>
        public GZipStream(Stream baseStream, ZLibCompressOptions compOpts)
            : base(baseStream, compOpts, Format.GZip) { }

        /// <summary>
        /// Create decompressing GZipStream.
        /// </summary>
        public GZipStream(Stream baseStream, ZLibDecompressOptions decompOpts)
            : base(baseStream, decompOpts, Format.GZip) { }
    }
    #endregion
}
