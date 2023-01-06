using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Joveler.Compression.XZ.Tests
{
    [TestClass]
    [TestCategory("Joveler.Compression.XZ")]
    public class XZHardwareTests
    {
        [TestMethod]
        public void PhysMem()
        {
            ulong physMem = XZHardware.PhysMem();
            Console.WriteLine($"Hardware Physical Memory = {physMem}");
        }

        [TestMethod]
        public void CpuThreads()
        {
            uint xzCoreCount = XZHardware.CpuThreads();
            uint bclCoreCount = (uint)Environment.ProcessorCount;
            Assert.AreEqual(bclCoreCount, xzCoreCount);
            Console.WriteLine($"Hardware CPU Threads = {xzCoreCount}");
        }
    }
}
