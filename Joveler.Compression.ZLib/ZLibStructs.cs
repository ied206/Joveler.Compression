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

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Joveler.Compression.ZLib
{
    #region Enums
    internal enum ZLibFlush : int
    {
        NoFlush = 0,
        PartialFlush = 1,
        SyncFlush = 2,
        FullFlush = 3,
        Finish = 4,
        Block = 5,
        Trees = 6,
    }

    /// <summary>
    /// Return codes for the compression/decompression functions.
    /// Negative values are errors, positive values are used for special but normal events.
    /// </summary>
    public enum ZLibRet : int
    {
        Ok = 0,
        StreamEnd = 1,
        NeedDictionary = 2,
        ErrNo = -1,
        StreamError = -2,
        DataError = -3,
        MemoryError = -4,
        BufferError = -5,
        VersionError = -6,
    }

    internal enum ZLibCompStrategy : int
    {
        Filtered = 1,
        HuffmanOnly = 2,
        Rle = 3,
        Fixed = 4,
        Default = 0,
    }

    /// <summary>
    /// Possible values of the data_type field for deflate()
    /// </summary>
    internal enum ZLibDataType : int
    {
        Binary = 0,
        Ascii = 1,
        Unknown = 2,
    }

    /// <summary>
    /// The deflate compression method (the only one supported in this version)
    /// </summary>
    internal enum ZLibCompMethod : int
    {
        Deflated = 8,
    }

    public enum ZLibCompLevel : int
    {
        Default = -1,
        NoCompression = 0,
        BestSpeed = 1,
        BestCompression = 9,
        Level0 = 0,
        Level1 = 1,
        Level2 = 2,
        Level3 = 3,
        Level4 = 4,
        Level5 = 5,
        Level6 = 6,
        Level7 = 7,
        Level8 = 8,
        Level9 = 9,
    }

    public enum ZLibWindowBits : int
    {
        Default = 15,
        Bits9 = 9,
        Bits10 = 10,
        Bits11 = 11,
        Bits12 = 12,
        Bits13 = 13,
        Bits14 = 14,
        Bits15 = 15,
    }

    public enum ZLibMemLevel : int
    {
        Default = 8,
        Level1 = 1,
        Level2 = 2,
        Level3 = 3,
        Level4 = 4,
        Level5 = 5,
        Level6 = 6,
        Level7 = 7,
        Level8 = 8,
        Level9 = 9,
    }
    #endregion

    #region (Intended to be internal) ZLibOperateMode, ZLibOperateFormat
    internal enum ZLibStreamOperateMode
    {
        Compress,
        ParallelCompress,
        Decompress,
    }


    public enum ZLibOperateFormat
    {
        Deflate,
        ZLib,
        GZip,
        /// <summary>
        /// Support both ZLib and GZip format in decompress mode.
        /// <para>Only for decompress mode.</para>
        /// </summary>
        BothZLibGZip,
    }
    #endregion

    #region ZLibCompileFlags
    /// <summary>
    /// Flags indicating compile-time options.
    /// </summary>
    public class ZLibCompileFlags
    {
        #region Constructor
        public ZLibCompileFlags(uint flags)
        {
            RawFlags = flags;

            CUIntSize = ParseTypeSize(flags, 0);
            CULongSize = ParseTypeSize(flags, 2);
            PtrSize = ParseTypeSize(flags, 4);
            ZOffsetSize = ParseTypeSize(flags, 6);

            IsDebug = ParseBool(flags, 8);
            IsWinApi = ParseBool(flags, 10);
            IsBuildFixed = ParseBool(flags, 12);
            IsDynamicCrcTable = ParseBool(flags, 13);

            NoGZCompress = ParseBool(flags, 16);
            NoGZip = ParseBool(flags, 17);

            PKZipBugWorkaround = ParseBool(flags, 20);
            FastestDeflateOnly = ParseBool(flags, 21);
        }
        #endregion

        #region Flag Properties
        public uint RawFlags { get; }

        // Type sizes, two bits each, 00 = 16 bits, 01 = 32, 10 = 64, 11 = other:
        /// <summary>
        /// 1.0: size of unsigned int
        /// </summary>
        /// <remarks>
        /// Value is set as byte size, one of 2, 4 or 8. If not one of them, value is 0.
        /// </remarks>
        public int CUIntSize { get; }
        /// <summary>
        /// 3.2: size of unsigned long
        /// </summary>
        /// <remarks>
        /// Value is set as byte size, one of 2, 4 or 8. If not one of them, value is 0.
        /// </remarks>
        public int CULongSize { get; }
        /// <summary>
        /// 5.4: size of void * (pointer)
        /// </summary>
        /// <remarks>
        /// Value is set as byte size, one of 2, 4 or 8. If not one of them, value is 0.
        /// </remarks>
        public int PtrSize { get; }
        /// <summary>
        /// 7.6: size of z_off_t
        /// </summary>
        public int ZOffsetSize { get; }
        // Compiler, assembler, and debug options:
        /// <summary>
        /// 8: ZLIB_DEBUG
        /// </summary>
        public bool IsDebug { get; }
        /// <summary>
        /// 10: ZLIB_WINAPI -- exported functions use the WINAPI calling convention
        /// </summary>
        public bool IsWinApi { get; }
        // One-time table building (smaller code, but not thread-safe if true):
        /// <summary>
        /// 12: BUILDFIXED -- build static block decoding tables when needed (not supported by zlib-ng)
        /// </summary>
        public bool IsBuildFixed { get; }
        /// <summary>
        /// 13: DYNAMIC_CRC_TABLE -- build CRC calculation tables when needed
        /// </summary>
        public bool IsDynamicCrcTable { get; }
        // Library content (indicates missing functionality):
        /// <summary>
        /// 16: NO_GZCOMPRESS -- gz* functions cannot compress (to avoid linking
        ///                      deflate code when not needed)
        /// </summary>
        public bool NoGZCompress { get; }
        /// <summary>
        /// 17: NO_GZIP -- deflate can't write gzip streams, and inflate can't detect
        ///                and decode gzip streams(to avoid linking crc code)
        /// </summary>
        public bool NoGZip { get; }
        // Operation variations (changes in library functionality):
        /// <summary>
        /// 20: PKZIP_BUG_WORKAROUND -- slightly more permissive inflate
        /// </summary>
        public bool PKZipBugWorkaround { get; }
        /// <summary>
        /// 21: FASTEST -- deflate algorithm with only one, lowest compression level
        /// </summary>
        public bool FastestDeflateOnly { get; }
        #endregion

        #region (static) Parse methods
        private static int ParseTypeSize(uint flags, int bitPos)
        {
            uint andVal = (flags >> bitPos) & 0b11;
            return andVal switch
            {
                0 => 2,
                1 => 4,
                2 => 8,
                _ => 0,
            };
        }

        private static bool ParseBool(uint flags, int bitPos)
        {
            uint andVal = (flags >> bitPos) & 0b1;
            return andVal != 0;
        }
        #endregion
    }
    #endregion

    #region ZStreamBase (inheritance)
    [StructLayout(LayoutKind.Sequential)]
    internal abstract unsafe class ZStreamBase
    {
        public static uint DowncastCULong64(ulong val64, [CallerMemberName] string caller = "")
        {
            if (uint.MaxValue < val64)
                throw new OverflowException($"{caller}: [{val64}] cannot be represented in 32bit unsigned integer.");
            return (uint)val64;
        }

        /// <summary>
        /// next input byte
        /// </summary>
        public abstract byte* NextIn { get; set; }
        /// <summary>
        /// number of bytes available at next_in
        /// </summary>
        public abstract uint AvailIn { get; set; }
        /// <summary>
        /// total number of input bytes read so far
        /// </summary>
        public abstract ulong TotalIn { get; set; }

        /// <summary>
        /// next output byte will go here
        /// </summary>
        public abstract byte* NextOut { get; set; }
        /// <summary>
        /// remaining free space at next_out
        /// </summary>
        public abstract uint AvailOut { get; set; }
        /// <summary>
        /// total number of bytes output so far
        /// </summary>
        public abstract ulong TotalOut { get; set; }

        /// <summary>
        /// last error message, NULL if no error
        /// </summary>
        protected abstract IntPtr Msg { get; set; }
        /// <summary>
        /// last error message, NULL if no error
        /// </summary>
        public string? LastErrorMsg => Marshal.PtrToStringAnsi(Msg);

        /// <summary>
        /// best guess about the data type: binary or text for deflate, or the decoding state for inflate
        /// </summary>
        public abstract ZLibDataType DataType { get; set; }
        /// <summary>
        /// Adler-32 or CRC-32 value of the uncompressed data
        /// </summary>
        public abstract uint Adler { get; set; }
    }
    #endregion

    #region ZStream (32bit long)
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe sealed class ZStreamL32 : ZStreamBase
    {
        /// <inheritdoc/>
        public override unsafe byte* NextIn
        {
            get => _nextIn;
            set => _nextIn = value;
        }
        private byte* _nextIn = null;

        /// <inheritdoc/>
        public override uint AvailIn
        {
            get => _availIn;
            set => _availIn = value;
        }
        private uint _availIn = 0;

        /// <inheritdoc/>
        public override ulong TotalIn
        {
            get => _totalIn;
            set => _totalIn = DowncastCULong64(value);
        }
        private uint _totalIn = 0;

        /// <inheritdoc/>
        public override unsafe byte* NextOut
        {
            get => _nextOut;
            set => _nextOut = value;
        }
        private byte* _nextOut = null;
        /// <inheritdoc/>
        public override uint AvailOut
        {
            get => _availOut;
            set => _availOut = value;
        }
        private uint _availOut = 0;
        /// <inheritdoc/>
        public override ulong TotalOut
        {
            get => _totalOut;
            set => _totalOut = DowncastCULong64(value);
        }
        private uint _totalOut = 0;

        protected override IntPtr Msg
        {
            get => _msg;
            set => _msg = value;
        }
        private IntPtr _msg = IntPtr.Zero;
        /// <summary>
        /// not visible by applications
        /// </summary>
        private readonly IntPtr _state = IntPtr.Zero;

        /// <summary>
        /// used to allocate the internal state
        /// </summary>
        private readonly IntPtr _zalloc = IntPtr.Zero;
        /// <summary>
        /// used to free the internal state
        /// </summary>
        private readonly IntPtr _zfree = IntPtr.Zero;
        /// <summary>
        /// private data object passed to zalloc and zfree
        /// </summary>
        private readonly IntPtr _opaque = IntPtr.Zero;

        /// <inheritdoc/>
        public override ZLibDataType DataType
        {
            get => _dataType;
            set => _dataType = value;
        }
        private ZLibDataType _dataType = ZLibDataType.Binary;
        /// <inheritdoc/>
        public override uint Adler
        {
            get => _adler;
            set => _adler = value;
        }
        private uint _adler = 0;
        /// <summary>
        /// reserved for future use
        /// </summary>
        private readonly uint _reserved = 0;
    }
    #endregion

    #region ZStream (64bit long)
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe sealed class ZStreamL64 : ZStreamBase
    {
        /// <inheritdoc/>
        public override unsafe byte* NextIn
        {
            get => _nextIn;
            set => _nextIn = value;
        }
        private byte* _nextIn = null;

        /// <inheritdoc/>
        public override uint AvailIn
        {
            get => _availIn;
            set => _availIn = value;
        }
        private uint _availIn = 0;

        /// <inheritdoc/>
        public override ulong TotalIn
        {
            get => _totalIn;
            set => _totalIn = value;
        }
        private ulong _totalIn = 0;

        /// <inheritdoc/>
        public override unsafe byte* NextOut
        {
            get => _nextOut;
            set => _nextOut = value;
        }
        private byte* _nextOut = null;
        /// <inheritdoc/>
        public override uint AvailOut
        {
            get => _availOut;
            set => _availOut = value;
        }
        private uint _availOut = 0;
        /// <inheritdoc/>
        public override ulong TotalOut
        {
            get => _totalOut;
            set => _totalOut = value;
        }
        private ulong _totalOut = 0;

        protected override IntPtr Msg
        {
            get => _msg;
            set => _msg = value;
        }
        private IntPtr _msg = IntPtr.Zero;
        /// <summary>
        /// not visible by applications
        /// </summary>
        private readonly IntPtr _state = IntPtr.Zero;

        /// <summary>
        /// used to allocate the internal state
        /// </summary>
        private readonly IntPtr _zalloc = IntPtr.Zero;
        /// <summary>
        /// used to free the internal state
        /// </summary>
        private readonly IntPtr _zfree = IntPtr.Zero;
        /// <summary>
        /// private data object passed to zalloc and zfree
        /// </summary>
        private readonly IntPtr _opaque = IntPtr.Zero;

        /// <inheritdoc/>
        public override ZLibDataType DataType
        {
            get => _dataType;
            set => _dataType = value;
        }
        private ZLibDataType _dataType = ZLibDataType.Binary;
        /// <inheritdoc/>
        public override uint Adler
        {
            get => DowncastCULong64(_adler);
            set => _adler = value;
        }
        private ulong _adler = 0;
        /// <summary>
        /// reserved for future use
        /// </summary>
        private readonly ulong _reserved = 0;
    }
    #endregion

    #region ZStream for zlib-ng (32bit long)
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe sealed class ZNgStreamL32 : ZStreamBase
    {
        /// <inheritdoc/>
        public override unsafe byte* NextIn
        {
            get => _nextIn;
            set => _nextIn = value;
        }
        private byte* _nextIn = null;

        /// <inheritdoc/>
        public override uint AvailIn
        {
            get => _availIn;
            set => _availIn = value;
        }
        private uint _availIn = 0;

        /// <inheritdoc/>
        public override ulong TotalIn
        {
            get => _totalIn.ToUInt64();
            set => _totalIn = new UIntPtr(value);
        }
        private UIntPtr _totalIn = UIntPtr.Zero;

        /// <inheritdoc/>
        public override unsafe byte* NextOut
        {
            get => _nextOut;
            set => _nextOut = value;
        }
        private byte* _nextOut = null;
        /// <inheritdoc/>
        public override uint AvailOut
        {
            get => _availOut;
            set => _availOut = value;
        }
        private uint _availOut = 0;
        /// <inheritdoc/>
        public override ulong TotalOut
        {
            get => _totalOut.ToUInt64();
            set => _totalOut = new UIntPtr(value);
        }
        private UIntPtr _totalOut = UIntPtr.Zero;

        /// <inheritdoc/>
        protected override IntPtr Msg
        {
            get => _msg;
            set => _msg = value;
        }
        private IntPtr _msg = IntPtr.Zero;
        /// <summary>
        /// not visible by applications
        /// </summary>
        private readonly IntPtr _state = IntPtr.Zero;

        /// <summary>
        /// used to allocate the internal state
        /// </summary>
        private readonly IntPtr _zalloc = IntPtr.Zero;
        /// <summary>
        /// used to free the internal state
        /// </summary>
        private readonly IntPtr _zfree = IntPtr.Zero;
        /// <summary>
        /// private data object passed to zalloc and zfree
        /// </summary>
        private readonly IntPtr _opaque = IntPtr.Zero;

        /// <inheritdoc/>
        public override ZLibDataType DataType
        {
            get => _dataType;
            set => _dataType = value;
        }
        private ZLibDataType _dataType = ZLibDataType.Binary;
        /// <inheritdoc/>
        public override uint Adler
        {
            get => (uint)_adler;
            set => _adler = value;
        }
        private ulong _adler = 0;
        private readonly uint _reserved = 0;
    }
    #endregion

    #region ZStream for zlib-ng (64bit long)
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe sealed class ZNgStreamL64 : ZStreamBase
    {
        /// <inheritdoc/>
        public override unsafe byte* NextIn
        {
            get => _nextIn;
            set => _nextIn = value;
        }
        private byte* _nextIn = null;

        /// <inheritdoc/>
        public override uint AvailIn
        {
            get => _availIn;
            set => _availIn = value;
        }
        private uint _availIn = 0;

        /// <inheritdoc/>
        public override ulong TotalIn
        {
            get => _totalIn.ToUInt64();
            set => _totalIn = new UIntPtr(value);
        }
        private UIntPtr _totalIn = UIntPtr.Zero;

        /// <inheritdoc/>
        public override unsafe byte* NextOut
        {
            get => _nextOut;
            set => _nextOut = value;
        }
        private byte* _nextOut = null;
        /// <inheritdoc/>
        public override uint AvailOut
        {
            get => _availOut;
            set => _availOut = value;
        }
        private uint _availOut = 0;
        /// <inheritdoc/>
        public override ulong TotalOut
        {
            get => _totalOut.ToUInt64();
            set => _totalOut = new UIntPtr(value);
        }
        private UIntPtr _totalOut = UIntPtr.Zero;

        /// <inheritdoc/>
        protected override IntPtr Msg
        {
            get => _msg;
            set => _msg = value;
        }
        private IntPtr _msg = IntPtr.Zero;
        /// <summary>
        /// not visible by applications
        /// </summary>
        private readonly IntPtr _state = IntPtr.Zero;

        /// <summary>
        /// used to allocate the internal state
        /// </summary>
        private readonly IntPtr _zalloc = IntPtr.Zero;
        /// <summary>
        /// used to free the internal state
        /// </summary>
        private readonly IntPtr _zfree = IntPtr.Zero;
        /// <summary>
        /// private data object passed to zalloc and zfree
        /// </summary>
        private readonly IntPtr _opaque = IntPtr.Zero;

        /// <inheritdoc/>
        public override ZLibDataType DataType
        {
            get => _dataType;
            set => _dataType = value;
        }
        private ZLibDataType _dataType = ZLibDataType.Binary;
        /// <inheritdoc/>
        public override uint Adler
        {
            get => (uint)_adler;
            set => _adler = value;
        }
        private ulong _adler = 0;
        private readonly ulong _reserved = 0;
    }
    #endregion
}
