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
using System.IO;
#if !NET451
using System.Runtime.InteropServices;
#endif

namespace Joveler.Compression.ZLib
{
    #region ZLibInit
    public static class ZLibInit
    {
        #region GlobalInit, GlobalCleanup
        public static void GlobalInit(string libPath = null, int bufferSize = 64 * 1024)
        {
            if (NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgAlreadyInit);

#if !NET451
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                NativeMethods.LongBitType = NativeMethods.LongBits.Long32;
                if (libPath == null)
                    throw new ArgumentNullException(nameof(libPath));

                libPath = Path.GetFullPath(libPath);
                if (!File.Exists(libPath))
                    throw new ArgumentException("Specified .dll file does not exist");

                // Set proper directory to search, unless LoadLibrary can fail when loading chained dll files.
                string libDir = Path.GetDirectoryName(libPath);
                if (libDir != null && !libDir.Equals(AppDomain.CurrentDomain.BaseDirectory))
                    NativeMethods.Win32.SetDllDirectory(libDir);

                NativeMethods.hModule = NativeMethods.Win32.LoadLibrary(libPath);
                if (NativeMethods.hModule == IntPtr.Zero)
                    throw new ArgumentException($"Unable to load [{libPath}]", new Win32Exception());

                // Reset dll search directory to prevent dll hijacking
                NativeMethods.Win32.SetDllDirectory(null);

                // Check if dll is valid zlibwapi.dll
                if (NativeMethods.Win32.GetProcAddress(NativeMethods.hModule, "zlibCompileFlags") == IntPtr.Zero ||
                    NativeMethods.Win32.GetProcAddress(NativeMethods.hModule, "adler32") == IntPtr.Zero)
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
                if (NativeMethods.Linux.dlsym(NativeMethods.hModule, "zlibCompileFlags") == IntPtr.Zero ||
                    NativeMethods.Linux.dlsym(NativeMethods.hModule, "adler32") == IntPtr.Zero)
                {
                    GlobalCleanup();
                    throw new ArgumentException($"[{libPath}] is not a valid libz.so");
                }
            }
#endif

            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (bufferSize < 4096)
                bufferSize = 4096;
            NativeMethods.BufferSize = bufferSize;

            try
            {
                NativeMethods.LoadFunctions();
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
#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                {
                    int ret = NativeMethods.Win32.FreeLibrary(NativeMethods.hModule);
                    Debug.Assert(ret != 0);
                }
#if !NET451
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    int ret = NativeMethods.Linux.dlclose(NativeMethods.hModule);
                    Debug.Assert(ret == 0);
                }
#endif
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
