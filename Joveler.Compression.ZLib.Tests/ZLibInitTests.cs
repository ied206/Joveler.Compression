/*
    C# tests by Hajin Jang
    Copyright (C) 2017-present Hajin Jang

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
    [DoNotParallelize]
    public class ZLibInitUpCdeclTests : ZLibInitTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.UpstreamCdecl;
    }

    [TestClass]
    [DoNotParallelize]
    public class ZLibInitNgCdeclTests : ZLibInitTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.ZLibNgCdecl;
    }

    #region ZLibInitTestsBase
    public abstract class ZLibInitTestsBase : ZLibTestBase
    {
        [TestMethod]
        public void VersionTests()
        {
            Console.WriteLine(ZLibInit.VersionString());
        }
    }
    #endregion

    [TestClass]
    public class ZLiLoadTests 
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            TestSetup.Cleanup();
        }

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void ClassCleanup()
        {
            TestSetup.Cleanup();
        }

        #region LegacyInitCompatShim
        [TestMethod]
        [DoNotParallelize]
        public void LegacyInitCompatShim()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            string libPath = TestSetup.GetNativeLibPath(TestNativeAbi.UpstreamCdecl);
            string libDir = Path.GetDirectoryName(libPath);
            string newLibPath = Path.Combine(libDir, "zlibwapi.dll");
            Console.WriteLine($"First try libPath (DOES NOT EXIST): {newLibPath}");
            Console.WriteLine($"Second try libPath (DOES EXIST: {libPath}");

            ZLibInit.TryGlobalCleanup();

            // Supress Obsolete warning for compat shim testing
#pragma warning disable CS0618
            ZLibInit.GlobalInit(newLibPath);
#pragma warning restore CS0618

            Console.WriteLine(ZLibInit.VersionString());
        }
        #endregion
    }
}
