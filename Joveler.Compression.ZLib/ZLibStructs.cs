﻿/*
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

using System;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable EnumUnderlyingTypeIsInt
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global

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
    public enum ZLibRet
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

    #region ZStream for 32bit long
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe class ZStreamL32
    {
#pragma warning disable 169
#pragma warning disable IDE0044
        /// <summary>
        /// next input byte
        /// </summary>
        public byte* NextIn;
        /// <summary>
        /// number of bytes available at next_in
        /// </summary>
        public uint AvailIn;
        /// <summary>
        /// total number of input bytes read so far
        /// </summary>
        public uint TotalIn;

        /// <summary>
        /// next output byte will go here
        /// </summary>
        public byte* NextOut;
        /// <summary>
        /// remaining free space at next_out
        /// </summary>
        public uint AvailOut;
        /// <summary>
        /// total number of bytes output so far
        /// </summary>
        public uint TotalOut;

        private IntPtr Msg = IntPtr.Zero;
        /// <summary>
        /// last error message, NULL if no error
        /// </summary>
        public string LastErrorMsg => Marshal.PtrToStringAnsi(Msg);
        /// <summary>
        /// not visible by applications
        /// </summary>
        private IntPtr State = IntPtr.Zero;

        /// <summary>
        /// used to allocate the internal state
        /// </summary>
        private IntPtr ZAlloc = IntPtr.Zero;
        /// <summary>
        /// used to free the internal state
        /// </summary>
        private IntPtr ZFree = IntPtr.Zero;
        /// <summary>
        /// private data object passed to zalloc and zfree
        /// </summary>
        private IntPtr Opaque = IntPtr.Zero;

        /// <summary>
        /// best guess about the data type: binary or text for deflate, or the decoding state for inflate
        /// </summary>
        public int DataType;
        /// <summary>
        /// Adler-32 or CRC-32 value of the uncompressed data
        /// </summary>
        public uint Adler;
        /// <summary>
        /// reserved for future use
        /// </summary>
        private uint Reserved;
#pragma warning restore 169
#pragma warning restore IDE0044
    }
    #endregion

    #region ZStream for 64bit long
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe class ZStreamL64
    {
#pragma warning disable 169
#pragma warning disable IDE0044
        /// <summary>
        /// next input byte
        /// </summary>
        public byte* NextIn;
        /// <summary>
        /// number of bytes available at next_in
        /// </summary>
        public uint AvailIn;
        /// <summary>
        /// total number of input bytes read so far
        /// </summary>
        public ulong TotalIn;

        /// <summary>
        /// next output byte will go here
        /// </summary>
        public byte* NextOut;
        /// <summary>
        /// remaining free space at next_out
        /// </summary>
        public uint AvailOut;
        /// <summary>
        /// total number of bytes output so far
        /// </summary>
        public ulong TotalOut;

        private IntPtr Msg = IntPtr.Zero;
        /// <summary>
        /// last error message, NULL if no error
        /// </summary>
        public string LastErrorMsg => Marshal.PtrToStringAnsi(Msg);
        /// <summary>
        /// not visible by applications
        /// </summary>
        private IntPtr State = IntPtr.Zero;

        /// <summary>
        /// used to allocate the internal state
        /// </summary>
        private IntPtr ZAlloc = IntPtr.Zero;
        /// <summary>
        /// used to free the internal state
        /// </summary>
        private IntPtr ZFree = IntPtr.Zero;
        /// <summary>
        /// private data object passed to zalloc and zfree
        /// </summary>
        private IntPtr Opaque = IntPtr.Zero;

        /// <summary>
        /// best guess about the data type: binary or text for deflate, or the decoding state for inflate
        /// </summary>
        public int DataType;
        /// <summary>
        /// Adler-32 or CRC-32 value of the uncompressed data
        /// </summary>
        public ulong Adler;
        /// <summary>
        /// reserved for future use
        /// </summary>
        private ulong Reserved;
#pragma warning restore 169
#pragma warning restore IDE0044
    }
    #endregion
}
