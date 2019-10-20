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

            string verStr = XZInit.VersionString();
            Console.WriteLine($"liblzma Version (String)  = {verStr}");
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
        private void EncoderMemUsageTemplate(LzmaCompLevel level, bool extreme)
        {
            void PrintMemUsage(ulong usage, int threads = 0)
            {
                char extremeChar = extreme ? 'e' : ' ';
                uint purePreset = (uint)level;
                string msg;
                if (threads == 0)
                    msg = $"Encoder Mem Usage (p{purePreset}{extremeChar}) = {usage / (1024 * 1024) + 1}MB ({usage}B)";
                else
                    msg = $"Encoder Mem Usage (p{purePreset}{extremeChar}, {threads}T) = {usage / (1024 * 1024) + 1}MB ({usage}B)";
                Console.WriteLine(msg);
            }

            ulong single = XZInit.EncoderMemUsage(level, extreme);
            ulong multi1 = XZInit.EncoderMultiMemUsage(level, extreme, 1);
            ulong multi2 = XZInit.EncoderMultiMemUsage(level, extreme, 2);
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
            EncoderMemUsageTemplate(LzmaCompLevel.Level0, false);
            EncoderMemUsageTemplate(LzmaCompLevel.Level0, true);
            EncoderMemUsageTemplate(LzmaCompLevel.Default, false);
            EncoderMemUsageTemplate(LzmaCompLevel.Default, true);
            EncoderMemUsageTemplate(LzmaCompLevel.Level9, false);
            EncoderMemUsageTemplate(LzmaCompLevel.Level9, true);
        }
        #endregion

        #region DecoderMemUsage
        private void DecoderMemUsageTemplate(LzmaCompLevel level, bool extreme)
        {
            void PrintMemUsage(ulong usage)
            {
                char extremeChar = extreme ? 'e' : ' ';
                uint purePreset = (uint)level;
                Console.WriteLine($"Decoder Mem Usage (p{purePreset}{extremeChar}) = {usage / (1024 * 1024) + 1}MB ({usage}B)");
            }

            ulong usage = XZInit.DecoderMemUsage(level, extreme);
            PrintMemUsage(usage);
            Assert.AreNotEqual(ulong.MaxValue, usage);
        }

        [TestMethod]
        public void DecoderMemUsage()
        {
            DecoderMemUsageTemplate(LzmaCompLevel.Level0, false);
            DecoderMemUsageTemplate(LzmaCompLevel.Level0, true);
            DecoderMemUsageTemplate(LzmaCompLevel.Default, false);
            DecoderMemUsageTemplate(LzmaCompLevel.Default, true);
            DecoderMemUsageTemplate(LzmaCompLevel.Level9, false);
            DecoderMemUsageTemplate(LzmaCompLevel.Level9, true);
        }
        #endregion
    }
}
