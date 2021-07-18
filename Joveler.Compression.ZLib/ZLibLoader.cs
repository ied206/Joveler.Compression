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
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace Joveler.Compression.ZLib
{
    internal class ZLibLoader : DynLoaderBase
    {
        #region Constructor
        public ZLibLoader() : base() { }
        #endregion

        #region LP64 and LLP64
        internal L32d L32 = new L32d();
        internal L64d L64 = new L64d();
        #endregion

        #region (override) DefaultLibFileName
        protected override string DefaultLibFileName
        {
            get
            {
#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "libz.so.1";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "libz.dylib";
#endif
                throw new PlatformNotSupportedException();
            }
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        protected override void LoadFunctions()
        {
            L32.Lib = this;
            L64.Lib = this;

            switch (PlatformLongSize)
            {
                case PlatformLongSize.Long32:
                    #region Deflate - DeflateInit2, Deflate, DeflateEnd
                    L32.DeflateInit2 = GetFuncPtr<L32d.deflateInit2_>(nameof(L32d.deflateInit2_));
                    L32.Deflate = GetFuncPtr<L32d.deflate>(nameof(L32d.deflate));
                    L32.DeflateEnd = GetFuncPtr<L32d.deflateEnd>(nameof(L32d.deflateEnd));
                    #endregion

                    #region Inflate - InflateInit2, Inflate, InflateEnd
                    L32.InflateInit2 = GetFuncPtr<L32d.inflateInit2_>(nameof(L32d.inflateInit2_));
                    L32.Inflate = GetFuncPtr<L32d.inflate>(nameof(L32d.inflate));
                    L32.InflateEnd = GetFuncPtr<L32d.inflateEnd>(nameof(L32d.inflateEnd));
                    #endregion
                    break;
                case PlatformLongSize.Long64:
                    #region Deflate - DeflateInit2, Deflate, DeflateEnd
                    L64.DeflateInit2 = GetFuncPtr<L64d.deflateInit2_>(nameof(L64d.deflateInit2_));
                    L64.Deflate = GetFuncPtr<L64d.deflate>(nameof(L64d.deflate));
                    L64.DeflateEnd = GetFuncPtr<L64d.deflateEnd>(nameof(L64d.deflateEnd));
                    #endregion

                    #region Inflate - InflateInit2, Inflate, InflateEnd
                    L64.InflateInit2 = GetFuncPtr<L64d.inflateInit2_>(nameof(L64d.inflateInit2_));
                    L64.Inflate = GetFuncPtr<L64d.inflate>(nameof(L64d.inflate));
                    L64.InflateEnd = GetFuncPtr<L64d.inflateEnd>(nameof(L64d.inflateEnd));
                    #endregion
                    break;
            }

            #region Checksum - Adler32, Crc32
            Adler32 = GetFuncPtr<adler32>(nameof(adler32));
            Crc32 = GetFuncPtr<crc32>(nameof(crc32));
            #endregion

            #region Version - ZLibVersion
            ZLibVersionPtr = GetFuncPtr<zlibVersion>(nameof(zlibVersion));
            #endregion
        }

        protected override void ResetFunctions()
        {
            switch (PlatformLongSize)
            {
                case PlatformLongSize.Long32:
                    #region Deflate - DeflateInit2, Deflate, DeflateEnd
                    L32.DeflateInit2 = null;
                    L32.Deflate = null;
                    L32.DeflateEnd = null;
                    #endregion

                    #region Inflate - InflateInit2, Inflate, InflateEnd
                    L32.InflateInit2 = null;
                    L32.Inflate = null;
                    L32.InflateEnd = null;
                    #endregion
                    break;
                case PlatformLongSize.Long64:
                    #region Deflate - DeflateInit2, Deflate, DeflateEnd
                    L64.DeflateInit2 = null;
                    L64.Deflate = null;
                    L64.DeflateEnd = null;
                    #endregion

                    #region Inflate - InflateInit2, Inflate, InflateEnd
                    L64.InflateInit2 = null;
                    L64.Inflate = null;
                    L64.InflateEnd = null;
                    #endregion
                    break;
            }

            #region Checksum - Adler32, Crc32
            Adler32 = null;
            Crc32 = null;
            #endregion

            #region Version - ZLibVersion
            ZLibVersionPtr = null;
            #endregion
        }
        #endregion

        #region zlib Function Pointers
        internal class L64d
        {
            public ZLibLoader Lib { get; internal set; }

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet deflateInit2_(
                ZStreamL64 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                int windowBits,
                ZLibMemLevel memLevel,
                ZLibCompStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal deflateInit2_ DeflateInit2;

            internal ZLibRet DeflateInit(ZStreamL64 strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel)
            {
                string zlibVer = Lib.ZLibVersion();
                return DeflateInit2(strm, level, ZLibCompMethod.Deflated, windowBits, memLevel,
                        ZLibCompStrategy.Default, zlibVer, Marshal.SizeOf<ZStreamL64>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet deflate(
                ZStreamL64 strm,
                ZLibFlush flush);
            internal deflate Deflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet deflateEnd(
                ZStreamL64 strm);
            internal deflateEnd DeflateEnd;
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet inflateInit2_(
                ZStreamL64 strm,
                int windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal inflateInit2_ InflateInit2;

            internal ZLibRet InflateInit(ZStreamL64 strm, int windowBits)
            {
                string zlibVer = Lib.ZLibVersion();
                return InflateInit2(strm, windowBits, zlibVer, Marshal.SizeOf<ZStreamL64>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet inflate(
                ZStreamL64 strm,
                ZLibFlush flush);
            internal inflate Inflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet inflateEnd(
                ZStreamL64 strm);
            internal inflateEnd InflateEnd;
            #endregion
        }

        internal class L32d
        {
            public ZLibLoader Lib { get; internal set; }

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet deflateInit2_(
                ZStreamL32 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                int windowBits,
                ZLibMemLevel memLevel,
                ZLibCompStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal deflateInit2_ DeflateInit2;

            internal ZLibRet DeflateInit(ZStreamL32 strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel)
            {
                string zlibVer = Lib.ZLibVersion();
                return DeflateInit2(strm, level, ZLibCompMethod.Deflated, windowBits, memLevel,
                        ZLibCompStrategy.Default, zlibVer, Marshal.SizeOf<ZStreamL32>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet deflate(
                ZStreamL32 strm,
                ZLibFlush flush);
            internal deflate Deflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet deflateEnd(
                ZStreamL32 strm);
            internal deflateEnd DeflateEnd;
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet inflateInit2_(
                ZStreamL32 strm,
                int windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal inflateInit2_ InflateInit2;

            internal ZLibRet InflateInit(ZStreamL32 strm, int windowBits)
            {
                string zlibVer = Lib.ZLibVersion();
                return InflateInit2(strm, windowBits, zlibVer, Marshal.SizeOf<ZStreamL32>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet inflate(
                ZStreamL32 strm,
                ZLibFlush flush);
            internal inflate Inflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibRet inflateEnd(ZStreamL32 strm);
            internal inflateEnd InflateEnd;
            #endregion
        }

        #region Checksum - Adler32, Crc32
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal unsafe delegate uint adler32(
            uint adler,
            byte* buf,
            uint len);
        internal adler32 Adler32;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal unsafe delegate uint crc32(
            uint crc,
            byte* buf,
            uint len);
        internal crc32 Crc32;
        #endregion

        #region Version - ZLibVersion
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr zlibVersion();
        private zlibVersion ZLibVersionPtr;
        internal string ZLibVersion() => Marshal.PtrToStringAnsi(ZLibVersionPtr());
        #endregion
        #endregion
    }
}
