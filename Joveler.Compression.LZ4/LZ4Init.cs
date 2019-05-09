/*
    Derived from LZ4 header files (BSD 2-Clause)
    Copyright (c) 2011-2016, Yann Collet

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2019 Hajin Jang

    Redistribution and use in source and binary forms, with or without modification,
    are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this
      list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice, this
      list of conditions and the following disclaimer in the documentation and/or
      other materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
    WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
    ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
    (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
    LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
    ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
    (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
    SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
#if !NET451
using System.Runtime.InteropServices;
#endif

namespace Joveler.Compression.LZ4
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class LZ4Init
    {
        #region GlobalInit
        public static void GlobalInit(string libPath) => GlobalInit(libPath, 64 * 1024);

        public static void GlobalInit(string libPath, int bufferSize)
        {
            if (NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgAlreadyInit);

#if !NET451
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
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

                // Check if dll is valid (liblz4.so.1.8.2.dll)
                if (NativeMethods.Win32.GetProcAddress(NativeMethods.hModule, "LZ4F_getVersion") == IntPtr.Zero)
                {
                    GlobalCleanup();
                    throw new ArgumentException($"[{libPath}] is not a valid LZ4 library");
                }
            }
#if !NET451
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (libPath == null)
                    libPath = "/usr/lib/x86_64-linux-gnu/liblz4.so.1"; // Try to call system-installed lz4
                if (!File.Exists(libPath))
                    throw new ArgumentException("Specified .so file does not exist");

                NativeMethods.hModule = NativeMethods.Linux.dlopen(libPath, NativeMethods.Linux.RTLD_NOW | NativeMethods.Linux.RTLD_GLOBAL);
                if (NativeMethods.hModule == IntPtr.Zero)
                    throw new ArgumentException($"Unable to load [{libPath}], {NativeMethods.Linux.dlerror()}");

                // Check if dll is valid libz.so
                if (NativeMethods.Linux.dlsym(NativeMethods.hModule, "LZ4F_getVersion") == IntPtr.Zero)
                {
                    GlobalCleanup();
                    throw new ArgumentException($"[{libPath}] is not a valid liblz4.so");
                }
            }
#endif

            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (bufferSize < 4096)
                bufferSize = 4096;
            LZ4FrameStream.BufferSize = bufferSize;

            try
            {
                NativeMethods.LoadFunctions();
                LZ4FrameStream.FrameVersion = NativeMethods.GetFrameVersion();
            }
            catch (Exception)
            {
                GlobalCleanup();
                throw;
            }
        }
        #endregion

        #region GlobalCleanup
        public static void GlobalCleanup()
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

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
        #endregion

        #region Version
        public static Version Version()
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            /*
                Definition from "lz4.h"

#define LZ4_VERSION_MAJOR    1 
#define LZ4_VERSION_MINOR    8 
#define LZ4_VERSION_RELEASE  3

#define LZ4_VERSION_NUMBER (LZ4_VERSION_MAJOR *100*100 + LZ4_VERSION_MINOR *100 + LZ4_VERSION_RELEASE)

#define LZ4_LIB_VERSION LZ4_VERSION_MAJOR.LZ4_VERSION_MINOR.LZ4_VERSION_RELEASE
            */

            int verInt = (int)NativeMethods.VersionNumber();
            int major = verInt / 10000;
            int minor = verInt % 10000 / 100;
            int revision = verInt % 100;

            return new Version(major, minor, revision);
        }

        public static string VersionString()
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            return NativeMethods.VersionString();
        }
        #endregion
    }
}
