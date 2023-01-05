using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joveler.Compression.XZ.Tests
{
    [TestClass]
    [TestCategory("Joveler.Compression.XZ")]
    internal class XZHardwareTests
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
