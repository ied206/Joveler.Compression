using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
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

        private void EncoderMemUsageTemplate(uint preset)
        {
            ulong mem1 = XZInit.EncoderMemUsage(preset);
            Assert.AreNotEqual(ulong.MaxValue, mem1);

            ulong mem2;
            using (MemoryStream ms = new MemoryStream())
            using (XZStream xzs = new XZStream(ms, LzmaMode.Compress, preset))
            {
                mem2 = xzs.MaxMemUsage;
            }
            Assert.AreNotEqual(ulong.MaxValue, mem2);

            Assert.AreEqual(mem2, mem1);

            char isExtreme = (preset & XZStream.ExtremeFlag) > 0 ? 'e' : ' ';
            preset &= ~XZStream.ExtremeFlag;
            Console.WriteLine($"Encoder Mem Usage (p{preset}{isExtreme}) = {mem1 / (1024 * 1024) + 1}MB");
                
        }

        [TestMethod]
        public void EncoderMemUsage()
        {
            EncoderMemUsageTemplate(XZStream.DefaultPreset | XZStream.ExtremeFlag);
            
        }
    }
}
