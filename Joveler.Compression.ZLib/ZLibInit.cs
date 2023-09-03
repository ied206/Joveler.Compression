/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler
    Copyright (C) 2017-2023 Hajin Jang

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
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.Compression.ZLib
{
    public class ZLibInitOptions
    {
        /// <summary>
        /// Does the native library have 'stdcall' calling convention? Set it to default unless you know what you are doing.
        /// <para>Set it to false for zlib1.dll (cdecl), and true for zlibwapi.dll (stdcall).</para>
        /// <para>This flag is effective only on Windows x86. Otherwise it will be ignored.</para>
        /// </summary>
        public bool IsWindowsStdcall { get; set; } = false;
        /// <summary>
        /// Does the naive library have zlib-ng 'modern' ABI? Set it to default unless you know what you are doing.
        /// <para>Set it to true only if you are loading one of 'zlib-ng2.dll', 'libz-ng.so' or 'libz-ng.dylib'.</para>
        /// <para>If the native library was built with zlib-ng 'compat' mode, set it to false.</para>
        /// </summary>
        public bool IsZLibNgModernAbi { get; set; } = false;
    }

    #region ZLibInit
    public static class ZLibInit
    {
        #region LoadManager
        internal static ZLibLoadManager Manager = new ZLibLoadManager();
        internal static ZLibLoader Lib => Manager.Lib;
        #endregion

        #region GlobalInit, GlobalCleanup
        /// <summary>
        /// Init system-default zlib native library.
        /// On Windows, calling this will cause an exception.
        /// </summary>
        public static void GlobalInit() => GlobalInit(null, new ZLibInitOptions()
        {
            IsWindowsStdcall = false,
            IsZLibNgModernAbi = false,
        });

        /// <summary>
        /// (Deprecated) Init supplied zlib native library. Use <see cref="GlobalInit(string libPath, ZLibInitOptions opts)"/> instead.
        /// <para>On Windows x86, whether to use stdcall/cdecl symbol would be guessed by dll filename.</para>
        /// <para>On Windows, calling this method will try convert filepath zlibwapi.dll to zlib1.dll if loading zlibwapi.dll has failed.</para>
        /// </summary>
        /// <param name="libPath">
        /// The path of the zlib native library file.
        /// </param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete($"Provided for backward ABI compatibility only!\r\nUse GlobalInit(string libPath, bool isStdcall) instead.\r\nAlso, please read libray release note and update your native library filepath.")]
        public static void GlobalInit(string libPath)
        {
            // Joveler.Compression.ZLib v4.x bundlded `zlibwapi.dll`.
            // Joveler.Compression.ZLib v5.x will ship `zlib1.dll` instead.
            // To accomodate users who will not update zlib init code snippet, add a compatibility shim.
            // This shim is effective only on Windows target.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && libPath != null)
            {
                const string stdcallDllName = "zlibwapi.dll";
                const string cdeclDllName = "zlib1.dll";

                string dllDir = Path.GetDirectoryName(libPath);
                string dllFileName = Path.GetFileName(libPath);

                // Crude stdcall guess logic for backward compatibility.
                bool isZLibwapi = false;
                if (dllFileName.Equals(stdcallDllName, StringComparison.OrdinalIgnoreCase))
                    isZLibwapi = true;
                else if (dllFileName.Equals(cdeclDllName, StringComparison.OrdinalIgnoreCase))
                    isZLibwapi = false;

                // If loading zlib with `zlibwapi.dll` failed, try reloading it with `zlib1.dll`.
                try
                {
                    // First, try loading supplied path itself.
                    ZLibInitOptions opts = new ZLibInitOptions()
                    {
                        IsWindowsStdcall = isZLibwapi,
                        IsZLibNgModernAbi = false,
                    };
                    Manager.GlobalInit(libPath, opts);
                }
                catch (DllNotFoundException stdEx)
                {
                    // It seems user did not update init code snippet, and used "zlibwapi.dll".
                    // Let's try loading it with `zlib1.dll` instead.
                    if (isZLibwapi)
                    {
                        string cdeclDllPath;
                        if (dllDir == null)
                            cdeclDllPath = cdeclDllName;
                        else
                            cdeclDllPath = Path.Combine(dllDir, cdeclDllName);

                        try
                        {
                            ZLibInitOptions loadData = new ZLibInitOptions()
                            {
                                IsWindowsStdcall = false,
                                IsZLibNgModernAbi = false,
                            };
                            Manager.GlobalInit(cdeclDllPath, loadData);
                        }
                        catch (DllNotFoundException)
                        {
                            throw stdEx;
                        }
                    }
                }
            }
            else
            {
                ZLibInitOptions loadData = new ZLibInitOptions();
                Manager.GlobalInit(libPath, loadData);
            }
        }

        /// <summary>
        /// Init supplied zlib native library, with explicit stdcall/cdecl flag.
        /// </summary>
        /// <param name="libPath">
        /// The path of the zlib native library file.
        /// </param>
        /// <param name="opts">
        /// Controls the ABI used to interface native library.
        /// In most cases, using a default value is enough.
        /// </param>
        public static void GlobalInit(string libPath, ZLibInitOptions opts)
        {
            Manager.GlobalInit(libPath, opts);
        }

        public static void GlobalCleanup()
        {
            Manager.GlobalCleanup();
        }

        public static bool TryGlobalCleanup()
        {
            return Manager.TryGlobalCleanup();
        }
        #endregion

        #region Version - (Static)
        /// <summary>
        /// The application can compare zlibVersion and ZLIB_VERSION for consistency.
        /// If the first character differs, the library code actually used is not
        /// compatible with the zlib.h header file used by the application.  This check
        /// is automatically made by deflateInit and inflateInit.
        /// </summary>
        public static string VersionString()
        {
            Manager.EnsureLoaded();
            return Lib.NativeAbi.ZLibVersion();
        }
        #endregion
    }
    #endregion
}
