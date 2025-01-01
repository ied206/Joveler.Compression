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

using Joveler.DynLoader;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Joveler.Compression.ZLib
{
    internal class ZLibLoader : DynLoaderBase
    {
        #region Constructor
        public ZLibLoader() : base()
        {
        }
        #endregion

        #region Native ABI configurations
        internal ZLibNativeAbi NativeAbi;

        /// <summary>
        /// Does the loaded native library use stdcall calling convention?
        /// </summary>
        internal bool UseStdcall { get; private set; } = false;
        /// <summary>
        /// Does the loaded native library was built to have zlib-ng 'modern' ABI?
        /// </summary>
        internal bool UseZLibNgModernAbi { get; private set; } = false;
        #endregion

        #region (override) DefaultLibFileName
        protected override string DefaultLibFileName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "libz.so.1";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "libz.dylib";
                throw new PlatformNotSupportedException();
            }
        }
        #endregion

        #region HandleLoadData
        protected override void HandleLoadData(object data)
        {
            if (data is not ZLibInitOptions initOpts)
                return;

            // Use stdcall only if `IsWindowsStdcall` is active on Windows x86 platform.
            UseStdcall = initOpts.IsWindowsStdcall &&
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                RuntimeInformation.ProcessArchitecture == Architecture.X86;

            UseZLibNgModernAbi = initOpts.IsZLibNgModernAbi;
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        protected override void LoadFunctions()
        {
            if (UseZLibNgModernAbi)
            {
                if (PlatformLongSize == PlatformLongSize.Long64)
                {
                    NativeAbi = new ZLibNgNativeAbiL64(this);
                }
                else if (PlatformLongSize == PlatformLongSize.Long32)
                {
                    if (UseStdcall)
                        NativeAbi = new ZLibNgNativeAbiStdcallL32(this);
                    else
                        NativeAbi = new ZLibNgNativeAbiCdeclL32(this);
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }
            }
            else
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
            }

            NativeAbi.LoadFunctions();

            ValidateNativeAbi();
        }

        protected override void ResetFunctions()
        {
            NativeAbi.ResetFunctions();
            NativeAbi = null;
        }
        #endregion

        #region Create a new ZStream object
        internal ZStreamBase CreateZStream()
        {
            if (UseZLibNgModernAbi)
            {
                return PlatformLongSize switch
                {
                    PlatformLongSize.Long32 => new ZNgStreamL32(),
                    PlatformLongSize.Long64 => new ZNgStreamL64(),
                    _ => throw new PlatformNotSupportedException(),
                };
            }
            else
            {
                return PlatformLongSize switch
                {
                    PlatformLongSize.Long32 => new ZStreamL32(),
                    PlatformLongSize.Long64 => new ZStreamL64(),
                    _ => throw new PlatformNotSupportedException(),
                };
            }
        }
        #endregion

        #region Validate zlib compile-time options
        internal void ValidateNativeAbi()
        {
            uint rawFlags = NativeAbi.ZLibCompileFlags();
            ZLibCompileFlags flags = new ZLibCompileFlags(rawFlags);

            // [*] Check type sizes
            // Check C-type uint byte size
            if (flags.CUIntSize != 4)
                throw new ZLibNativeAbiException(nameof(ZLibCompileFlags.CUIntSize));

            // Check C-type ulong byte size
            bool ulongValid = PlatformLongSize switch
            {
                PlatformLongSize.Long32 => flags.CULongSize == 4,
                PlatformLongSize.Long64 => flags.CULongSize == 8,
                _ => flags.CULongSize == 0,
            };
            if (ulongValid == false)
                throw new ZLibNativeAbiException(nameof(ZLibCompileFlags.CULongSize));

            // Check C-type pointer size
            if (flags.PtrSize != IntPtr.Size)
                throw new ZLibNativeAbiException(nameof(ZLibCompileFlags.PtrSize));

            // [*] Check compiler options (Windows x86 only)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.ProcessArchitecture == Architecture.X86)
            {
                if (flags.IsWinApi != UseStdcall)
                    throw new ZLibNativeAbiException(nameof(ZLibCompileFlags.IsWinApi));
            }

            // [*] Check library functionality
            // Required for GZipStream implementation
            if (flags.NoGZip)
                throw new ZLibNativeAbiException(nameof(ZLibCompileFlags.NoGZip));

            // [*] Check operation variations
            // Required for compression level
            if (flags.FastestDeflateOnly)
                throw new ZLibNativeAbiException(nameof(ZLibCompileFlags.FastestDeflateOnly));
        }
        #endregion

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

            #region Advanced - DeflateSetDictionary, DeflateReset, DeflateParams
            public abstract unsafe ZLibRet DeflateSetDictionary(ZStreamBase strm, byte* dictionary, uint dictLength);
            public abstract ZLibRet DeflateReset(ZStreamBase strm);
            public abstract ZLibRet DeflateParams(ZStreamBase strm, int level, int strategy);
            #endregion

            #region Checksum - Adler32, Crc32
            public abstract unsafe uint Adler32(uint adler, byte* buf, uint len);
            public abstract unsafe uint Crc32(uint crc, byte* buf, uint len);
            public abstract unsafe uint Adler32Combine(uint adler1, uint adler2, int len2);
            public abstract unsafe uint Crc32Combine(uint crc1, uint crc2, int len2);
            #endregion

            #region Version - ZLibVersion
            public abstract string ZLibVersion();
            #endregion

            #region CompileFlags
            public abstract uint ZLibCompileFlags();
            #endregion
        }
        #endregion

        #region zlib: NativeAbi - Cdecl Base
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

        #region zlib: NativeAbi - Stdcall Base
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

        #region zlib: NativeAbi Long64 (x64, arm64)
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

                DeflateSetDictionaryPtr = Lib.GetFuncPtr<deflateSetDictionary>(nameof(deflateSetDictionary));
                DeflateResetPtr = Lib.GetFuncPtr<deflateReset>(nameof(deflateReset));
                DeflateParamsPtr = Lib.GetFuncPtr<deflateParams>(nameof(deflateParams));

                Adler32CombinePtr = Lib.GetFuncPtr<adler32_combine>(nameof(adler32_combine));
                Crc32CombinePtr = Lib.GetFuncPtr<crc32_combine>(nameof(crc32_combine));

                ZLibCompileFlagsPtr = Lib.GetFuncPtr<zlibCompileFlags>(nameof(zlibCompileFlags));

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

                DeflateSetDictionaryPtr = null;
                DeflateResetPtr = null;
                DeflateParamsPtr = null;

                Adler32CombinePtr = null;
                Crc32CombinePtr = null;

                ZLibCompileFlagsPtr = null;

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

            #region Advanced - DeflateSetDictionary, DeflateReset, DeflateParams
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet deflateSetDictionary(
                ZStreamL64 strm,
                byte* dictionary,
                uint dictLength);
            internal unsafe deflateSetDictionary DeflateSetDictionaryPtr;
            public override unsafe ZLibRet DeflateSetDictionary(ZStreamBase strm, byte* dictionary, uint dictLength)
            {
                return DeflateSetDictionaryPtr((ZStreamL64)strm, dictionary, dictLength);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet deflateReset(
                ZStreamL64 strm);
            internal unsafe deflateReset DeflateResetPtr;
            public override ZLibRet DeflateReset(ZStreamBase strm)
            {
                return DeflateResetPtr((ZStreamL64)strm);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet deflateParams(
                ZStreamL64 strm,
                int level,
                int strategy);
            internal unsafe deflateParams DeflateParamsPtr;
            public override ZLibRet DeflateParams(ZStreamBase strm, int level, int strategy)
            {
                return DeflateParamsPtr((ZStreamL64)strm, level, strategy);
            }
            #endregion

            #region Checksum - Adler32, Crc32 (Combine)
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint adler32_combine(
               uint adler1,
               uint adler2,
               long len2);
            internal adler32_combine Adler32CombinePtr;
            public override uint Adler32Combine(uint adler1, uint adler2, int len2)
            {
                return Adler32CombinePtr(adler1, adler2, len2);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint crc32_combine(
                uint crc1,
                uint crc2,
                long len2);
            internal crc32_combine Crc32CombinePtr;
            public override uint Crc32Combine(uint crc1, uint crc2, int len2)
            {
                return Crc32CombinePtr(crc1, crc2, len2);
            }
            #endregion

            #region ZLibCompileFlags
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ulong zlibCompileFlags();
            internal zlibCompileFlags ZLibCompileFlagsPtr;
            public override uint ZLibCompileFlags()
            {
                return (uint)ZLibCompileFlagsPtr();
            }
            #endregion
        }
        #endregion

        #region zlib: NativeAbi Long32 - Cdecl (arm, POSIX Windows x86 cdecl)
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

                DeflateSetDictionaryPtr = Lib.GetFuncPtr<deflateSetDictionary>(nameof(deflateSetDictionary));
                DeflateResetPtr = Lib.GetFuncPtr<deflateReset>(nameof(deflateReset));
                DeflateParamsPtr = Lib.GetFuncPtr<deflateParams>(nameof(deflateParams));

                Adler32CombinePtr = Lib.GetFuncPtr<adler32_combine>(nameof(adler32_combine));
                Crc32CombinePtr = Lib.GetFuncPtr<crc32_combine>(nameof(crc32_combine));

                ZLibCompileFlagsPtr = Lib.GetFuncPtr<zlibCompileFlags>(nameof(zlibCompileFlags));

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

                DeflateSetDictionaryPtr = null;
                DeflateResetPtr = null;
                DeflateParamsPtr = null;

                Adler32CombinePtr = null;
                Crc32CombinePtr = null;

                ZLibCompileFlagsPtr = null;

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

            #region Advanced - DeflateSetDictionary, DeflateReset, DeflateParams
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet deflateSetDictionary(
                ZStreamL32 strm,
                byte* dictionary,
                uint dictLength);
            internal unsafe deflateSetDictionary DeflateSetDictionaryPtr;
            public override unsafe ZLibRet DeflateSetDictionary(ZStreamBase strm, byte* dictionary, uint dictLength)
            {
                return DeflateSetDictionaryPtr((ZStreamL32)strm, dictionary, dictLength);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet deflateReset(
                ZStreamL32 strm);
            internal unsafe deflateReset DeflateResetPtr;
            public override ZLibRet DeflateReset(ZStreamBase strm)
            {
                return DeflateResetPtr((ZStreamL32)strm);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet deflateParams(
                ZStreamL32 strm,
                int level,
                int strategy);
            internal unsafe deflateParams DeflateParamsPtr;
            public override ZLibRet DeflateParams(ZStreamBase strm, int level, int strategy)
            {
                return DeflateParamsPtr((ZStreamL32)strm, level, strategy);
            }
            #endregion

            #region Checksum - Adler32, Crc32 (Combine)
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint adler32_combine(
                uint adler1,
                uint adler2,
                int len2);
            internal adler32_combine Adler32CombinePtr;
            public override uint Adler32Combine(uint adler1, uint adler2, int len2)
            {
                return Adler32CombinePtr(adler1, adler2, len2);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint crc32_combine(
                uint crc1,
                uint crc2,
                int len2);
            internal crc32_combine Crc32CombinePtr;
            public override uint Crc32Combine(uint crc1, uint crc2, int len2)
            {
                return Crc32CombinePtr(crc1, crc2, len2);
            }
            #endregion

            #region ZLibCompileFlags
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate uint zlibCompileFlags();
            internal zlibCompileFlags ZLibCompileFlagsPtr;
            public override uint ZLibCompileFlags()
            {
                return ZLibCompileFlagsPtr();
            }
            #endregion
        }
        #endregion

        #region zlib: NativeAbi Long32 - Stdcall (Windows x86 stdcall)
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

                DeflateSetDictionaryPtr = Lib.GetFuncPtr<deflateSetDictionary>(nameof(deflateSetDictionary));
                DeflateResetPtr = Lib.GetFuncPtr<deflateReset>(nameof(deflateReset));
                DeflateParamsPtr = Lib.GetFuncPtr<deflateParams>(nameof(deflateParams));

                Adler32CombinePtr = Lib.GetFuncPtr<adler32_combine>(nameof(adler32_combine));
                Crc32CombinePtr = Lib.GetFuncPtr<crc32_combine>(nameof(crc32_combine));

                ZLibCompileFlagsPtr = Lib.GetFuncPtr<zlibCompileFlags>(nameof(zlibCompileFlags));

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

                DeflateSetDictionaryPtr = null;
                DeflateResetPtr = null;
                DeflateParamsPtr = null;

                Adler32CombinePtr = null;
                Crc32CombinePtr = null;

                ZLibCompileFlagsPtr = null;

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

            #region Advanced - DeflateSetDictionary, DeflateReset, DeflateParams
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet deflateSetDictionary(
                ZStreamL32 strm,
                byte* dictionary,
                uint dictLength);
            internal unsafe deflateSetDictionary DeflateSetDictionaryPtr;
            public override unsafe ZLibRet DeflateSetDictionary(ZStreamBase strm, byte* dictionary, uint dictLength)
            {
                return DeflateSetDictionaryPtr((ZStreamL32)strm, dictionary, dictLength);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet deflateReset(
                ZStreamL32 strm);
            internal unsafe deflateReset DeflateResetPtr;
            public override ZLibRet DeflateReset(ZStreamBase strm)
            {
                return DeflateResetPtr((ZStreamL32)strm);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet deflateParams(
                ZStreamL32 strm,
                int level,
                int strategy);
            internal unsafe deflateParams DeflateParamsPtr;
            public override ZLibRet DeflateParams(ZStreamBase strm, int level, int strategy)
            {
                return DeflateParamsPtr((ZStreamL32)strm, level, strategy);
            }
            #endregion

            #region Checksum - Adler32, Crc32 (Combine)
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint adler32_combine(
                uint adler1,
                uint adler2,
                int len2);
            internal adler32_combine Adler32CombinePtr;
            public override uint Adler32Combine(uint adler1, uint adler2, int len2)
            {
                return Adler32CombinePtr(adler1, adler2, len2);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint crc32_combine(
                uint crc1,
                uint crc2,
                int len2);
            internal crc32_combine Crc32CombinePtr;
            public override uint Crc32Combine(uint crc1, uint crc2, int len2)
            {
                return Crc32CombinePtr(crc1, crc2, len2);
            }
            #endregion

            #region ZLibCompileFlags
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate uint zlibCompileFlags();
            internal zlibCompileFlags ZLibCompileFlagsPtr;
            public override uint ZLibCompileFlags()
            {
                return ZLibCompileFlagsPtr();
            }
            #endregion
        }
        #endregion

        #region zlib-ng: NativeAbi - Cdecl Base
        internal abstract class ZLibNgNativeAbiCdecl : ZLibNativeAbi
        {
            protected const CallingConvention CallConv = CallingConvention.Cdecl;

            public ZLibNgNativeAbiCdecl(ZLibLoader lib) : base(lib)
            {
            }

            #region Load and Reset Functions
            public override void LoadFunctions()
            {
                Adler32Ptr = Lib.GetFuncPtr<zng_adler32>(nameof(zng_adler32));
                Crc32Ptr = Lib.GetFuncPtr<zng_crc32>(nameof(zng_crc32));
                Adler32CombinePtr = Lib.GetFuncPtr<zng_adler32_combine>(nameof(zng_adler32_combine));
                Crc32CombinePtr = Lib.GetFuncPtr<zng_crc32_combine>(nameof(zng_crc32_combine));
                ZLibNgVersionPtr = Lib.GetFuncPtr<zlibng_version>(nameof(zlibng_version));
            }

            public override void ResetFunctions()
            {
                Adler32Ptr = null;
                Crc32Ptr = null;
                Adler32CombinePtr = null;
                Crc32CombinePtr = null;
                ZLibNgVersionPtr = null;
            }
            #endregion

            #region Checksum - Adler32, Crc32
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint zng_adler32(
                uint adler,
                byte* buf,
                uint len);
            internal zng_adler32 Adler32Ptr;
            public override unsafe uint Adler32(uint adler, byte* buf, uint len)
            {
                return Adler32Ptr(adler, buf, len);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint zng_crc32(
                uint crc,
                byte* buf,
                uint len);
            internal zng_crc32 Crc32Ptr;
            public override unsafe uint Crc32(uint crc, byte* buf, uint len)
            {
                return Crc32Ptr(crc, buf, len);
            }
            #endregion

            #region Checksum - Adler32, Crc32 (Combine)
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint zng_adler32_combine(
               uint adler1,
               uint adler2,
               long len2);
            internal zng_adler32_combine Adler32CombinePtr;
            public override uint Adler32Combine(uint adler1, uint adler2, int len2)
            {
                return Adler32CombinePtr(adler1, adler2, len2);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint zng_crc32_combine(
                uint crc1,
                uint crc2,
                long len2);
            internal zng_crc32_combine Crc32CombinePtr;
            public override uint Crc32Combine(uint crc1, uint crc2, int len2)
            {
                return Crc32CombinePtr(crc1, crc2, len2);
            }
            #endregion

            #region Version - ZLibVersion
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate IntPtr zlibng_version();
            internal zlibng_version ZLibNgVersionPtr;
            public override string ZLibVersion() => Marshal.PtrToStringAnsi(ZLibNgVersionPtr());
            #endregion
        }
        #endregion

        #region zlib-ng: NativeAbi - Stdcall Base
        internal abstract class ZLibNgNativeAbiStdcall : ZLibNativeAbi
        {
            protected const CallingConvention CallConv = CallingConvention.StdCall;

            public ZLibNgNativeAbiStdcall(ZLibLoader lib) : base(lib)
            {
            }

            #region Load and Reset Functions
            public override void LoadFunctions()
            {
                Adler32Ptr = Lib.GetFuncPtr<zng_adler32>(nameof(zng_adler32));
                Crc32Ptr = Lib.GetFuncPtr<zng_crc32>(nameof(zng_crc32));
                Adler32CombinePtr = Lib.GetFuncPtr<zng_adler32_combine>(nameof(zng_adler32_combine));
                Crc32CombinePtr = Lib.GetFuncPtr<zng_crc32_combine>(nameof(zng_crc32_combine));
                ZLibNgVersionPtr = Lib.GetFuncPtr<zlibng_version>(nameof(zlibng_version));
            }

            public override void ResetFunctions()
            {
                Adler32Ptr = null;
                Crc32Ptr = null;
                Adler32CombinePtr = null;
                Crc32CombinePtr = null;
                ZLibNgVersionPtr = null;
            }
            #endregion

            #region Checksum - Adler32, Crc32
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint zng_adler32(
                uint adler,
                byte* buf,
                uint len);
            internal zng_adler32 Adler32Ptr;
            public override unsafe uint Adler32(uint adler, byte* buf, uint len)
            {
                return Adler32Ptr(adler, buf, len);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint zng_crc32(
                uint crc,
                byte* buf,
                uint len);
            internal zng_crc32 Crc32Ptr;
            public override unsafe uint Crc32(uint crc, byte* buf, uint len)
            {
                return Crc32Ptr(crc, buf, len);
            }
            #endregion

            #region Checksum - Adler32, Crc32 (Combine)
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint zng_adler32_combine(
               uint adler1,
               uint adler2,
               long len2);
            internal zng_adler32_combine Adler32CombinePtr;
            public override uint Adler32Combine(uint adler1, uint adler2, int len2)
            {
                return Adler32CombinePtr(adler1, adler2, len2);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate uint zng_crc32_combine(
                uint crc1,
                uint crc2,
                long len2);
            internal zng_crc32_combine Crc32CombinePtr;
            public override uint Crc32Combine(uint crc1, uint crc2, int len2)
            {
                return Crc32CombinePtr(crc1, crc2, len2);
            }
            #endregion

            #region Version - ZLibVersion
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate IntPtr zlibng_version();
            internal zlibng_version ZLibNgVersionPtr;
            public override string ZLibVersion() => Marshal.PtrToStringAnsi(ZLibNgVersionPtr());
            #endregion
        }
        #endregion

        #region zlib-ng: NativeAbi Long64 (x64, arm64)
        internal sealed class ZLibNgNativeAbiL64 : ZLibNgNativeAbiCdecl
        {
            public ZLibNgNativeAbiL64(ZLibLoader lib) : base(lib)
            {
            }

            #region Load and Reset Functions
            public override void LoadFunctions()
            {
                DeflateInit2Ptr = Lib.GetFuncPtr<zng_deflateInit2>(nameof(zng_deflateInit2));
                DeflatePtr = Lib.GetFuncPtr<zng_deflate>(nameof(zng_deflate));
                DeflateEndPtr = Lib.GetFuncPtr<zng_deflateEnd>(nameof(zng_deflateEnd));

                InflateInit2Ptr = Lib.GetFuncPtr<zng_inflateInit2>(nameof(zng_inflateInit2));
                InflatePtr = Lib.GetFuncPtr<zng_inflate>(nameof(zng_inflate));
                InflateEndPtr = Lib.GetFuncPtr<zng_inflateEnd>(nameof(zng_inflateEnd));

                DeflateSetDictionaryPtr = Lib.GetFuncPtr<zng_deflateSetDictionary>(nameof(zng_deflateSetDictionary));
                DeflateResetPtr = Lib.GetFuncPtr<zng_deflateReset>(nameof(zng_deflateReset));
                DeflateParamsPtr = Lib.GetFuncPtr<zng_deflateParams>(nameof(zng_deflateParams));

                ZLibCompileFlagsPtr = Lib.GetFuncPtr<zng_zlibCompileFlags>(nameof(zng_zlibCompileFlags));

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

                DeflateSetDictionaryPtr = null;
                DeflateResetPtr = null;
                DeflateParamsPtr = null;

                ZLibCompileFlagsPtr = null;

                base.ResetFunctions();
            }
            #endregion

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_deflateInit2(
                ZNgStreamL64 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                int windowBits,
                ZLibMemLevel memLevel,
                ZLibCompStrategy strategy);
            internal zng_deflateInit2 DeflateInit2Ptr;
            public override ZLibRet DeflateInit(ZStreamBase strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel)
            {
                return DeflateInit2Ptr((ZNgStreamL64)strm, level, ZLibCompMethod.Deflated, windowBits, memLevel, ZLibCompStrategy.Default);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_deflate(
                ZNgStreamL64 strm,
                ZLibFlush flush);
            internal zng_deflate DeflatePtr;
            public override ZLibRet Deflate(ZStreamBase strm, ZLibFlush flush)
            {
                return DeflatePtr((ZNgStreamL64)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_deflateEnd(
                ZNgStreamL64 strm);
            internal zng_deflateEnd DeflateEndPtr;
            public override ZLibRet DeflateEnd(ZStreamBase strm)
            {
                return DeflateEndPtr((ZNgStreamL64)strm);
            }
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_inflateInit2(
                ZNgStreamL64 strm,
                int windowBits);
            internal zng_inflateInit2 InflateInit2Ptr;
            public override ZLibRet InflateInit(ZStreamBase strm, int windowBits)
            {
                string zlibVer = ZLibVersion();
                return InflateInit2Ptr((ZNgStreamL64)strm, windowBits);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_inflate(
                ZNgStreamL64 strm,
                ZLibFlush flush);
            internal zng_inflate InflatePtr;
            public override ZLibRet Inflate(ZStreamBase strm, ZLibFlush flush)
            {
                return InflatePtr((ZNgStreamL64)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_inflateEnd(
                ZNgStreamL64 strm);
            internal zng_inflateEnd InflateEndPtr;
            public override ZLibRet InflateEnd(ZStreamBase strm)
            {
                return InflateEndPtr((ZNgStreamL64)strm);
            }
            #endregion

            #region Advanced - DeflateSetDictionary, DeflateReset, DeflateParams
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet zng_deflateSetDictionary(
                ZNgStreamL64 strm,
                byte* dictionary,
                uint dictLength);
            internal unsafe zng_deflateSetDictionary DeflateSetDictionaryPtr;
            public override unsafe ZLibRet DeflateSetDictionary(ZStreamBase strm, byte* dictionary, uint dictLength)
            {
                return DeflateSetDictionaryPtr((ZNgStreamL64)strm, dictionary, dictLength);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet zng_deflateReset(
                ZNgStreamL64 strm);
            internal unsafe zng_deflateReset DeflateResetPtr;
            public override ZLibRet DeflateReset(ZStreamBase strm)
            {
                return DeflateResetPtr((ZNgStreamL64)strm);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet zng_deflateParams(
                ZNgStreamL64 strm,
                int level,
                int strategy);
            internal unsafe zng_deflateParams DeflateParamsPtr;
            public override ZLibRet DeflateParams(ZStreamBase strm, int level, int strategy)
            {
                return DeflateParamsPtr((ZNgStreamL64)strm, level, strategy);
            }
            #endregion

            #region ZLibCompileFlags
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ulong zng_zlibCompileFlags();
            internal zng_zlibCompileFlags ZLibCompileFlagsPtr;
            public override uint ZLibCompileFlags()
            {
                return (uint)ZLibCompileFlagsPtr();
            }
            #endregion
        }
        #endregion

        #region zlib-ng: NativeAbi Long32 - Cdecl (arm, POSIX x86, Windows x86 cdecl)
        internal sealed class ZLibNgNativeAbiCdeclL32 : ZLibNgNativeAbiCdecl
        {
            public ZLibNgNativeAbiCdeclL32(ZLibLoader lib) : base(lib)
            {
            }

            #region Load and Reset Functions
            public override void LoadFunctions()
            {
                DeflateInit2Ptr = Lib.GetFuncPtr<zng_deflateInit2>(nameof(zng_deflateInit2));
                DeflatePtr = Lib.GetFuncPtr<zng_deflate>(nameof(zng_deflate));
                DeflateEndPtr = Lib.GetFuncPtr<zng_deflateEnd>(nameof(zng_deflateEnd));

                InflateInit2Ptr = Lib.GetFuncPtr<zng_inflateInit2>(nameof(zng_inflateInit2));
                InflatePtr = Lib.GetFuncPtr<zng_inflate>(nameof(zng_inflate));
                InflateEndPtr = Lib.GetFuncPtr<zng_inflateEnd>(nameof(zng_inflateEnd));

                DeflateSetDictionaryPtr = Lib.GetFuncPtr<zng_deflateSetDictionary>(nameof(zng_deflateSetDictionary));
                DeflateResetPtr = Lib.GetFuncPtr<zng_deflateReset>(nameof(zng_deflateReset));
                DeflateParamsPtr = Lib.GetFuncPtr<zng_deflateParams>(nameof(zng_deflateParams));

                ZLibCompileFlagsPtr = Lib.GetFuncPtr<zng_zlibCompileFlags>(nameof(zng_zlibCompileFlags));

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

                DeflateSetDictionaryPtr = null;
                DeflateResetPtr = null;
                DeflateParamsPtr = null;

                ZLibCompileFlagsPtr = null;

                base.ResetFunctions();
            }
            #endregion

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_deflateInit2(
                ZNgStreamL32 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                int windowBits,
                ZLibMemLevel memLevel,
                ZLibCompStrategy strategy);
            internal zng_deflateInit2 DeflateInit2Ptr;
            public override ZLibRet DeflateInit(ZStreamBase strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel)
            {
                return DeflateInit2Ptr((ZNgStreamL32)strm, level, ZLibCompMethod.Deflated, windowBits, memLevel, ZLibCompStrategy.Default);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_deflate(
                ZNgStreamL32 strm,
                ZLibFlush flush);
            internal zng_deflate DeflatePtr;
            public override ZLibRet Deflate(ZStreamBase strm, ZLibFlush flush)
            {
                return DeflatePtr((ZNgStreamL32)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_deflateEnd(
                ZNgStreamL32 strm);
            internal zng_deflateEnd DeflateEndPtr;
            public override ZLibRet DeflateEnd(ZStreamBase strm)
            {
                return DeflateEndPtr((ZNgStreamL32)strm);
            }
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_inflateInit2(
                ZNgStreamL32 strm,
                int windowBits);
            internal zng_inflateInit2 InflateInit2Ptr;
            public override ZLibRet InflateInit(ZStreamBase strm, int windowBits)
            {
                string zlibVer = ZLibVersion();
                return InflateInit2Ptr((ZNgStreamL32)strm, windowBits);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_inflate(
                ZNgStreamL32 strm,
                ZLibFlush flush);
            internal zng_inflate InflatePtr;
            public override ZLibRet Inflate(ZStreamBase strm, ZLibFlush flush)
            {
                return InflatePtr((ZNgStreamL32)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_inflateEnd(
                ZNgStreamL32 strm);
            internal zng_inflateEnd InflateEndPtr;
            public override ZLibRet InflateEnd(ZStreamBase strm)
            {
                return InflateEndPtr((ZNgStreamL32)strm);
            }
            #endregion

            #region Advanced - DeflateSetDictionary, DeflateReset, DeflateParams
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet zng_deflateSetDictionary(
                ZNgStreamL32 strm,
                byte* dictionary,
                uint dictLength);
            internal unsafe zng_deflateSetDictionary DeflateSetDictionaryPtr;
            public override unsafe ZLibRet DeflateSetDictionary(ZStreamBase strm, byte* dictionary, uint dictLength)
            {
                return DeflateSetDictionaryPtr((ZNgStreamL32)strm, dictionary, dictLength);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet zng_deflateReset(
                ZNgStreamL32 strm);
            internal unsafe zng_deflateReset DeflateResetPtr;
            public override ZLibRet DeflateReset(ZStreamBase strm)
            {
                return DeflateResetPtr((ZNgStreamL32)strm);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet zng_deflateParams(
                ZNgStreamL32 strm,
                int level,
                int strategy);
            internal unsafe zng_deflateParams DeflateParamsPtr;
            public override ZLibRet DeflateParams(ZStreamBase strm, int level, int strategy)
            {
                return DeflateParamsPtr((ZNgStreamL32)strm, level, strategy);
            }
            #endregion

            #region ZLibCompileFlags
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate uint zng_zlibCompileFlags();
            internal zng_zlibCompileFlags ZLibCompileFlagsPtr;
            public override uint ZLibCompileFlags()
            {
                return ZLibCompileFlagsPtr();
            }
            #endregion
        }
        #endregion

        #region zlib-ng: NativeAbi Long32 - Stdcall (Windwos x86 stdcall)
        internal sealed class ZLibNgNativeAbiStdcallL32 : ZLibNgNativeAbiStdcall
        {
            public ZLibNgNativeAbiStdcallL32(ZLibLoader lib) : base(lib)
            {
            }

            #region Load and Reset Functions
            public override void LoadFunctions()
            {
                DeflateInit2Ptr = Lib.GetFuncPtr<zng_deflateInit2>(nameof(zng_deflateInit2));
                DeflatePtr = Lib.GetFuncPtr<zng_deflate>(nameof(zng_deflate));
                DeflateEndPtr = Lib.GetFuncPtr<zng_deflateEnd>(nameof(zng_deflateEnd));

                InflateInit2Ptr = Lib.GetFuncPtr<zng_inflateInit2>(nameof(zng_inflateInit2));
                InflatePtr = Lib.GetFuncPtr<zng_inflate>(nameof(zng_inflate));
                InflateEndPtr = Lib.GetFuncPtr<zng_inflateEnd>(nameof(zng_inflateEnd));

                DeflateSetDictionaryPtr = Lib.GetFuncPtr<zng_deflateSetDictionary>(nameof(zng_deflateSetDictionary));
                DeflateResetPtr = Lib.GetFuncPtr<zng_deflateReset>(nameof(zng_deflateReset));
                DeflateParamsPtr = Lib.GetFuncPtr<zng_deflateParams>(nameof(zng_deflateParams));

                ZLibCompileFlagsPtr = Lib.GetFuncPtr<zng_zlibCompileFlags>(nameof(zng_zlibCompileFlags));

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

                DeflateSetDictionaryPtr = null;
                DeflateResetPtr = null;
                DeflateParamsPtr = null;

                ZLibCompileFlagsPtr = null;

                base.ResetFunctions();
            }
            #endregion

            #region Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_deflateInit2(
                ZNgStreamL32 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                int windowBits,
                ZLibMemLevel memLevel,
                ZLibCompStrategy strategy);
            internal zng_deflateInit2 DeflateInit2Ptr;
            public override ZLibRet DeflateInit(ZStreamBase strm, ZLibCompLevel level, int windowBits, ZLibMemLevel memLevel)
            {
                return DeflateInit2Ptr((ZNgStreamL32)strm, level, ZLibCompMethod.Deflated, windowBits, memLevel, ZLibCompStrategy.Default);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_deflate(
                ZNgStreamL32 strm,
                ZLibFlush flush);
            internal zng_deflate DeflatePtr;
            public override ZLibRet Deflate(ZStreamBase strm, ZLibFlush flush)
            {
                return DeflatePtr((ZNgStreamL32)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_deflateEnd(
                ZNgStreamL32 strm);
            internal zng_deflateEnd DeflateEndPtr;
            public override ZLibRet DeflateEnd(ZStreamBase strm)
            {
                return DeflateEndPtr((ZNgStreamL32)strm);
            }
            #endregion

            #region Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_inflateInit2(
                ZNgStreamL32 strm,
                int windowBits);
            internal zng_inflateInit2 InflateInit2Ptr;
            public override ZLibRet InflateInit(ZStreamBase strm, int windowBits)
            {
                string zlibVer = ZLibVersion();
                return InflateInit2Ptr((ZNgStreamL32)strm, windowBits);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_inflate(
                ZNgStreamL32 strm,
                ZLibFlush flush);
            internal zng_inflate InflatePtr;
            public override ZLibRet Inflate(ZStreamBase strm, ZLibFlush flush)
            {
                return InflatePtr((ZNgStreamL32)strm, flush);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal delegate ZLibRet zng_inflateEnd(
                ZNgStreamL32 strm);
            internal zng_inflateEnd InflateEndPtr;
            public override ZLibRet InflateEnd(ZStreamBase strm)
            {
                return InflateEndPtr((ZNgStreamL32)strm);
            }
            #endregion

            #region Advanced - DeflateSetDictionary, DeflateReset, DeflateParams
            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet zng_deflateSetDictionary(
                ZNgStreamL32 strm,
                byte* dictionary,
                uint dictLength);
            internal unsafe zng_deflateSetDictionary DeflateSetDictionaryPtr;
            public override unsafe ZLibRet DeflateSetDictionary(ZStreamBase strm, byte* dictionary, uint dictLength)
            {
                return DeflateSetDictionaryPtr((ZNgStreamL32)strm, dictionary, dictLength);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet zng_deflateReset(
                ZNgStreamL32 strm);
            internal unsafe zng_deflateReset DeflateResetPtr;
            public override ZLibRet DeflateReset(ZStreamBase strm)
            {
                return DeflateResetPtr((ZNgStreamL32)strm);
            }

            [UnmanagedFunctionPointer(CallConv)]
            internal unsafe delegate ZLibRet zng_deflateParams(
                ZNgStreamL32 strm,
                int level,
                int strategy);
            internal unsafe zng_deflateParams DeflateParamsPtr;
            public override ZLibRet DeflateParams(ZStreamBase strm, int level, int strategy)
            {
                return DeflateParamsPtr((ZNgStreamL32)strm, level, strategy);
            }
            #endregion

            #region ZLibCompileFlags
            [UnmanagedFunctionPointer(CallConv)]
            internal delegate uint zng_zlibCompileFlags();
            internal zng_zlibCompileFlags ZLibCompileFlagsPtr;
            public override uint ZLibCompileFlags()
            {
                return ZLibCompileFlagsPtr();
            }
            #endregion
        }
        #endregion
    }
}
