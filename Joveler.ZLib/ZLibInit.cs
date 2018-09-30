using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.ZLib
{
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
                    int ret = NativeMethods.Win32.FreeLibrary(NativeMethods.hModule);
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

        #region Version
        /// <summary>
        /// The application can compare zlibVersion and ZLIB_VERSION for consistency.
        /// If the first character differs, the library code actually used is not
        /// compatible with the zlib.h header file used by the application.  This check
        /// is automatically made by deflateInit and inflateInit.
        /// </summary>
        public static string VersionString() => NativeMethods.ZLibVersion();
        #endregion
    }
    #endregion
}
