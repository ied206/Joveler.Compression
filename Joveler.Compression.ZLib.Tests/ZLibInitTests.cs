/*
    C# tests by Hajin Jang
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


using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.Compression.ZLib.Tests
{
    [TestClass]
    [TestCategory("Joveler.Compression.ZLib")]
    public class ZLibInitTests
    {
        [TestMethod]
        public void VersionTests()
        {
            Console.WriteLine(ZLibInit.VersionString());
        }

        #region LegacyInitCompatShim
        [TestMethod]
        [DoNotParallelize]
        public void LegacyInitCompatShim()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            string libPath = TestSetup.GetNativeLibPath(false);
            try
            {
                string libDir = Path.GetDirectoryName(libPath);
                string newLibPath = Path.Combine(libDir, "zlibwapi.dll");
                Console.WriteLine($"First try libPath (DOES NOT EXIST): {newLibPath}");
                Console.WriteLine($"Second try libPath (DOES EXIST: {libPath}");

                ZLibInit.GlobalCleanup();

                // Supress Obsolete warning for compat shim testing
#pragma warning disable CS0618
                ZLibInit.GlobalInit(newLibPath);
#pragma warning restore CS0618

                Console.WriteLine(ZLibInit.VersionString());
            }
            finally
            {
                // Reload to zlib1.dll
                if (ZLibInit.IsLoaded)
                    ZLibInit.GlobalCleanup();

                ZLibInit.GlobalInit(libPath, TestSetup.GetNativeLoadOptions());
                Console.WriteLine($"zlib instance restored: {libPath}");
            }
        }
        #endregion

        #region zlib-ng Modern ABI Load Tests
        [TestMethod]
        [DoNotParallelize]
        public void LoadZLibNgModernAbi()
        {
            try
            {
                string libPath = TestSetup.GetNativeLibPath(true);
                Console.WriteLine($"Try loading zlib-ng modern ABI: {libPath}");

                ZLibInit.GlobalCleanup();
                ZLibInit.GlobalInit(libPath, new ZLibInitOptions() { IsZLibNgModernAbi = true });
                Console.WriteLine(ZLibInit.VersionString());
            }
            finally
            {
                // Reload to default zlib path
                if (ZLibInit.IsLoaded)
                    ZLibInit.GlobalCleanup();

                string libPath = TestSetup.GetNativeLibPath(false);
                ZLibInit.GlobalInit(libPath, TestSetup.GetNativeLoadOptions());
                Console.WriteLine($"zlib instance restored: {libPath}");
            }
        }
        #endregion
    }
}
