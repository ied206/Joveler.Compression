/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

    C# Wrapper based on zlibnet v1.3.3 (https://zlibnet.codeplex.com/)
    Copyright (C) @hardon (https://www.codeplex.com/site/users/view/hardon)
    
    Maintained by Hajin Jang
    Copyright (C) 2017-present Hajin Jang

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
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Joveler.Compression.ZLib
{
    #region StreamOptions
    public sealed class ZLibCompressOptions
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
        public int BufferSize { get; set; } = DeflateStreamBase.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the zlib stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
        /// <summary>
        /// Buffer pool to use for internal buffer.
        /// </summary>
        public ArrayPool<byte>? BufferPool { get; set; } = ArrayPool<byte>.Shared;
    }

    public sealed class ZLibDecompressOptions
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
        /// <summary>
        /// Size of the internal buffer.
        /// </summary>
        public int BufferSize { get; set; } = DeflateStreamBase.DefaultBufferSize;
        /// <summary>
        /// Whether to leave the base stream object open after disposing the zlib stream object.
        /// </summary>
        public bool LeaveOpen { get; set; } = false;
        /// <summary>
        /// Buffer pool to use for internal buffer.
        /// </summary>
        public ArrayPool<byte>? BufferPool { get; set; } = ArrayPool<byte>.Shared;
    }
    #endregion

    #region DeflateSerialStream
    /// <summary>
    /// The stream which compresses or decompresses zlib-related stream format in single-thread.
    /// </summary>
    internal sealed class DeflateSerialStream : Stream
    {
        #region Fields and Properties
        private readonly ZLibStreamOperateMode _mode;
        private readonly bool _leaveOpen;
        private bool _disposed = false;

        // Singlethread Compress/Decompress
        private ZStreamBase? _zs;
        private GCHandle _zsPin;

        private readonly int _bufferSize;
        private readonly PooledBuffer _workBuffer;

        public Stream? BaseStream { get; private set; }
        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;

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
        public DeflateSerialStream(Stream baseStream, ZLibCompressOptions compOpts, ZLibOperateFormat format)
        {
            ZLibInit.Manager.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));

            _disposed = false;
            _bufferSize = CheckBufferSize(compOpts.BufferSize);
            _leaveOpen = compOpts.LeaveOpen;

            _mode = ZLibStreamOperateMode.Compress;
            ArrayPool<byte> pool = compOpts.BufferPool ?? ArrayPool<byte>.Shared;
            _workBuffer = new PooledBuffer(pool, _bufferSize);

            // Check and set compress options
            int windowBits = ZLibLoader.ProcessFormatWindowBits(compOpts.WindowBits, _mode, format);
            ZLibLoader.CheckMemLevel(compOpts.MemLevel);

            _zs = ZLibInit.Lib.CreateZStream();
            _zsPin = GCHandle.Alloc(_zs, GCHandleType.Pinned);

            ZLibRet ret = ZLibInit.Lib.NativeAbi.DeflateInit(_zs, compOpts.Level, windowBits, compOpts.MemLevel);
            ZLibException.CheckReturnValue(ret, _zs);
        }

        public DeflateSerialStream(Stream baseStream, ZLibDecompressOptions decompOpts, ZLibOperateFormat format)
        {
            ZLibInit.Manager.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = ZLibStreamOperateMode.Decompress;
            _disposed = false;

            // Check and set decompress options
            _leaveOpen = decompOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(decompOpts.BufferSize);
            ArrayPool<byte> pool = decompOpts.BufferPool ?? ArrayPool<byte>.Shared;
            _workBuffer = new PooledBuffer(pool, _bufferSize);

            // Prepare and init ZStream
            _zs = ZLibInit.Lib.CreateZStream();
            _zsPin = GCHandle.Alloc(_zs, GCHandleType.Pinned);

            int windowBits = ZLibLoader.ProcessFormatWindowBits(decompOpts.WindowBits, _mode, format);
            ZLibRet ret = ZLibInit.Lib.NativeAbi.InflateInit(_zs, windowBits);
            ZLibException.CheckReturnValue(ret, _zs);
        }
        #endregion

        #region Disposable Pattern
        ~DeflateSerialStream()
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
                if (BaseStream != null)
                {
                    if (_mode == ZLibStreamOperateMode.Compress)
                        FinishWrite();
                    if (!_leaveOpen)
                        BaseStream.Dispose();
                    BaseStream = null;
                }

                if (_zs != null)
                {
                    ZLibInit.Lib.NativeAbi.DeflateEnd(_zs);
                    _zsPin.Free();
                    _zs = null;
                }

                if (!_workBuffer.Disposed)
                    _workBuffer.Dispose();

                _disposed = true;
            }

            // Dispose the base class
            base.Dispose(disposing);
        }
        #endregion

        #region Stream Methods and Properties
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        { // For Decompress
            if (_mode != ZLibStreamOperateMode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");
            ZLibLoader.CheckReadWriteArgs(buffer, offset, count);
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
            if (_mode != ZLibStreamOperateMode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");
            if (BaseStream == null || _zs == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            // Discard the additional data if decompress is already done
            if (_workBuffer.Disposed)
                return 0;

            int readSize = 0;
            fixed (byte* readPtr = _workBuffer.Buf) // [In] Compressed
            fixed (byte* writePtr = span) // [Out] RAW
            {
                _zs!.NextIn = readPtr + _workBuffer.DataEndIdx;
                _zs.NextOut = writePtr;
                _zs.AvailOut = (uint)span.Length;

                while (0 < _zs.AvailOut)
                {
                    if (_zs.AvailIn == 0)
                    { // Compressed Data is no longer available in array, so read more from _stream
                        int baseReadSize = BaseStream.Read(_workBuffer.Buf, 0, _workBuffer.Capacity);

                        _workBuffer.DataEndIdx = 0;
                        _zs.NextIn = readPtr;
                        _zs.AvailIn = (uint)baseReadSize;
                        TotalIn += baseReadSize;
                    }

                    uint inCount = _zs.AvailIn;
                    uint outCount = _zs.AvailOut;

                    // flush method for inflate has no effect
                    ZLibRet ret = ZLibInit.Lib.NativeAbi.Inflate(_zs, ZLibFlush.NoFlush);

                    _workBuffer.DataEndIdx += (int)(inCount - _zs.AvailIn);
                    readSize += (int)(outCount - _zs.AvailOut);

                    if (ret == ZLibRet.StreamEnd)
                    {
                        _workBuffer.Dispose(); // magic for StreamEnd
                        break;
                    }

                    ZLibException.CheckReturnValue(ret, _zs);
                }
            }

            TotalOut += readSize;
            return readSize;
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_mode != ZLibStreamOperateMode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");
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
            if (_mode != ZLibStreamOperateMode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");
            if (BaseStream == null || _zs == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            TotalIn += span.Length;

            fixed (byte* readPtr = span) // [In] RAW
            fixed (byte* writePtr = _workBuffer.Buf) // [Out] Compressed
            {
                _zs.NextIn = readPtr;
                _zs.AvailIn = (uint)span.Length;
                _zs.NextOut = writePtr + _workBuffer.DataEndIdx;
                _zs.AvailOut = (uint)(_workBuffer.Capacity - _workBuffer.DataEndIdx);

                while (0 < _zs.AvailIn)
                {
                    uint outCount = _zs.AvailOut;
                    ZLibRet ret = ZLibInit.Lib.NativeAbi.Deflate(_zs, ZLibFlush.NoFlush);
                    _workBuffer.DataEndIdx += (int)(outCount - _zs.AvailOut);

                    if (_zs.AvailOut == 0)
                    {
                        BaseStream.Write(_workBuffer.Buf, 0, _workBuffer.Capacity);
                        TotalOut += _workBuffer.Capacity;

                        _workBuffer.Clear();
                        _zs.NextOut = writePtr;
                        _zs.AvailOut = (uint)_workBuffer.Capacity;
                    }

                    ZLibException.CheckReturnValue(ret, _zs);
                }
            }
        }

        private unsafe void FinishWrite()
        {
            if (BaseStream == null || _zs == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            fixed (byte* writePtr = _workBuffer.Buf)
            {
                _zs.NextIn = (byte*)0;
                _zs.AvailIn = 0;
                _zs.NextOut = writePtr + _workBuffer.DataEndIdx;
                _zs.AvailOut = (uint)(_workBuffer.Capacity - _workBuffer.DataEndIdx);

                ZLibRet ret = ZLibRet.Ok;
                while (ret != ZLibRet.StreamEnd)
                {
                    if (_zs.AvailOut != 0)
                    {
                        uint outCount = _zs.AvailOut;
                        ret = ZLibInit.Lib.NativeAbi.Deflate(_zs, ZLibFlush.Finish);
                        _workBuffer.DataEndIdx += (int)(outCount - _zs.AvailOut);

                        if (ret != ZLibRet.StreamEnd && ret != ZLibRet.Ok)
                            throw new ZLibException(ret, _zs.LastErrorMsg);
                    }

                    BaseStream.Write(_workBuffer.Buf, 0, _workBuffer.DataEndIdx);
                    TotalOut += _workBuffer.DataEndIdx;

                    _workBuffer.Clear();
                    _zs.NextOut = writePtr;
                    _zs.AvailOut = (uint)_workBuffer.Capacity;
                }
            }
        }

        /// <inheritdoc />
        public override unsafe void Flush()
        {
            if (BaseStream == null || _zs == null)
                throw new ObjectDisposedException("This stream had been disposed.");

            if (_mode == ZLibStreamOperateMode.Decompress)
            {
                BaseStream.Flush();
                return;
            }

            fixed (byte* writePtr = _workBuffer.Buf)
            {
                _zs.NextIn = (byte*)0;
                _zs.AvailIn = 0;

                do
                {
                    _zs.NextOut = writePtr + _workBuffer.DataEndIdx;
                    _zs.AvailOut = (uint)(_workBuffer.Capacity - _workBuffer.DataEndIdx);

                    uint outCount = _zs.AvailOut;
                    ZLibRet ret = ZLibInit.Lib.NativeAbi.Deflate(_zs, ZLibFlush.PartialFlush);
                    int bytesWritten = (int)(outCount - _zs.AvailOut);
                    _workBuffer.DataEndIdx += bytesWritten;

                    ZLibException.CheckReturnValue(ret, _zs);

                    // Write the buffer ASAP since this function is 'Flush()'.
                    BaseStream.Write(_workBuffer.Buf, 0, _workBuffer.DataEndIdx);
                    TotalOut += _workBuffer.DataEndIdx;

                    _workBuffer.Clear();
                }
                while (_zs.AvailOut == 0);
            }

            BaseStream.Flush();
        }

        /// <inheritdoc />
        public override bool CanRead => _mode == ZLibStreamOperateMode.Decompress && BaseStream != null && BaseStream.CanRead;
        /// <inheritdoc />
        public override bool CanWrite => _mode == ZLibStreamOperateMode.Compress && BaseStream != null && BaseStream.CanWrite;
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
                if (_mode == ZLibStreamOperateMode.Compress)
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
