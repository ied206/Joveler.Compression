/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2019 Hajin Jang

    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
#if !NET451
using System.Runtime.InteropServices;
#endif

namespace Joveler.Compression.XZ
{
    public static class XZInit
    {
        #region GlobalInit
        public static void GlobalInit(string libPath, int bufferSize = 64 * 1024)
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

                // Check if dll is valid (liblzma.dll)
                if (NativeMethods.Win32.GetProcAddress(NativeMethods.hModule, "lzma_version_number") == IntPtr.Zero)
                {
                    GlobalCleanup();
                    throw new ArgumentException($"[{libPath}] is not a valid liblzma library");
                }
            }
#if !NET451
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (libPath == null)
                    libPath = "/lib/x86_64-linux-gnu/liblzma.so.5"; // Try to call system-installed lz4
                if (!File.Exists(libPath))
                    throw new ArgumentException("Specified .so file does not exist");

                NativeMethods.hModule = NativeMethods.Linux.dlopen(libPath, NativeMethods.Linux.RTLD_NOW | NativeMethods.Linux.RTLD_GLOBAL);
                if (NativeMethods.hModule == IntPtr.Zero)
                    throw new ArgumentException($"Unable to load [{libPath}], {NativeMethods.Linux.dlerror()}");

                // Check if dll is valid (liblzma.dll)
                if (NativeMethods.Linux.dlsym(NativeMethods.hModule, "lzma_version_number") == IntPtr.Zero)
                {
                    GlobalCleanup();
                    throw new ArgumentException($"[{libPath}] is not a valid liblzma.so");
                }
            }
#endif

            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (bufferSize < 4096)
                bufferSize = 4096;
            XZStream.BufferSize = bufferSize;

            try
            {
                NativeMethods.LoadFunctions();
                XZStream.BufferSize = bufferSize;
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

        #region Version - (Static)
        public static Version Version()
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            /*
             * Note from "lzma\version.h"
             *
             * The version number is of format xyyyzzzs where
             *  - x = major
             *  - yyy = minor
             *  - zzz = revision
             *  - s indicates stability: 0 = alpha, 1 = beta, 2 = stable
             *
             * The same xyyyzzz triplet is never reused with different stability levels.
             * For example, if 5.1.0alpha has been released, there will never be 5.1.0beta
             * or 5.1.0 stable.
             */

            int verInt = (int)NativeMethods.LzmaVersionNumber();
            int major = verInt / 10000000;
            int minor = verInt % 10000000 / 10000;
            int revision = verInt % 10000 / 10;
            int stability = verInt % 10;

            return new Version(major, minor, revision, stability);
        }

        public static string VersionString()
        {
            if (!NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgInitFirstError);

            return NativeMethods.LzmaVersionString();
        }
        #endregion
    }
}
