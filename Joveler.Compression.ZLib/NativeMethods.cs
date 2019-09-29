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
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace Joveler.Compression.ZLib
{
    #region NativeMethods
    internal static class NativeMethods
    {
        #region Const
        internal const string MsgInitFirstError = "Please call ZLibInit.GlobalInit() first!";
        internal const string MsgAlreadyInit = "Joveler.Compression.ZLib is already initialized.";

        private const int DEF_MEM_LEVEL = 8;
        private const string ZLIB_VERSION = "1.2.11"; // This code is based on zlib 1.2.11's zlib.h
        #endregion

        #region Native Library Loading
        internal static IntPtr hModule = IntPtr.Zero;
        internal static readonly object LoadLock = new object();
        internal static bool Loaded => hModule != IntPtr.Zero;

        internal enum LongBits
        {
            Long64 = 0, // Windows, Linux 32bit
            Long32 = 1, // Linux 64bit
        }
        internal static LongBits LongBitType { get; set; }

        public static bool IsLoaded()
        {
            lock (LoadLock)
            {
                return Loaded;
            }
        }

        public static void EnsureLoaded()
        {
            lock (LoadLock)
            {
                if (!Loaded)
                    throw new InvalidOperationException(MsgInitFirstError);
            }
        }

        public static void EnsureNotLoaded()
        {
            lock (LoadLock)
            {
                if (Loaded)
                    throw new InvalidOperationException(MsgAlreadyInit);
            }
        }
        #endregion

        #region Windows kernel32 API
        internal static class Win32
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            [SuppressMessage("Globalization", "CA2101:Specify marshaling for P/Invoke string arguments")]
            internal static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

            [DllImport("kernel32.dll")]
            internal static extern int FreeLibrary(IntPtr hModule);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern int SetDllDirectory([MarshalAs(UnmanagedType.LPWStr)] string lpPathName);
        }
        #endregion

        #region Linux libdl API
#pragma warning disable IDE1006 // 명명 스타일
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        internal static class Linux
        {
            internal const int RTLD_NOW = 0x0002;
            internal const int RTLD_GLOBAL = 0x0100;

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr dlopen(string fileName, int flags);

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int dlclose(IntPtr handle);

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern string dlerror();

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr dlsym(IntPtr handle, string symbol);
        }
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
#pragma warning restore IDE1006 // 명명 스타일
        #endregion

        #region GlobalInit, GlobalCleanup
        public static void GlobalInit(string libPath = null)
        {
            lock (LoadLock)
            {
                if (Loaded)
                    throw new InvalidOperationException(MsgAlreadyInit);

#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                {
                    LongBitType = LongBits.Long32;
                    if (libPath == null)
                        throw new ArgumentNullException(nameof(libPath));

                    libPath = Path.GetFullPath(libPath);
                    if (!File.Exists(libPath))
                        throw new ArgumentException("Specified .dll file does not exist");

                    // Set proper directory to search, unless LoadLibrary can fail when loading chained dll files.
                    string libDir = Path.GetDirectoryName(libPath);
                    if (libDir != null && !libDir.Equals(AppDomain.CurrentDomain.BaseDirectory))
                        Win32.SetDllDirectory(libDir);
                    // SetDllDictionary guard
                    try
                    {
                        hModule = Win32.LoadLibrary(libPath);
                        if (hModule == IntPtr.Zero)
                            throw new ArgumentException($"Unable to load [{libPath}]", new Win32Exception());
                    }
                    finally
                    {
                        // Reset dll search directory to prevent dll hijacking
                        Win32.SetDllDirectory(null);
                    }

                    // Check if dll is valid (zlibwapi.dll)
                    if (Win32.GetProcAddress(hModule, nameof(L32.deflate)) == IntPtr.Zero ||
                        Win32.GetProcAddress(hModule, nameof(L32.inflate)) == IntPtr.Zero ||
                        Win32.GetProcAddress(hModule, nameof(adler32)) == IntPtr.Zero)
                    {
                        GlobalCleanup();
                        throw new ArgumentException($"[{libPath}] is not valid zlibwapi.dll");
                    }
                }
#if !NET451
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Architecture arch = RuntimeInformation.ProcessArchitecture;
                    switch (arch)
                    {
                        case Architecture.Arm:
                        case Architecture.X86:
                            NativeMethods.LongBitType = NativeMethods.LongBits.Long32;
                            break;
                        case Architecture.Arm64:
                        case Architecture.X64:
                            NativeMethods.LongBitType = NativeMethods.LongBits.Long64;
                            break;
                    }

                    if (libPath == null)
                        libPath = "/lib/x86_64-linux-gnu/libz.so.1"; // Try to call system-installed zlib
                    if (!File.Exists(libPath))
                        throw new ArgumentException("Specified .so file does not exist");

                    NativeMethods.hModule = NativeMethods.Linux.dlopen(libPath, NativeMethods.Linux.RTLD_NOW | NativeMethods.Linux.RTLD_GLOBAL);
                    if (NativeMethods.hModule == IntPtr.Zero)
                        throw new ArgumentException($"Unable to load [{libPath}], {NativeMethods.Linux.dlerror()}");

                    // Check if dll is valid libz.so
                    if (NativeMethods.Linux.dlsym(NativeMethods.hModule, nameof(L32.deflate)) == IntPtr.Zero ||
                        NativeMethods.Linux.dlsym(NativeMethods.hModule, nameof(L32.inflate)) == IntPtr.Zero ||
                        NativeMethods.Linux.dlsym(NativeMethods.hModule, nameof(adler32)) == IntPtr.Zero)
                    {
                        GlobalCleanup();
                        throw new ArgumentException($"[{libPath}] is not a valid libz.so");
                    }
                }
#endif

                try
                {
                    LoadFunctions();
                }
                catch (Exception)
                {
                    GlobalCleanup();
                    throw;
                }
            }
        }

        public static void GlobalCleanup()
        {
            lock (LoadLock)
            {
                if (!Loaded)
                    throw new InvalidOperationException(MsgInitFirstError);

                ResetFunctions();
#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                {
                    int ret = Win32.FreeLibrary(hModule);
                    Debug.Assert(ret != 0);
                }
#if !NET451
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    int ret = NativeMethods.Linux.dlclose(NativeMethods.hModule);
                    Debug.Assert(ret == 0);
                }
#endif
                hModule = IntPtr.Zero;
            }
        }
        #endregion

        #region GetFuncPtr
        private static T GetFuncPtr<T>(string funcSymbol) where T : Delegate
        {
            IntPtr funcPtr;
#if !NET451
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                funcPtr = Win32.GetProcAddress(hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new InvalidOperationException($"Cannot import [{funcSymbol}]", new Win32Exception());
            }
#if !NET451
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                funcPtr = Linux.dlsym(hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new InvalidOperationException($"Cannot import [{funcSymbol}], {Linux.dlerror()}");
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
#endif

            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        public static void LoadFunctions()
        {
            if (LongBitType == LongBits.Long32)
            {
                #region (Common) Deflate - DeflateInit2, Deflate, DeflateEnd
                L32.DeflateInit2 = GetFuncPtr<L32.deflateInit2_>(nameof(L32.deflateInit2_));
                L32.Deflate = GetFuncPtr<L32.deflate>(nameof(L32.deflate));
                L32.DeflateEnd = GetFuncPtr<L32.deflateEnd>(nameof(L32.deflateEnd));
                #endregion

                #region (Common) Inflate - InflateInit2, Inflate, InflateEnd
                L32.InflateInit2 = GetFuncPtr<L32.inflateInit2_>(nameof(L32.inflateInit2_));
                L32.Inflate = GetFuncPtr<L32.inflate>(nameof(L32.inflate));
                L32.InflateEnd = GetFuncPtr<L32.inflateEnd>(nameof(L32.inflateEnd));
                #endregion
            }
            else
            {
                #region (Common) Deflate - DeflateInit2, Deflate, DeflateEnd
                L64.DeflateInit2 = GetFuncPtr<L64.deflateInit2_>(nameof(L64.deflateInit2_));
                L64.Deflate = GetFuncPtr<L64.deflate>(nameof(L64.deflate));
                L64.DeflateEnd = GetFuncPtr<L64.deflateEnd>(nameof(L64.deflateEnd));
                #endregion

                #region (Common) Inflate - InflateInit2, Inflate, InflateEnd
                L64.InflateInit2 = GetFuncPtr<L64.inflateInit2_>(nameof(L64.inflateInit2_));
                L64.Inflate = GetFuncPtr<L64.inflate>(nameof(L64.inflate));
                L64.InflateEnd = GetFuncPtr<L64.inflateEnd>(nameof(L64.inflateEnd));
                #endregion
            }

            #region (zlibwapi) Checksum - Adler32, Crc32
            Adler32 = GetFuncPtr<adler32>(nameof(adler32));
            Crc32 = GetFuncPtr<crc32>(nameof(crc32));
            #endregion

            #region (zlibwapi) ZLibVersion
            ZLibVersion = GetFuncPtr<zlibVersion>(nameof(zlibVersion));
            #endregion
        }

        public static void ResetFunctions()
        {
            #region (Common) Deflate - DeflateInit2, Deflate, DeflateEnd
            L64.DeflateInit2 = null;
            L64.Deflate = null;
            L64.DeflateEnd = null;
            #endregion

            #region (Common) Inflate - InflateInit2, Inflate, InflateEnd
            L64.InflateInit2 = null;
            L64.Inflate = null;
            L64.InflateEnd = null;
            #endregion

            #region (zlibwapi) Checksum - Adler32, Crc32
            Adler32 = null;
            Crc32 = null;
            #endregion

            #region (zlibwapi) ZLibVersion
            ZLibVersion = null;
            #endregion
        }
        #endregion

        #region zlib Function Pointers
        internal static class L64
        {
            #region (Common) Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn deflateInit2_(
                ZStreamL64 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                ZLibWriteType windowBits,
                int memLevel,
                ZLibCompStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal static deflateInit2_ DeflateInit2;

            internal static ZLibReturn DeflateInit(ZStreamL64 strm, ZLibCompLevel level, ZLibWriteType windowBits)
            {
                return DeflateInit2(strm, level, ZLibCompMethod.Deflated, windowBits, DEF_MEM_LEVEL,
                        ZLibCompStrategy.Default, ZLIB_VERSION, Marshal.SizeOf<ZStreamL64>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn deflate(
                ZStreamL64 strm,
                ZLibFlush flush);
            internal static deflate Deflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn deflateEnd(
                ZStreamL64 strm);
            internal static deflateEnd DeflateEnd;
            #endregion

            #region (Common) Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn inflateInit2_(
                ZStreamL64 strm,
                ZLibOpenType windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal static inflateInit2_ InflateInit2;

            internal static ZLibReturn InflateInit(ZStreamL64 strm, ZLibOpenType windowBits)
            {
                return InflateInit2(strm, windowBits, ZLIB_VERSION, Marshal.SizeOf<ZStreamL64>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn inflate(
                ZStreamL64 strm,
                ZLibFlush flush);
            internal static inflate Inflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn inflateEnd(
                ZStreamL64 strm);
            internal static inflateEnd InflateEnd;
            #endregion
        }

        internal static class L32
        {
            #region (Common) Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn deflateInit2_(
                ZStreamL32 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                ZLibWriteType windowBits,
                int memLevel,
                ZLibCompStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal static deflateInit2_ DeflateInit2;

            internal static ZLibReturn DeflateInit(ZStreamL32 strm, ZLibCompLevel level, ZLibWriteType windowBits)
            {
                return DeflateInit2(strm, level, ZLibCompMethod.Deflated, windowBits, DEF_MEM_LEVEL,
                        ZLibCompStrategy.Default, ZLIB_VERSION, Marshal.SizeOf<ZStreamL32>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn deflate(
                ZStreamL32 strm,
                ZLibFlush flush);
            internal static deflate Deflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn deflateEnd(
                ZStreamL32 strm);
            internal static deflateEnd DeflateEnd;
            #endregion

            #region (Common) Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn inflateInit2_(
                ZStreamL32 strm,
                ZLibOpenType windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal static inflateInit2_ InflateInit2;

            internal static ZLibReturn InflateInit(ZStreamL32 strm, ZLibOpenType windowBits)
            {
                return InflateInit2(strm, windowBits, ZLIB_VERSION, Marshal.SizeOf<ZStreamL32>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn inflate(
                ZStreamL32 strm,
                ZLibFlush flush);
            internal static inflate Inflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturn inflateEnd(ZStreamL32 strm);
            internal static inflateEnd InflateEnd;
            #endregion
        }

        #region Checksum - Adler32, Crc32
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal unsafe delegate uint adler32(
            uint adler,
            byte* buf,
            uint len);
        internal static adler32 Adler32;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal unsafe delegate uint crc32(
            uint crc,
            byte* buf,
            uint len);
        internal static crc32 Crc32;
        #endregion

        #region Version - ZLibVersion
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        internal delegate string zlibVersion();
        internal static zlibVersion ZLibVersion;
        #endregion
        #endregion
    }
    #endregion
}
