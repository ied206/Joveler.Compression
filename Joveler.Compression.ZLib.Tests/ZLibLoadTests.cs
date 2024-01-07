using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.Compression.ZLib.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ZLibLoadTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        { // In this class, all tests requires that zlib native libraries must not be loaded.
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

            try
            {
                string libPath = TestSetup.GetNativeLibPath(TestNativeAbi.UpstreamCdecl);
                string libDir = Path.GetDirectoryName(libPath);
                string newLibPath = Path.Combine(libDir, "zlibwapi.dll");
                Console.WriteLine($"First try libPath (DOES NOT EXIST): {newLibPath}");
                Console.WriteLine($"Second try libPath (DOES EXIST: {libPath}");

                // Supress Obsolete warning for compat shim testing
#pragma warning disable CS0618
                ZLibInit.GlobalInit(newLibPath);
#pragma warning restore CS0618

                Console.WriteLine(ZLibInit.VersionString());
            }
            finally
            {
                ZLibInit.TryGlobalCleanup();
            }
        }
        #endregion
    }
}
