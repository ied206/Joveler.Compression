/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

    C# Wrapper based on zlibnet v1.3.3 (https://zlibnet.codeplex.com/)
    Copyright (C) @hardon (https://www.codeplex.com/site/users/view/hardon)
    Copyright (C) 2017-2018 Hajin Jang

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
using System.IO;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace Joveler.ZLib
{
    #region PinnedArray
    internal class PinnedArray<T> : IDisposable
    {
        private GCHandle _hArray;
        public T[] Array;
        public IntPtr Ptr => _hArray.AddrOfPinnedObject();

        public IntPtr this[int idx] => Marshal.UnsafeAddrOfPinnedArrayElement(Array, idx);
        public static implicit operator IntPtr(PinnedArray<T> fixedArray) => fixedArray[0];

        public PinnedArray(T[] array)
        {
            Array = array;
            _hArray = GCHandle.Alloc(array, GCHandleType.Pinned);
        }

        ~PinnedArray()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_hArray.IsAllocated)
                    _hArray.Free();
            }
        }
    }
    #endregion

    #region ZLibInit
    public static class ZLibInit
    {
        #region GlobalInit, GlobalCleanup
        public static void GlobalInit(string libPath = null, int bufferSize = 64 * 1024)
        {
            if (NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgAlreadyInited);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                NativeMethods.LongBitType = NativeMethods.LongBits.Long32;
                if (libPath == null || !File.Exists(libPath))
                    throw new ArgumentException("Specified .dll file does not exist");

                NativeMethods.hModule = NativeMethods.Win32.LoadLibrary(libPath);
                if (NativeMethods.hModule == IntPtr.Zero)
                    throw new ArgumentException($"Unable to load [{libPath}]", new Win32Exception());

                // Check if dll is valid zlibwapi.dll
                if (NativeMethods.Win32.GetProcAddress(NativeMethods.hModule, "zlibCompileFlags") == IntPtr.Zero ||
                    NativeMethods.Win32.GetProcAddress(NativeMethods.hModule, "adler32") == IntPtr.Zero)
                {
                    GlobalCleanup();
                    throw new ArgumentException($"[{libPath}] is not valid zlibwapi.dll");
                }
            }
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
                if (NativeMethods.Linux.dlsym(NativeMethods.hModule, "zlibCompileFlags") == IntPtr.Zero ||
                    NativeMethods.Linux.dlsym(NativeMethods.hModule, "adler32") == IntPtr.Zero)
                {
                    GlobalCleanup();
                    throw new ArgumentException($"[{libPath}] is not a valid libz.so");
                }
            }
            
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (bufferSize < 4096)
                bufferSize = 4096;
            NativeMethods.BufferSize = bufferSize;

            try
            {
                NativeMethods.LoadFuntions();
            }
            catch (Exception)
            {
                GlobalCleanup();
                throw;
            }
        }

        public static void GlobalCleanup()
        {
            if (NativeMethods.hModule != IntPtr.Zero)
            {
                NativeMethods.ResetFunctions();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    uint ret = NativeMethods.Win32.FreeLibrary(NativeMethods.hModule);
                    Debug.Assert(ret != 0);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    int ret = NativeMethods.Linux.dlclose(NativeMethods.hModule);
                    Debug.Assert(ret == 0);
                }
                NativeMethods.hModule = IntPtr.Zero;
            }
            else
            {
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);
            }
        }
        #endregion

        #region ZLibVersion
        /// <summary>
        /// The application can compare zlibVersion and ZLIB_VERSION for consistency.
        /// If the first character differs, the library code actually used is not
        /// compatible with the zlib.h header file used by the application.  This check
        /// is automatically made by deflateInit and inflateInit.
        /// </summary>
        public static string ZLibVersion() => NativeMethods.ZLibVersion();
        #endregion
    }
    #endregion

    #region NativeMethods
    internal static class NativeMethods
    {
        #region Const
        internal const string MsgInitFirstError = "Please call ZLib.GlobalInit() first!";
        internal const string MsgAlreadyInited = "Joveler.ZLib is already initialized.";

        private const int DEF_MEM_LEVEL = 8;
        private const string ZLIB_VERSION = "1.2.11"; // This code is based on zlib 1.2.11's zlib.h
        #endregion

        #region Fields
        internal enum LongBits
        {
            Long64 = 0, // Windows, Linux 32bit
            Long32 = 1, // Linux 64bit
        }

        internal static IntPtr hModule;
        internal static LongBits LongBitType { get; set; }
        internal static bool Loaded => hModule != IntPtr.Zero;
        internal static int BufferSize { get; set; } = 64 * 1024;
        #endregion

        #region Windows kernel32 API
        internal static class Win32
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPTStr)] string lpFileName);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            internal static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern uint FreeLibrary(IntPtr hModule);
        }
        #endregion

        #region Linux libdl API
#pragma warning disable IDE1006 // 명명 스타일
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
#pragma warning restore IDE1006 // 명명 스타일
        #endregion

        #region LoadFunctions, ResetFunctions
        private static T GetFuncPtr<T>(string funcSymbol) where T : Delegate
        {
            IntPtr funcPtr;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                funcPtr = Win32.GetProcAddress(hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new ArgumentException($"Cannot import [{funcSymbol}]", new Win32Exception());
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                funcPtr = Linux.dlsym(hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new ArgumentException($"Cannot import [{funcSymbol}]", Linux.dlerror());
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }

        public static void LoadFuntions()
        {
            if (LongBitType == LongBits.Long32)
            {
                #region (Common) Deflate - DeflateInit2, Deflate, DeflateEnd
                L32.DeflateInit2 = GetFuncPtr<L32.deflateInit2_>("deflateInit2_");
                L32.Deflate = GetFuncPtr<L32.deflate>("deflate");
                L32.DeflateEnd = GetFuncPtr<L32.deflateEnd>("deflateEnd");
                #endregion

                #region (Common) Inflate - InflateInit2, Inflate, InflateEnd
                L32.InflateInit2 = GetFuncPtr<L32.inflateInit2_>("inflateInit2_");
                L32.Inflate = GetFuncPtr<L32.inflate>("inflate");
                L32.InflateEnd = GetFuncPtr<L32.inflateEnd>("inflateEnd");
                #endregion
            }
            else
            {
                #region (Common) Deflate - DeflateInit2, Deflate, DeflateEnd
                L64.DeflateInit2 = GetFuncPtr<L64.deflateInit2_>("deflateInit2_");
                L64.Deflate = GetFuncPtr<L64.deflate>("deflate");
                L64.DeflateEnd = GetFuncPtr<L64.deflateEnd>("deflateEnd");
                #endregion

                #region (Common) Inflate - InflateInit2, Inflate, InflateEnd
                L64.InflateInit2 = GetFuncPtr<L64.inflateInit2_>("inflateInit2_");
                L64.Inflate = GetFuncPtr<L64.inflate>("inflate");
                L64.InflateEnd = GetFuncPtr<L64.inflateEnd>("inflateEnd");
                #endregion
            }

            #region (zlibwapi) Checksum - Adler32, Crc32
            Adler32 = GetFuncPtr<adler32>("adler32");
            Crc32 = GetFuncPtr<crc32>("crc32");
            #endregion

            #region (zlibwapi) ZLibVersion
            ZLibVersion = GetFuncPtr<zlibVersion>("zlibVersion");
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

        #region CheckZLibLoaded
        internal static void CheckZLibLoaded()
        {
            if (!Loaded)
                ZLibInit.GlobalInit();
        }
        #endregion

        #region zlib Function Pointers
        internal static class L64
        {
            #region (Common) Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode deflateInit2_(
                ZStreamL64 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                ZLibWriteType windowBits,
                int memLevel,
                ZLibCompressionStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal static deflateInit2_ DeflateInit2;

            internal static ZLibReturnCode DeflateInit(ZStreamL64 strm, ZLibCompLevel level, ZLibWriteType windowBits)
            {
                return DeflateInit2(strm, level, ZLibCompMethod.DEFLATED, windowBits, DEF_MEM_LEVEL,
                        ZLibCompressionStrategy.DEFAULT_STRATEGY, ZLIB_VERSION, Marshal.SizeOf<ZStreamL64>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode deflate(
                ZStreamL64 strm,
                ZLibFlush flush);
            internal static deflate Deflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode deflateEnd(
                ZStreamL64 strm);
            internal static deflateEnd DeflateEnd;
            #endregion

            #region (Common) Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode inflateInit2_(
                ZStreamL64 strm,
                ZLibOpenType windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal static inflateInit2_ InflateInit2;

            internal static ZLibReturnCode InflateInit(ZStreamL64 strm, ZLibOpenType windowBits)
            {
                return InflateInit2(strm, windowBits, ZLIB_VERSION, Marshal.SizeOf<ZStreamL64>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode inflate(
                ZStreamL64 strm,
                ZLibFlush flush);
            internal static inflate Inflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode inflateEnd(
                ZStreamL64 strm);
            internal static inflateEnd InflateEnd;
            #endregion
        }

        internal static class L32
        {
            #region (Common) Deflate - DeflateInit2, Deflate, DeflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode deflateInit2_(
                ZStreamL32 strm,
                ZLibCompLevel level,
                ZLibCompMethod method,
                ZLibWriteType windowBits,
                int memLevel,
                ZLibCompressionStrategy strategy,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal static deflateInit2_ DeflateInit2;

            internal static ZLibReturnCode DeflateInit(ZStreamL32 strm, ZLibCompLevel level, ZLibWriteType windowBits)
            {
                return DeflateInit2(strm, level, ZLibCompMethod.DEFLATED, windowBits, DEF_MEM_LEVEL,
                        ZLibCompressionStrategy.DEFAULT_STRATEGY, ZLIB_VERSION, Marshal.SizeOf<ZStreamL32>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode deflate(
                ZStreamL32 strm,
                ZLibFlush flush);
            internal static deflate Deflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode deflateEnd(
                ZStreamL32 strm);
            internal static deflateEnd DeflateEnd;
            #endregion

            #region (Common) Inflate - InflateInit2, Inflate, InflateEnd
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode inflateInit2_(
                ZStreamL32 strm,
                ZLibOpenType windowBits,
                [MarshalAs(UnmanagedType.LPStr)] string version,
                int stream_size);
            internal static inflateInit2_ InflateInit2;

            internal static ZLibReturnCode InflateInit(ZStreamL32 strm, ZLibOpenType windowBits)
            {
                return InflateInit2(strm, windowBits, ZLIB_VERSION, Marshal.SizeOf<ZStreamL32>());
            }

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode inflate(
                ZStreamL32 strm,
                ZLibFlush flush);
            internal static inflate Inflate;

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            internal delegate ZLibReturnCode inflateEnd(ZStreamL32 strm);
            internal static inflateEnd InflateEnd;
            #endregion
        }

        #region Checksum - Adler32, Crc32
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate uint adler32(
            uint crc,
            IntPtr buf,
            uint len);
        internal static adler32 Adler32;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate uint crc32(
            uint crc,
            IntPtr buf,
            uint len);
        internal static crc32 Crc32;
        #endregion

        #region ZLibVersion
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        internal delegate string zlibVersion();
        internal static zlibVersion ZLibVersion;
        #endregion
        #endregion
    }
    #endregion
}
