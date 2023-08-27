/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler
    Copyright (C) 2017-2020 Hajin Jang

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
using System.Runtime.InteropServices;

namespace Joveler.Compression.ZLib
{
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
        public static void GlobalInit() => GlobalInit(null, false);
        /// <summary>
        /// Init supplied zlib native library.
        /// <para>On Windows, using <see cref="GlobalInit(string libPath, bool isZLibWapi)"/> instead is recommended.</para>
        /// <para>On Windows x86, whether to use stdcall/cdecl symbol would be guessed by dll filename.</para>
        /// </summary>
        /// <param name="libPath">
        /// The path of the zlib native library file.
        /// </param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete($"Provided for backward compatibility only. Use GlobalInit(string libPath, bool isStdcall) instead.")]
        public static void GlobalInit(string libPath)
        {
            ZLibLoadData loadData = new ZLibLoadData();

            // Crude stdcall guess logic for backward compatibility.
            // On Windows, using GlobalInit(libPath, isZLibWapi) is recommended.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (libPath.StartsWith("zlibwapi", StringComparison.OrdinalIgnoreCase))
                    loadData.IsWindowsX86Stdcall = true;
                else if (libPath.StartsWith("zlib1", StringComparison.OrdinalIgnoreCase))
                    loadData.IsWindowsX86Stdcall = false;
            }

            Manager.GlobalInit(libPath, loadData);
        }

        /// <summary>
        /// Init supplied zlib native library, with explicit stdcall/cdecl flag.
        /// </summary>
        /// <param name="libPath">
        /// The path of the zlib native library file.
        /// </param>
        /// <param name="isStdcall">
        /// Set it to true for zlibwapi.dll (stdcall). Set it to false for zlib1.dll (cdecl).
        /// <para>This flag is effective only on Windows x86.</para>
        /// </param>
        public static void GlobalInit(string libPath, bool isStdcall)
        {
            ZLibLoadData loadData = new ZLibLoadData()
            {
                IsWindowsX86Stdcall = isStdcall,
            };
            Manager.GlobalInit(libPath, loadData);
        }
        public static void GlobalCleanup() => Manager.GlobalCleanup();
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
