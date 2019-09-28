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

        #region EncoderMemUsage
        private void EncoderMemUsageTemplate(uint preset)
        {
            void PrintMemUsage(ulong usage, int threads = 0)
            {
                char isExtreme = (preset & XZStream.ExtremeFlag) > 0 ? 'e' : ' ';
                uint purePreset = preset & ~XZStream.ExtremeFlag;
                string msg;
                if (threads == 0)
                    msg = $"Encoder Mem Usage (p{purePreset}{isExtreme}) = {usage / (1024 * 1024) + 1}MB ({usage}B)";
                else
                    msg = $"Encoder Mem Usage (p{purePreset}{isExtreme}, {threads}T) = {usage / (1024 * 1024) + 1}MB ({usage}B)";
                Console.WriteLine(msg);
            }

            ulong single = XZInit.EncoderMemUsage(preset);
            ulong multi1 = XZInit.EncoderMultiMemUsage(preset, 1);
            ulong multi2 = XZInit.EncoderMultiMemUsage(preset, 2);
            PrintMemUsage(single);
            PrintMemUsage(multi1, 1);
            PrintMemUsage(multi2, 2);

            Assert.AreNotEqual(ulong.MaxValue, single);
            Assert.AreNotEqual(ulong.MaxValue, multi1);
            Assert.AreNotEqual(ulong.MaxValue, multi2);
            Assert.IsTrue(single < multi1);
        }

        [TestMethod]
        public void EncoderMemUsage()
        {
            EncoderMemUsageTemplate(XZStream.MinimumPreset);
            EncoderMemUsageTemplate(XZStream.MinimumPreset | XZStream.ExtremeFlag);
            EncoderMemUsageTemplate(XZStream.DefaultPreset);
            EncoderMemUsageTemplate(XZStream.DefaultPreset | XZStream.ExtremeFlag);
            EncoderMemUsageTemplate(XZStream.MaximumPreset);
            EncoderMemUsageTemplate(XZStream.MaximumPreset | XZStream.ExtremeFlag);
        }
        #endregion

        #region DecoderMemUsage
        private void DecoderMemUsageTemplate(uint preset)
        {
            void PrintMemUsage(ulong usage, int threads = 0)
            {
                char isExtreme = (preset & XZStream.ExtremeFlag) > 0 ? 'e' : ' ';
                uint purePreset = preset & ~XZStream.ExtremeFlag;
                Console.WriteLine($"Decoder Mem Usage (p{purePreset}{isExtreme}) = {usage / (1024 * 1024) + 1}MB ({usage}B)");
            }

            ulong usage = XZInit.DecoderMemUsage(preset);
            PrintMemUsage(usage);
            Assert.AreNotEqual(ulong.MaxValue, usage);
        }

        [TestMethod]
        public void DecoderMemUsage()
        {
            DecoderMemUsageTemplate(XZStream.MinimumPreset);
            DecoderMemUsageTemplate(XZStream.MinimumPreset | XZStream.ExtremeFlag);
            DecoderMemUsageTemplate(XZStream.DefaultPreset);
            DecoderMemUsageTemplate(XZStream.DefaultPreset | XZStream.ExtremeFlag);
            DecoderMemUsageTemplate(XZStream.MaximumPreset);
            DecoderMemUsageTemplate(XZStream.MaximumPreset | XZStream.ExtremeFlag);
        }
        #endregion
    }
}
