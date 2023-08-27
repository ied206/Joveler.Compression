/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

    C# Wrapper based on zlibnet v1.3.3 (https://zlibnet.codeplex.com/)
    Copyright (C) @hardon (https://www.codeplex.com/site/users/view/hardon)
    
    Maintained by Hajin Jang
    Copyright (C) 2017-2023 Hajin Jang

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

namespace Joveler.Compression.ZLib
{
    internal class ZLibLoadData
    {
        public bool IsWindowsX86Stdcall { get; set; }
    }

    internal partial class ZLibLoader : DynLoaderBase
    {
        #region Constructor
        public ZLibLoader() : base()
        {
        }
        #endregion

        #region cdecl and stdcall, LP64 and LLP64
#if !ZLIB_INHERIT_CALL
        internal CdeclL32d CL32 = new CdeclL32d();
        internal StdcallL32d SL32 = new StdcallL32d();
        internal L64d L64 = new L64d();

        internal StdcallNoLong Stdcall = new StdcallNoLong();
        internal CdeclNoLong Cdecl = new CdeclNoLong();
#else
        internal ZLibNativeAbi NativeAbi;
#endif

        internal bool UseStdcall { get; private set; } = true;
        #endregion


        #region (override) DefaultLibFileName
        protected override string DefaultLibFileName
        {
            get
            {
#if !NETFRAMEWORK
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "libz.so.1";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "libz.dylib";
#endif
                throw new PlatformNotSupportedException();
            }
        }
        #endregion

        #region HandleLoadData
        protected override void HandleLoadData(object data)
        {
            if (data is not ZLibLoadData loadData)
                return;

            // Use stdcall only if `IsX86WindowsStdcall` is active on Windows x86 platform.
            UseStdcall = loadData.IsWindowsX86Stdcall &&
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                RuntimeInformation.ProcessArchitecture == Architecture.X86;
        }
        #endregion

#if !ZLIB_INHERIT_CALL
        #region LoadFunctions, ResetFunctions
        protected override void LoadFunctions()
        {
            CL32.Lib = this;
            SL32.Lib = this;
            L64.Lib = this;

            switch (PlatformLongSize)
            {
                case PlatformLongSize.Long32: // cdecl/stdcall branch required
                    if (UseStdcall)
                    {
                        #region Deflate - DeflateInit2, Deflate, DeflateEnd
                        SL32.DeflateInit2 = GetFuncPtr<StdcallL32d.deflateInit2_>(nameof(StdcallL32d.deflateInit2_));
                        SL32.Deflate = GetFuncPtr<StdcallL32d.deflate>(nameof(StdcallL32d.deflate));
                        SL32.DeflateEnd = GetFuncPtr<StdcallL32d.deflateEnd>(nameof(StdcallL32d.deflateEnd));
                        #endregion

                        #region Inflate - InflateInit2, Inflate, InflateEnd
                        SL32.InflateInit2 = GetFuncPtr<StdcallL32d.inflateInit2_>(nameof(StdcallL32d.inflateInit2_));
                        SL32.Inflate = GetFuncPtr<StdcallL32d.inflate>(nameof(StdcallL32d.inflate));
                        SL32.InflateEnd = GetFuncPtr<StdcallL32d.inflateEnd>(nameof(StdcallL32d.inflateEnd));
                        #endregion
                    }
                    else
                    {
                        #region Deflate - DeflateInit2, Deflate, DeflateEnd
                        CL32.DeflateInit2 = GetFuncPtr<CdeclL32d.deflateInit2_>(nameof(CdeclL32d.deflateInit2_));
                        CL32.Deflate = GetFuncPtr<CdeclL32d.deflate>(nameof(CdeclL32d.deflate));
                        CL32.DeflateEnd = GetFuncPtr<CdeclL32d.deflateEnd>(nameof(CdeclL32d.deflateEnd));
                        #endregion

                        #region Inflate - InflateInit2, Inflate, InflateEnd
                        CL32.InflateInit2 = GetFuncPtr<CdeclL32d.inflateInit2_>(nameof(CdeclL32d.inflateInit2_));
                        CL32.Inflate = GetFuncPtr<CdeclL32d.inflate>(nameof(CdeclL32d.inflate));
                        CL32.InflateEnd = GetFuncPtr<CdeclL32d.inflateEnd>(nameof(CdeclL32d.inflateEnd));
                        #endregion
                    }
                    break;
                case PlatformLongSize.Long64: // Calling convention designation ignored
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

            if (UseStdcall)
            {
                #region Checksum - Adler32, Crc32
                Stdcall.Adler32 = GetFuncPtr<StdcallNoLong.adler32>(nameof(StdcallNoLong.adler32));
                Stdcall.Crc32 = GetFuncPtr<StdcallNoLong.crc32>(nameof(StdcallNoLong.crc32));
                #endregion

                #region Version - ZLibVersion
                Stdcall.ZLibVersionPtr = GetFuncPtr<StdcallNoLong.zlibVersion>(nameof(StdcallNoLong.zlibVersion));
                #endregion
            }
            else
            {
                #region Checksum - Adler32, Crc32
                Cdecl.Adler32 = GetFuncPtr<CdeclNoLong.adler32>(nameof(CdeclNoLong.adler32));
                Cdecl.Crc32 = GetFuncPtr<CdeclNoLong.crc32>(nameof(CdeclNoLong.crc32));
                #endregion

                #region Version - ZLibVersion
                Cdecl.ZLibVersionPtr = GetFuncPtr<CdeclNoLong.zlibVersion>(nameof(CdeclNoLong.zlibVersion));
                #endregion
            }
        }

        protected override void ResetFunctions()
        {
            switch (PlatformLongSize)
            {
                case PlatformLongSize.Long32: // cdecl/stdcall branch required
                    if (UseStdcall)
                    {
                        #region Deflate - DeflateInit2, Deflate, DeflateEnd
                        SL32.DeflateInit2 = null;
                        SL32.Deflate = null;
                        SL32.DeflateEnd = GetFuncPtr<StdcallL32d.deflateEnd>(nameof(StdcallL32d.deflateEnd));
                        #endregion

                        #region Inflate - InflateInit2, Inflate, InflateEnd
                        SL32.InflateInit2 = null;
                        SL32.Inflate = null;
                        SL32.InflateEnd = null;
                        #endregion
                    }
                    else
                    {
                        #region Deflate - DeflateInit2, Deflate, DeflateEnd
                        CL32.DeflateInit2 = null;
                        CL32.Deflate = null;
                        CL32.DeflateEnd = null;
                        #endregion

                        #region Inflate - InflateInit2, Inflate, InflateEnd
                        CL32.InflateInit2 = null;
                        CL32.Inflate = null;
                        CL32.InflateEnd = null;
                        #endregion
                    }
                    break;
                case PlatformLongSize.Long64: // Calling convention designation ignored
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

            if (UseStdcall)
            {
                #region Checksum - Adler32, Crc32
                Stdcall.Adler32 = null;
                Stdcall.Crc32 = null;
                #endregion

                #region Version - ZLibVersion
                Stdcall.ZLibVersionPtr = null;
                #endregion
            }
            else
            {
                #region Checksum - Adler32, Crc32
                Cdecl.Adler32 = null;
                Cdecl.Crc32 = null;
                #endregion

                #region Version - ZLibVersion
                Cdecl.ZLibVersionPtr = null;
                #endregion
            }
        }
        #endregion
#else
        #region LoadFunctions, ResetFunctions
        protected override void LoadFunctions()
        {
            if (PlatformLongSize == PlatformLongSize.Long64)
            {
                NativeAbi = new ZLibNativeAbiL64(this);
            }
            else if (PlatformLongSize == PlatformLongSize.Long32)
            {
                if (UseStdcall)
                    NativeAbi = new ZLibNativeAbiStdcallL32(this);
                else
                    NativeAbi = new ZLibNativeAbiCdeclL32(this);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            NativeAbi.LoadFunctions();
        }

        protected override void ResetFunctions()
        {
            NativeAbi.ResetFunctions();
            NativeAbi = null;
        }
        #endregion
#endif

#if !ZLIB_INHERIT_CALL
        #region zlib Function Pointers
        internal class L64d
        {
            public ZLibLoader Lib { get; internal set; }

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate ZLibRet deflateInit2_(
                ZStreamDirectL64 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                int windowBits,
                ZLibMemLevel memLevel,
                ZLibCompStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal deflateInit2_ DeflateInit2;

            internal ZLibRet DeflateInit(ZStreamDirectL64 strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel)
            {
                // cdecl/stdcall detection is irrelevant and ignored on non-x86 architectures.
                string zlibVer = Lib.Cdecl.ZLibVersion();
                return DeflateInit2(strm, level, ZLibCompMethod.Deflated, windowBits, memLevel,
                        ZLibCompStrategy.Default, zlibVer, Marshal.SizeOf<ZStreamDirectL64>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate ZLibRet deflate(
                ZStreamDirectL64 strm,
                ZLibFlush flush);
            internal deflate Deflate;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate ZLibRet deflateEnd(
                ZStreamDirectL64 strm);
            internal deflateEnd DeflateEnd;
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate ZLibRet inflateInit2_(
                ZStreamDirectL64 strm,
                int windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal inflateInit2_ InflateInit2;

            internal ZLibRet InflateInit(ZStreamDirectL64 strm, int windowBits)
            {
                // cdecl/stdcall detection is irrelevant and ignored on non-x86 architectures.
                string zlibVer = Lib.Cdecl.ZLibVersion();
                return InflateInit2(strm, windowBits, zlibVer, Marshal.SizeOf<ZStreamDirectL64>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate ZLibRet inflate(
                ZStreamDirectL64 strm,
                ZLibFlush flush);
            internal inflate Inflate;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate ZLibRet inflateEnd(
                ZStreamDirectL64 strm);
            internal inflateEnd InflateEnd;
            #endregion
        }

        internal class CdeclL32d
        {
            private const CallingConvention CallConv = CallingConvention.Cdecl;
            public ZLibLoader Lib { get; internal set; }

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflateInit2_(
                ZStreamDirectL32 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                int windowBits,
                ZLibMemLevel memLevel,
                ZLibCompStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal deflateInit2_ DeflateInit2;

            internal ZLibRet DeflateInit(ZStreamDirectL32 strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel)
            {
                string zlibVer = Lib.Cdecl.ZLibVersion();
                return DeflateInit2(strm, level, ZLibCompMethod.Deflated, windowBits, memLevel,
                        ZLibCompStrategy.Default, zlibVer, Marshal.SizeOf<ZStreamDirectL32>());
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflate(
                ZStreamDirectL32 strm,
                ZLibFlush flush);
            internal deflate Deflate;

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflateEnd(
                ZStreamDirectL32 strm);
            internal deflateEnd DeflateEnd;
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflateInit2_(
                ZStreamDirectL32 strm,
                int windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal inflateInit2_ InflateInit2;

            internal ZLibRet InflateInit(ZStreamDirectL32 strm, int windowBits)
            {
                string zlibVer = Lib.Cdecl.ZLibVersion();
                return InflateInit2(strm, windowBits, zlibVer, Marshal.SizeOf<ZStreamDirectL32>());
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflate(
                ZStreamDirectL32 strm,
                ZLibFlush flush);
            internal inflate Inflate;

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflateEnd(ZStreamDirectL32 strm);
            internal inflateEnd InflateEnd;
            #endregion
        }

        internal class StdcallL32d
        {
            private const CallingConvention CallConv = CallingConvention.Cdecl;
            public ZLibLoader Lib { get; internal set; }

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflateInit2_(
                ZStreamDirectL32 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                int windowBits,
                ZLibMemLevel memLevel,
                ZLibCompStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal deflateInit2_ DeflateInit2;

            internal ZLibRet DeflateInit(ZStreamDirectL32 strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel)
            {
                string zlibVer = Lib.Stdcall.ZLibVersion();
                return DeflateInit2(strm, level, ZLibCompMethod.Deflated, windowBits, memLevel,
                        ZLibCompStrategy.Default, zlibVer, Marshal.SizeOf<ZStreamDirectL32>());
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflate(
                ZStreamDirectL32 strm,
                ZLibFlush flush);
            internal deflate Deflate;

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflateEnd(
                ZStreamDirectL32 strm);
            internal deflateEnd DeflateEnd;
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflateInit2_(
                ZStreamDirectL32 strm,
                int windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal inflateInit2_ InflateInit2;

            internal ZLibRet InflateInit(ZStreamDirectL32 strm, int windowBits)
            {
                string zlibVer = Lib.Stdcall.ZLibVersion();
                return InflateInit2(strm, windowBits, zlibVer, Marshal.SizeOf<ZStreamDirectL32>());
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflate(
                ZStreamDirectL32 strm,
                ZLibFlush flush);
            internal inflate Inflate;

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflateEnd(ZStreamDirectL32 strm);
            internal inflateEnd InflateEnd;
            #endregion
        }

        internal class StdcallNoLong
        {
            private const CallingConvention CallConv = CallingConvention.Cdecl;

            #region Checksum - Adler32, Crc32
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint adler32(
                uint adler,
                byte* buf,
                uint len);
            internal adler32 Adler32;

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint crc32(
                uint crc,
                byte* buf,
                uint len);
            internal crc32 Crc32;
            #endregion

            #region Version - ZLibVersion
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate IntPtr zlibVersion();
            internal zlibVersion ZLibVersionPtr;
            internal string ZLibVersion() => Marshal.PtrToStringAnsi(ZLibVersionPtr());
            #endregion
        }

        internal class CdeclNoLong
        {
            private const CallingConvention CallConv = CallingConvention.Cdecl;

            #region Checksum - Adler32, Crc32
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint adler32(
                uint adler,
                byte* buf,
                uint len);
            internal adler32 Adler32;

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint crc32(
                uint crc,
                byte* buf,
                uint len);
            internal crc32 Crc32;
            #endregion

            #region Version - ZLibVersion
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate IntPtr zlibVersion();
            internal zlibVersion ZLibVersionPtr;
            internal string ZLibVersion() => Marshal.PtrToStringAnsi(ZLibVersionPtr());
            #endregion
        }
        #endregion
#endif
    }
}
