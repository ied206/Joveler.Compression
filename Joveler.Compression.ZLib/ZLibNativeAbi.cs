using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Joveler.Compression.ZLib
{
    internal partial class ZLibLoader
    {
#if ZLIB_INHERIT_CALL
        #region NativeAbi Base
        internal abstract class ZLibNativeAbi
        {
            protected ZLibLoader Lib { get; }

            public ZLibNativeAbi(ZLibLoader lib)
            {
                Lib = lib;
            }

            #region Load and Reset Functions
            public abstract void LoadFunctions();
            public abstract void ResetFunctions();
            #endregion

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            public abstract ZLibRet DeflateInit(ZStreamBase strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel);
            public abstract ZLibRet Deflate(ZStreamBase strm, ZLibFlush flush);
            public abstract ZLibRet DeflateEnd(ZStreamBase strm);
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            public abstract ZLibRet InflateInit(ZStreamBase strm, int windowBits);
            public abstract ZLibRet Inflate(ZStreamBase strm, ZLibFlush flush);
            public abstract ZLibRet InflateEnd(ZStreamBase strm);
            #endregion

            #region Checksum - Adler32, Crc32
            public abstract unsafe uint Adler32(uint adler, byte* buf, uint len);
            public abstract unsafe uint Crc32(uint crc, byte* buf, uint len);
            #endregion

            #region Version - ZLibVersion
            public abstract string ZLibVersion();
            #endregion
        }
        #endregion

        #region NativeAbi Cdecl Base
        internal abstract class ZLibNativeAbiCdecl : ZLibNativeAbi
        {
            protected const CallingConvention CallConv = CallingConvention.Cdecl;

            public ZLibNativeAbiCdecl(ZLibLoader lib) : base(lib)
            {
            }

            #region Load and Reset Functions
            public override void LoadFunctions()
            {
                Adler32Ptr = Lib.GetFuncPtr<adler32>(nameof(adler32));
                Crc32Ptr = Lib.GetFuncPtr<crc32>(nameof(crc32));
                ZLibVersionPtr = Lib.GetFuncPtr<zlibVersion>(nameof(zlibVersion));
            }

            public override void ResetFunctions()
            {
                Adler32Ptr = null;
                Crc32Ptr = null;
                ZLibVersionPtr = null;
            }
            #endregion

            #region Checksum - Adler32, Crc32
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint adler32(
                uint adler,
                byte* buf,
                uint len);
            internal adler32 Adler32Ptr;
            public override unsafe uint Adler32(uint adler, byte* buf, uint len)
            {
                return Adler32Ptr(adler, buf, len);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint crc32(
                uint crc,
                byte* buf,
                uint len);
            internal crc32 Crc32Ptr;
            public override unsafe uint Crc32(uint crc, byte* buf, uint len)
            {
                return Crc32Ptr(crc, buf, len);
            }
            #endregion

            #region Version - ZLibVersion
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate IntPtr zlibVersion();
            internal zlibVersion ZLibVersionPtr;
            public override string ZLibVersion() => Marshal.PtrToStringAnsi(ZLibVersionPtr());
            #endregion
        }
        #endregion

        #region NativeAbi Stdcall Base
        internal abstract class ZLibNativeAbiStdcall : ZLibNativeAbi
        {
            protected const CallingConvention CallConv = CallingConvention.StdCall;

            public ZLibNativeAbiStdcall(ZLibLoader lib) : base(lib)
            {
            }

            #region Load and Reset Functions
            public override void LoadFunctions()
            {
                Adler32Ptr = Lib.GetFuncPtr<adler32>(nameof(adler32));
                Crc32Ptr = Lib.GetFuncPtr<crc32>(nameof(crc32));
                ZLibVersionPtr = Lib.GetFuncPtr<zlibVersion>(nameof(zlibVersion));
            }

            public override void ResetFunctions()
            {
                Adler32Ptr = null;
                Crc32Ptr = null;
                ZLibVersionPtr = null;
            }
            #endregion

            #region Checksum - Adler32, Crc32
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint adler32(
                uint adler,
                byte* buf,
                uint len);
            internal adler32 Adler32Ptr;
            public override unsafe uint Adler32(uint adler, byte* buf, uint len)
            {
                return Adler32Ptr(adler, buf, len);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint crc32(
                uint crc,
                byte* buf,
                uint len);
            internal crc32 Crc32Ptr;
            public override unsafe uint Crc32(uint crc, byte* buf, uint len)
            {
                return Crc32Ptr(crc, buf, len);
            }
            #endregion

            #region Version - ZLibVersion
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate IntPtr zlibVersion();
            internal zlibVersion ZLibVersionPtr;
            public override string ZLibVersion() => Marshal.PtrToStringAnsi(ZLibVersionPtr());
            #endregion
        }
        #endregion

        #region NativeAbi Long64 (x64, arm64)
        internal sealed class ZLibNativeAbiL64 : ZLibNativeAbiCdecl
        {
            public ZLibNativeAbiL64(ZLibLoader lib) : base(lib)
            {
            }

            #region Load and Reset Functions
            public override void LoadFunctions()
            {
                DeflateInit2Ptr = Lib.GetFuncPtr<deflateInit2_>(nameof(deflateInit2_));
                DeflatePtr = Lib.GetFuncPtr<deflate>(nameof(deflate));
                DeflateEndPtr = Lib.GetFuncPtr<deflateEnd>(nameof(deflateEnd));

                InflateInit2Ptr = Lib.GetFuncPtr<inflateInit2_>(nameof(inflateInit2_));
                InflatePtr = Lib.GetFuncPtr<inflate>(nameof(inflate));
                InflateEndPtr = Lib.GetFuncPtr<inflateEnd>(nameof(inflateEnd));

                base.LoadFunctions();
            }

            public override void ResetFunctions()
            {
                DeflateInit2Ptr = null;
                DeflatePtr = null;
                DeflateEndPtr = null;

                InflateInit2Ptr = null;
                InflatePtr = null;
                InflateEndPtr = null;

                base.ResetFunctions();
            }
            #endregion

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflateInit2_(
                ZStreamL64 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                int windowBits,
                ZLibMemLevel memLevel,
                ZLibCompStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal deflateInit2_ DeflateInit2Ptr;
            public override ZLibRet DeflateInit(ZStreamBase strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel)
            {
                // cdecl/stdcall detection is irrelevant and ignored on non-x86 architectures.
                string zlibVer = ZLibVersion();
                return DeflateInit2Ptr((ZStreamL64)strm, level, ZLibCompMethod.Deflated, windowBits, memLevel,
                        ZLibCompStrategy.Default, zlibVer, Marshal.SizeOf<ZStreamL64>());
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflate(
                ZStreamL64 strm,
                ZLibFlush flush);
            internal deflate DeflatePtr;
            public override ZLibRet Deflate(ZStreamBase strm, ZLibFlush flush)
            {
                return DeflatePtr((ZStreamL64)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflateEnd(
                ZStreamL64 strm);
            internal deflateEnd DeflateEndPtr;
            public override ZLibRet DeflateEnd(ZStreamBase strm)
            {
                return DeflateEndPtr((ZStreamL64)strm);
            }
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflateInit2_(
                ZStreamL64 strm,
                int windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal inflateInit2_ InflateInit2Ptr;
            public override ZLibRet InflateInit(ZStreamBase strm, int windowBits)
            {
                // cdecl/stdcall detection is irrelevant and ignored on non-x86 architectures.
                string zlibVer = ZLibVersion();
                return InflateInit2Ptr((ZStreamL64)strm, windowBits, zlibVer, Marshal.SizeOf<ZStreamL64>());
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflate(
                ZStreamL64 strm,
                ZLibFlush flush);
            internal inflate InflatePtr;
            public override ZLibRet Inflate(ZStreamBase strm, ZLibFlush flush)
            {
                return InflatePtr((ZStreamL64)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflateEnd(
                ZStreamL64 strm);
            internal inflateEnd InflateEndPtr;
            public override ZLibRet InflateEnd(ZStreamBase strm)
            {
                return InflateEndPtr((ZStreamL64)strm);
            }
            #endregion
        }
        #endregion

        #region NativeAbi Long32 - Cdecl (arm, Windows x86 cdecl)
        internal sealed class ZLibNativeAbiCdeclL32 : ZLibNativeAbiCdecl
        {
            public ZLibNativeAbiCdeclL32(ZLibLoader lib) : base(lib)
            {
            }

            #region Load and Reset Functions
            public override void LoadFunctions()
            {
                DeflateInit2Ptr = Lib.GetFuncPtr<deflateInit2_>(nameof(deflateInit2_));
                DeflatePtr = Lib.GetFuncPtr<deflate>(nameof(deflate));
                DeflateEndPtr = Lib.GetFuncPtr<deflateEnd>(nameof(deflateEnd));

                InflateInit2Ptr = Lib.GetFuncPtr<inflateInit2_>(nameof(inflateInit2_));
                InflatePtr = Lib.GetFuncPtr<inflate>(nameof(inflate));
                InflateEndPtr = Lib.GetFuncPtr<inflateEnd>(nameof(inflateEnd));

                base.LoadFunctions();
            }

            public override void ResetFunctions()
            {
                DeflateInit2Ptr = null;
                DeflatePtr = null;
                DeflateEndPtr = null;

                InflateInit2Ptr = null;
                InflatePtr = null;
                InflateEndPtr = null;

                base.ResetFunctions();
            }
            #endregion

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflateInit2_(
                ZStreamL32 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                int windowBits,
                ZLibMemLevel memLevel,
                ZLibCompStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal deflateInit2_ DeflateInit2Ptr;
            public override ZLibRet DeflateInit(ZStreamBase strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel)
            {
                // cdecl/stdcall detection is irrelevant and ignored on non-x86 architectures.
                string zlibVer = ZLibVersion();
                return DeflateInit2Ptr((ZStreamL32)strm, level, ZLibCompMethod.Deflated, windowBits, memLevel,
                        ZLibCompStrategy.Default, zlibVer, Marshal.SizeOf<ZStreamL32>());
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflate(
                ZStreamL32 strm,
                ZLibFlush flush);
            internal deflate DeflatePtr;
            public override ZLibRet Deflate(ZStreamBase strm, ZLibFlush flush)
            {
                return DeflatePtr((ZStreamL32)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflateEnd(
                ZStreamL32 strm);
            internal deflateEnd DeflateEndPtr;
            public override ZLibRet DeflateEnd(ZStreamBase strm)
            {
                return DeflateEndPtr((ZStreamL32)strm);
            }
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflateInit2_(
                ZStreamL32 strm,
                int windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal inflateInit2_ InflateInit2Ptr;
            public override ZLibRet InflateInit(ZStreamBase strm, int windowBits)
            {
                // cdecl/stdcall detection is irrelevant and ignored on non-x86 architectures.
                string zlibVer = ZLibVersion();
                return InflateInit2Ptr((ZStreamL32)strm, windowBits, zlibVer, Marshal.SizeOf<ZStreamL32>());
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflate(
                ZStreamL32 strm,
                ZLibFlush flush);
            internal inflate InflatePtr;
            public override ZLibRet Inflate(ZStreamBase strm, ZLibFlush flush)
            {
                return InflatePtr((ZStreamL32)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflateEnd(
                ZStreamL32 strm);
            internal inflateEnd InflateEndPtr;
            public override ZLibRet InflateEnd(ZStreamBase strm)
            {
                return InflateEndPtr((ZStreamL32)strm);
            }
            #endregion
        }
        #endregion

        #region NativeAbi Long32 - Stdcall (Windows x86 stdcall)
        internal sealed class ZLibNativeAbiStdcallL32 : ZLibNativeAbiStdcall
        {
            public ZLibNativeAbiStdcallL32(ZLibLoader lib) : base(lib)
            {
            }

            #region Load and Reset Functions
            public override void LoadFunctions()
            {
                DeflateInit2Ptr = Lib.GetFuncPtr<deflateInit2_>(nameof(deflateInit2_));
                DeflatePtr = Lib.GetFuncPtr<deflate>(nameof(deflate));
                DeflateEndPtr = Lib.GetFuncPtr<deflateEnd>(nameof(deflateEnd));

                InflateInit2Ptr = Lib.GetFuncPtr<inflateInit2_>(nameof(inflateInit2_));
                InflatePtr = Lib.GetFuncPtr<inflate>(nameof(inflate));
                InflateEndPtr = Lib.GetFuncPtr<inflateEnd>(nameof(inflateEnd));

                base.LoadFunctions();
            }

            public override void ResetFunctions()
            {
                DeflateInit2Ptr = null;
                DeflatePtr = null;
                DeflateEndPtr = null;

                InflateInit2Ptr = null;
                InflatePtr = null;
                InflateEndPtr = null;

                base.ResetFunctions();
            }
            #endregion

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflateInit2_(
                ZStreamL32 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                int windowBits,
                ZLibMemLevel memLevel,
                ZLibCompStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal deflateInit2_ DeflateInit2Ptr;
            public override ZLibRet DeflateInit(ZStreamBase strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel)
            {
                // cdecl/stdcall detection is irrelevant and ignored on non-x86 architectures.
                string zlibVer = ZLibVersion();
                return DeflateInit2Ptr((ZStreamL32)strm, level, ZLibCompMethod.Deflated, windowBits, memLevel,
                        ZLibCompStrategy.Default, zlibVer, Marshal.SizeOf<ZStreamL32>());
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflate(
                ZStreamL32 strm,
                ZLibFlush flush);
            internal deflate DeflatePtr;
            public override ZLibRet Deflate(ZStreamBase strm, ZLibFlush flush)
            {
                return DeflatePtr((ZStreamL32)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet deflateEnd(
                ZStreamL32 strm);
            internal deflateEnd DeflateEndPtr;
            public override ZLibRet DeflateEnd(ZStreamBase strm)
            {
                return DeflateEndPtr((ZStreamL32)strm);
            }
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflateInit2_(
                ZStreamL32 strm,
                int windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal inflateInit2_ InflateInit2Ptr;
            public override ZLibRet InflateInit(ZStreamBase strm, int windowBits)
            {
                // cdecl/stdcall detection is irrelevant and ignored on non-x86 architectures.
                string zlibVer = ZLibVersion();
                return InflateInit2Ptr((ZStreamL32)strm, windowBits, zlibVer, Marshal.SizeOf<ZStreamL32>());
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflate(
                ZStreamL32 strm,
                ZLibFlush flush);
            internal inflate InflatePtr;
            public override ZLibRet Inflate(ZStreamBase strm, ZLibFlush flush)
            {
                return InflatePtr((ZStreamL32)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet inflateEnd(
                ZStreamL32 strm);
            internal inflateEnd InflateEndPtr;
            public override ZLibRet InflateEnd(ZStreamBase strm)
            {
                return InflateEndPtr((ZStreamL32)strm);
            }
            #endregion
        }
        #endregion
#endif
    }
}
