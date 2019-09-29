/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

    C# Wrapper based on zlibnet v1.3.3 (https://zlibnet.codeplex.com/)
    Copyright (C) @hardon (https://www.codeplex.com/site/users/view/hardon)
    
    Maintained by Hajin Jang
    Copyright (C) 2017-2019 Hajin Jang

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
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

namespace Joveler.Compression.ZLib
{
    #region StreamOptions
    public class ZLibCompressOptions
    {
        public ZLibCompLevel Level { get; set; } = ZLibCompLevel.Default;
        public int BufferSize { get; set; } = DeflateStream.DefaultBufferSize;
        public bool LeaveOpen { get; set; } = false;
    }

    public class ZLibDecompressOptions
    {
        public int BufferSize { get; set; } = DeflateStream.DefaultBufferSize;
        public bool LeaveOpen { get; set; } = false;
    }
    #endregion

    #region DeflateStream
    public class DeflateStream : Stream
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

        private ZStreamL32 _zs32;
        private ZStreamL64 _zs64;
        private GCHandle _zsPin;
        private readonly int _bufferSize = DefaultBufferSize;

        private int _internalBufPos = 0;
        private readonly byte[] _internalBuf;

        public Stream BaseStream { get; private set; }
        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;

        // Const
        private const int ReadDone = -1;
        internal const int DefaultBufferSize = 64 * 1024;
        #endregion

        #region Constructor
        /// <summary>
        /// Create compressing DeflateStream.
        /// </summary>
        public DeflateStream(Stream baseStream, ZLibCompressOptions compOpts)
            : this(baseStream, compOpts, ZLibWriteType.Deflate) { }

        protected DeflateStream(Stream baseStream, ZLibCompressOptions compOpts, ZLibWriteType writeType)
        {
            NativeMethods.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Compress;
            _disposed = false;

            // Check and set compress options
            _leaveOpen = compOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(compOpts.BufferSize);
            _internalBuf = new byte[_bufferSize];

            // Prepare and init ZStream
            switch (NativeMethods.LongBitType)
            {
                case NativeMethods.LongBits.Long32:
                    {
                        _zs32 = new ZStreamL32();
                        _zsPin = GCHandle.Alloc(_zs32, GCHandleType.Pinned);

                        ZLibReturn ret = NativeMethods.L32.DeflateInit(_zs32, compOpts.Level, writeType);
                        ZLibException.CheckReturnValue(ret, _zs32);
                        break;
                    }
                case NativeMethods.LongBits.Long64:
                    {
                        _zs64 = new ZStreamL64();
                        _zsPin = GCHandle.Alloc(_zs64, GCHandleType.Pinned);

                        ZLibReturn ret = NativeMethods.L64.DeflateInit(_zs64, compOpts.Level, writeType);
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
            : this(baseStream, decompOpts, ZLibOpenType.Deflate) { }

        protected DeflateStream(Stream baseStream, ZLibDecompressOptions decompOpts, ZLibOpenType openType)
        {
            NativeMethods.EnsureLoaded();

            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _mode = Mode.Decompress;
            _disposed = false;

            // Check and set decompress options
            _leaveOpen = decompOpts.LeaveOpen;
            _bufferSize = CheckBufferSize(decompOpts.BufferSize);
            _internalBuf = new byte[_bufferSize];

            // Prepare and init ZStream
            switch (NativeMethods.LongBitType)
            {
                case NativeMethods.LongBits.Long32:
                    {
                        _zs32 = new ZStreamL32();
                        _zsPin = GCHandle.Alloc(_zs32, GCHandleType.Pinned);

                        ZLibReturn ret = NativeMethods.L32.InflateInit(_zs32, openType);
                        ZLibException.CheckReturnValue(ret, _zs32);
                        break;
                    }
                case NativeMethods.LongBits.Long64:
                    {
                        _zs64 = new ZStreamL64();
                        _zsPin = GCHandle.Alloc(_zs64, GCHandleType.Pinned);

                        ZLibReturn ret = NativeMethods.L64.InflateInit(_zs64, openType);
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

                switch (NativeMethods.LongBitType)
                {
                    case NativeMethods.LongBits.Long32:
                        {
                            if (_zs32 != null)
                            {
                                if (_mode == Mode.Compress)
                                    NativeMethods.L32.DeflateEnd(_zs32);
                                else
                                    NativeMethods.L32.InflateEnd(_zs32);
                                _zsPin.Free();
                                _zs32 = null;
                            }
                            break;
                        }
                    case NativeMethods.LongBits.Long64:
                        {
                            if (_zs64 != null)
                            {
                                if (_mode == Mode.Compress)
                                    NativeMethods.L64.DeflateEnd(_zs64);
                                else
                                    NativeMethods.L64.InflateEnd(_zs64);
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
        public unsafe int Read(Span<byte> span)
        { // For Decompress
            if (_mode != Mode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");

            if (_internalBufPos == ReadDone)
                return 0;

            int readSize = 0;
            fixed (byte* readPtr = _internalBuf) // [In] Compressed
            fixed (byte* writePtr = span) // [Out] Will-be-decompressed
            {
                switch (NativeMethods.LongBitType)
                {
                    case NativeMethods.LongBits.Long32:
                        {
                            _zs32.NextIn = readPtr + _internalBufPos;
                            _zs32.NextOut = writePtr;
                            _zs32.AvailOut = (uint)span.Length;

                            while (0 < _zs32.AvailOut)
                            {
                                if (_zs32.AvailIn == 0)
                                { // Compressed Data is no longer available in array, so read more from _stream
                                    int baseReadSize = BaseStream.Read(_internalBuf, 0, _internalBuf.Length);

                                    _internalBufPos = 0;
                                    _zs32.NextIn = readPtr;
                                    _zs32.AvailIn = (uint)baseReadSize;
                                    TotalIn += baseReadSize;
                                }

                                uint inCount = _zs32.AvailIn;
                                uint outCount = _zs32.AvailOut;

                                // flush method for inflate has no effect
                                ZLibReturn ret = NativeMethods.L32.Inflate(_zs32, ZLibFlush.NoFlush);

                                _internalBufPos += (int)(inCount - _zs32.AvailIn);
                                readSize += (int)(outCount - _zs32.AvailOut);

                                if (ret == ZLibReturn.StreamEnd)
                                {
                                    _internalBufPos = ReadDone; // magic for StreamEnd
                                    break;
                                }

                                ZLibException.CheckReturnValue(ret, _zs32);
                            }
                        }
                        break;
                    case NativeMethods.LongBits.Long64:
                        {
                            _zs64.NextIn = readPtr + _internalBufPos;
                            _zs64.NextOut = writePtr;
                            _zs64.AvailOut = (uint)span.Length;

                            while (0 < _zs64.AvailOut)
                            {
                                if (_zs64.AvailIn == 0)
                                { // Compressed Data is no longer available in array, so read more from _stream
                                    int baseReadSize = BaseStream.Read(_internalBuf, 0, _internalBuf.Length);

                                    _internalBufPos = 0;
                                    _zs64.NextIn = readPtr;
                                    _zs64.AvailIn = (uint)baseReadSize;
                                    TotalIn += baseReadSize;
                                }

                                uint inCount = _zs64.AvailIn;
                                uint outCount = _zs64.AvailOut;

                                // flush method for inflate has no effect
                                ZLibReturn ret = NativeMethods.L64.Inflate(_zs64, ZLibFlush.NoFlush);

                                _internalBufPos += (int)(inCount - _zs64.AvailIn);
                                readSize += (int)(outCount - _zs64.AvailOut);

                                if (ret == ZLibReturn.StreamEnd)
                                {
                                    _internalBufPos = ReadDone; // magic for StreamEnd
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

        public unsafe void Write(ReadOnlySpan<byte> span)
        {
            if (_mode != Mode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");

            TotalIn += span.Length;

            fixed (byte* readPtr = span) // [In] Compressed
            fixed (byte* writePtr = _internalBuf) // [Out] Will-be-decompressed
            {
                switch (NativeMethods.LongBitType)
                {
                    case NativeMethods.LongBits.Long32:
                        {
                            _zs32.NextIn = readPtr;
                            _zs32.AvailIn = (uint)span.Length;
                            _zs32.NextOut = writePtr + _internalBufPos;
                            _zs32.AvailOut = (uint)(_internalBuf.Length - _internalBufPos);

                            while (_zs32.AvailIn != 0)
                            {
                                uint outCount = _zs32.AvailOut;
                                ZLibReturn ret = NativeMethods.L32.Deflate(_zs32, ZLibFlush.NoFlush);
                                _internalBufPos += (int)(outCount - _zs32.AvailOut);

                                if (_zs32.AvailOut == 0)
                                {
                                    BaseStream.Write(_internalBuf, 0, _internalBuf.Length);
                                    TotalOut += _internalBuf.Length;

                                    _internalBufPos = 0;
                                    _zs32.NextOut = writePtr;
                                    _zs32.AvailOut = (uint)_internalBuf.Length;
                                }

                                ZLibException.CheckReturnValue(ret, _zs32);
                            }
                            break;
                        }
                    case NativeMethods.LongBits.Long64:
                        {
                            _zs64.NextIn = readPtr;
                            _zs64.AvailIn = (uint)span.Length;
                            _zs64.NextOut = writePtr + _internalBufPos;
                            _zs64.AvailOut = (uint)(_internalBuf.Length - _internalBufPos);

                            while (_zs64.AvailIn != 0)
                            {
                                uint outCount = _zs64.AvailOut;
                                ZLibReturn ret = NativeMethods.L64.Deflate(_zs64, ZLibFlush.NoFlush);
                                _internalBufPos += (int)(outCount - _zs64.AvailOut);

                                if (_zs64.AvailOut == 0)
                                {
                                    BaseStream.Write(_internalBuf, 0, _internalBuf.Length);
                                    TotalOut += _internalBuf.Length;

                                    _internalBufPos = 0;
                                    _zs64.NextOut = writePtr;
                                    _zs64.AvailOut = (uint)_internalBuf.Length;
                                }

                                ZLibException.CheckReturnValue(ret, _zs64);
                            }
                            break;
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
                switch (NativeMethods.LongBitType)
                {
                    case NativeMethods.LongBits.Long32:
                        {
                            _zs32.NextIn = (byte*)0;
                            _zs32.AvailIn = 0;
                            _zs32.NextOut = writePtr + _internalBufPos;
                            _zs32.AvailOut = (uint)(_internalBuf.Length - _internalBufPos);

                            ZLibReturn ret = ZLibReturn.Ok;
                            while (ret != ZLibReturn.StreamEnd)
                            {
                                if (_zs32.AvailOut != 0)
                                {
                                    uint outCount = _zs32.AvailOut;
                                    ret = NativeMethods.L32.Deflate(_zs32, ZLibFlush.Finish);

                                    _internalBufPos += (int)(outCount - _zs32.AvailOut);

                                    if (ret != ZLibReturn.StreamEnd && ret != ZLibReturn.Ok)
                                        throw new ZLibException(ret, _zs32.LastErrorMsg);
                                }

                                BaseStream.Write(_internalBuf, 0, _internalBufPos);
                                TotalOut += _internalBufPos;

                                _internalBufPos = 0;
                                _zs32.NextOut = writePtr;
                                _zs32.AvailOut = (uint)_internalBuf.Length;
                            }

                            break;
                        }
                    case NativeMethods.LongBits.Long64:
                        {
                            _zs64.NextIn = (byte*)0;
                            _zs64.AvailIn = 0;
                            _zs64.NextOut = writePtr + _internalBufPos;
                            _zs64.AvailOut = (uint)(_internalBuf.Length - _internalBufPos);

                            ZLibReturn ret = ZLibReturn.Ok;
                            while (ret != ZLibReturn.StreamEnd)
                            {
                                if (_zs64.AvailOut != 0)
                                {
                                    uint outCount = _zs64.AvailOut;
                                    ret = NativeMethods.L64.Deflate(_zs64, ZLibFlush.Finish);

                                    _internalBufPos += (int)(outCount - _zs64.AvailOut);

                                    if (ret != ZLibReturn.StreamEnd && ret != ZLibReturn.Ok)
                                        throw new ZLibException(ret, _zs64.LastErrorMsg);
                                }

                                BaseStream.Write(_internalBuf, 0, _internalBufPos);
                                TotalOut += _internalBufPos;

                                _internalBufPos = 0;
                                _zs64.NextOut = writePtr;
                                _zs64.AvailOut = (uint)_internalBuf.Length;
                            }

                            break;
                        }
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

    #region ZLibStream
    /// <inheritdoc />
    /// <summary>
    /// zlib header + adler32 et end.
    /// wraps a deflate stream
    /// </summary>
    public sealed class ZLibStream : DeflateStream
    {
        /// <summary>
        /// Create compressing ZLibStream.
        /// </summary>
        public ZLibStream(Stream baseStream, ZLibCompressOptions compOpts)
            : base(baseStream, compOpts, ZLibWriteType.ZLib) { }

        /// <summary>
        /// Create decompressing ZLibStream.
        /// </summary>
        public ZLibStream(Stream baseStream, ZLibDecompressOptions decompOpts)
            : base(baseStream, decompOpts, ZLibOpenType.ZLib) { }
    }
    #endregion

    #region GZipStream
    /// <inheritdoc />
    /// <summary>
    /// Saved to file (.gz) can be opened with zip utils.
    /// Have hdr + crc32 at end.
    /// Wraps a deflate stream
    /// </summary>
    public class GZipStream : DeflateStream
    {
        /// <summary>
        /// Create compressing GZipStream.
        /// </summary>
        public GZipStream(Stream baseStream, ZLibCompressOptions compOpts)
            : base(baseStream, compOpts, ZLibWriteType.GZip) { }

        /// <summary>
        /// Create decompressing GZipStream.
        /// </summary>
        public GZipStream(Stream baseStream, ZLibDecompressOptions decompOpts)
            : base(baseStream, decompOpts, ZLibOpenType.GZip) { }
    }
    #endregion
}
