using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
#if !NET451
using System.Runtime.InteropServices;
#endif

namespace Joveler.Compression.XZ
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class XZInit
    {
        #region GlobalInit
        public static void GlobalInit(string libPath, int bufferSize = 64 * 1024)
        {
            if (NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgAlreadyInited);
#if !NET451
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                if (libPath == null)
                    throw new ArgumentNullException(nameof(libPath));
                if (!File.Exists(libPath))
                    throw new FileNotFoundException("Specified dll does not exist");

                NativeMethods.hModule = NativeMethods.Win32.LoadLibrary(libPath);
                if (NativeMethods.hModule == IntPtr.Zero)
                    throw new ArgumentException($"Unable to load [{libPath}]", new Win32Exception());

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
