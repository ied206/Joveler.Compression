using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Joveler.Compression.XZ.Tests
{
    [TestClass]
    [TestCategory("Joveler.Compression.XZ")]
    public class XZInitTests
    {
        [TestMethod]
        public void Version()
        {
            Version expectVerInst = new Version(5, 2, 4, 2);
            Version verInst = XZInit.Version();
            Console.WriteLine($"liblzma Version (Version) = {verInst}");
            Assert.IsTrue(verInst.Equals(expectVerInst));

            string verStr = XZInit.VersionString();
            Console.WriteLine($"liblzma Version (String)  = {verStr}");
            Assert.IsTrue(verStr.Equals("5.2.4", StringComparison.Ordinal));
        }

        [TestMethod]
        public void PhysMem()
        {
            ulong physMem = XZInit.PhysMem();
            Console.WriteLine($"Hardware Physical Memory = {physMem}");
        }

        [TestMethod]
        public void CpuThreads()
        {
            uint xzCoreCount = XZInit.CpuThreads();
            uint bclCoreCount = (uint)Environment.ProcessorCount;
            Assert.AreEqual(bclCoreCount, xzCoreCount);
            Console.WriteLine($"Hardware CPU Threads = {xzCoreCount}");
        }
    }
}
