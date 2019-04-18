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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

namespace Joveler.Compression.ZLib
{
    #region DeflateStream
    public class DeflateStream : Stream
    {
        #region Fields and Properties
        private readonly ZLibMode _mode;
        private readonly bool _leaveOpen;
        private bool _disposed = false;

        private ZStreamL32 _zs32;
        private ZStreamL64 _zs64;
        private GCHandle _zsPtr;

        protected virtual ZLibOpenType OpenType => ZLibOpenType.Deflate;
        protected virtual ZLibWriteType WriteType => ZLibWriteType.Deflate;

        private readonly byte[] _internalBuf;
        private int _internalBufPos = 0;

        public long TotalIn { get; private set; } = 0;
        public long TotalOut { get; private set; } = 0;
        public Stream BaseStream { get; private set; }
        #endregion

        #region Constructor
        public DeflateStream(Stream stream, ZLibMode mode)
            : this(stream, mode, ZLibCompLevel.Default, false) { }

        public DeflateStream(Stream stream, ZLibMode mode, ZLibCompLevel level) :
            this(stream, mode, level, false)
        { }

        public DeflateStream(Stream stream, ZLibMode mode, bool leaveOpen) :
            this(stream, mode, ZLibCompLevel.Default, leaveOpen)
        { }

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        public DeflateStream(Stream stream, ZLibMode mode, ZLibCompLevel level, bool leaveOpen)
        {
            NativeMethods.CheckZLibLoaded();

            _leaveOpen = leaveOpen;
            BaseStream = stream;
            _mode = mode;
            _internalBufPos = 0;

            Debug.Assert(0 < NativeMethods.BufferSize, "Internal Logic Error at DeflateStream");
            _internalBuf = new byte[NativeMethods.BufferSize];

            switch (NativeMethods.LongBitType)
            {
                case NativeMethods.LongBits.Long32:
                    {
                        _zs32 = new ZStreamL32();
                        _zsPtr = GCHandle.Alloc(_zs32, GCHandleType.Pinned);

                        ZLibReturnCode ret;
                        if (_mode == ZLibMode.Compress)
                            ret = NativeMethods.L32.DeflateInit(_zs32, level, WriteType);
                        else
                            ret = NativeMethods.L32.InflateInit(_zs32, OpenType);
                        ZLibException.CheckReturnValue(ret, _zs32);
                        break;
                    }
                case NativeMethods.LongBits.Long64:
                    {
                        _zs64 = new ZStreamL64();
                        _zsPtr = GCHandle.Alloc(_zs64, GCHandleType.Pinned);

                        ZLibReturnCode ret;
                        if (_mode == ZLibMode.Compress)
                            ret = NativeMethods.L64.DeflateInit(_zs64, level, WriteType);
                        else
                            ret = NativeMethods.L64.InflateInit(_zs64, OpenType);
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
                    if (_mode == ZLibMode.Compress)
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
                                if (_mode == ZLibMode.Compress)
                                    NativeMethods.L32.DeflateEnd(_zs32);
                                else
                                    NativeMethods.L32.InflateEnd(_zs32);
                                _zsPtr.Free();
                                _zs32 = null;
                            }
                            break;
                        }
                    case NativeMethods.LongBits.Long64:
                        {
                            if (_zs64 != null)
                            {
                                if (_mode == ZLibMode.Compress)
                                    NativeMethods.L64.DeflateEnd(_zs64);
                                else
                                    NativeMethods.L64.InflateEnd(_zs64);
                                _zsPtr.Free();
                                _zs64 = null;
                            }
                            break;
                        }
                }

                _disposed = true;
            }
        }
        #endregion

        #region ValidateReadWriteArgs
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ValidateReadWriteArgs(byte[] buffer, int offset, int count)
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

        #region Stream Methods
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_mode != ZLibMode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");

            ValidateReadWriteArgs(buffer, offset, count);

            Span<byte> span = buffer.AsSpan(offset, count);
            return Read(span);
        }

        public unsafe int Read(Span<byte> span)
        {
            if (_mode != ZLibMode.Decompress)
                throw new NotSupportedException("Read() not supported on compression");

            int readLen = 0;
            if (_internalBufPos != -1)
            {
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
                                    ZLibReturnCode ret = NativeMethods.L32.Inflate(_zs32, ZLibFlush.NO_FLUSH);

                                    _internalBufPos += (int)(inCount - _zs32.AvailIn);
                                    readLen += (int)(outCount - _zs32.AvailOut);

                                    if (ret == ZLibReturnCode.STREAM_END)
                                    {
                                        _internalBufPos = -1; // magic for StreamEnd
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
                                    ZLibReturnCode ret = NativeMethods.L64.Inflate(_zs64, ZLibFlush.NO_FLUSH);

                                    _internalBufPos += (int)(inCount - _zs64.AvailIn);
                                    readLen += (int)(outCount - _zs64.AvailOut);

                                    if (ret == ZLibReturnCode.STREAM_END)
                                    {
                                        _internalBufPos = -1; // magic for StreamEnd
                                        break;
                                    }

                                    ZLibException.CheckReturnValue(ret, _zs64);
                                }
                            }
                            break;
                    }
                }
            }

            TotalOut += readLen;
            return readLen;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_mode != ZLibMode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");

            ValidateReadWriteArgs(buffer, offset, count);

            ReadOnlySpan<byte> span = buffer.AsSpan(offset, count);
            Write(span);
        }

        public unsafe void Write(ReadOnlySpan<byte> span)
        {
            if (_mode != ZLibMode.Compress)
                throw new NotSupportedException("Write() not supported on decompression");

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
                                ZLibReturnCode ret = NativeMethods.L32.Deflate(_zs32, ZLibFlush.NO_FLUSH);
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
                                ZLibReturnCode ret = NativeMethods.L64.Deflate(_zs64, ZLibFlush.NO_FLUSH);
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

            TotalIn += span.Length;
        }

        public override unsafe void Flush()
        {
            if (_mode == ZLibMode.Decompress)
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

                            ZLibReturnCode ret = ZLibReturnCode.OK;
                            while (ret != ZLibReturnCode.STREAM_END)
                            {
                                if (_zs32.AvailOut != 0)
                                {
                                    uint outCount = _zs32.AvailOut;
                                    ret = NativeMethods.L32.Deflate(_zs32, ZLibFlush.FINISH);

                                    _internalBufPos += (int)(outCount - _zs32.AvailOut);

                                    if (ret != ZLibReturnCode.STREAM_END && ret != ZLibReturnCode.OK)
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

                            ZLibReturnCode ret = ZLibReturnCode.OK;
                            while (ret != ZLibReturnCode.STREAM_END)
                            {
                                if (_zs64.AvailOut != 0)
                                {
                                    uint outCount = _zs64.AvailOut;
                                    ret = NativeMethods.L64.Deflate(_zs64, ZLibFlush.FINISH);

                                    _internalBufPos += (int)(outCount - _zs64.AvailOut);

                                    if (ret != ZLibReturnCode.STREAM_END && ret != ZLibReturnCode.OK)
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

        public override bool CanRead => _mode == ZLibMode.Decompress && BaseStream.CanRead;
        public override bool CanWrite => _mode == ZLibMode.Compress && BaseStream.CanWrite;
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
                if (_mode == ZLibMode.Compress)
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
    }
    #endregion

    #region ZLibStream
    /// <inheritdoc />
    /// <summary>
    /// zlib header + adler32 et end.
    /// wraps a deflate stream
    /// </summary>
    public class ZLibStream : DeflateStream
    {
        public ZLibStream(Stream stream, ZLibMode mode)
            : base(stream, mode) { }

        public ZLibStream(Stream stream, ZLibMode mode, bool leaveOpen) :
            base(stream, mode, leaveOpen)
        { }

        public ZLibStream(Stream stream, ZLibMode mode, ZLibCompLevel level) :
            base(stream, mode, level)
        { }

        public ZLibStream(Stream stream, ZLibMode mode, ZLibCompLevel level, bool leaveOpen) :
            base(stream, mode, level, leaveOpen)
        { }

        protected override ZLibOpenType OpenType => ZLibOpenType.ZLib;
        protected override ZLibWriteType WriteType => ZLibWriteType.ZLib;
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
        public GZipStream(Stream stream, ZLibMode mode)
            : base(stream, mode) { }

        public GZipStream(Stream stream, ZLibMode mode, bool leaveOpen)
            : base(stream, mode, leaveOpen) { }

        public GZipStream(Stream stream, ZLibMode mode, ZLibCompLevel level)
            : base(stream, mode, level) { }

        public GZipStream(Stream stream, ZLibMode mode, ZLibCompLevel level, bool leaveOpen)
            : base(stream, mode, level, leaveOpen) { }

        protected override ZLibOpenType OpenType => ZLibOpenType.GZip;
        protected override ZLibWriteType WriteType => ZLibWriteType.GZip;
    }
    #endregion
}
