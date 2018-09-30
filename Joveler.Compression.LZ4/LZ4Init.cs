using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.Compression.LZ4
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class LZ4Init
    {
        #region GlobalInit
        public static void GlobalInit(string libPath, int bufferSize = 64 * 1024)
        {
            if (NativeMethods.Loaded)
                throw new InvalidOperationException(NativeMethods.MsgAlreadyInited);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (libPath == null)
                    throw new ArgumentNullException(nameof(libPath));
                if (!File.Exists(libPath))
                    throw new FileNotFoundException("Specified dll does not exist");

                NativeMethods.hModule = NativeMethods.Win32.LoadLibrary(libPath);
                if (NativeMethods.hModule == IntPtr.Zero)
                    throw new ArgumentException($"Unable to load [{libPath}]", new Win32Exception());

                // Check if dll is valid (liblz4.so.1.8.2.dll)
                if (NativeMethods.Win32.GetProcAddress(NativeMethods.hModule, "LZ4F_getVersion") == IntPtr.Zero)
                {
                    GlobalCleanup();
                    throw new ArgumentException($"[{libPath}] is not a valid LZ4 library");
                }
            }
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

            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (bufferSize < 4096)
                bufferSize = 4096;
            LZ4FrameStream.BufferSize = bufferSize;

            try
            {
                NativeMethods.LoadFuntions();
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

            NativeMethods.ResetFuntcions();

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
                #define LZ4_VERSION_RELEASE  1 

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
