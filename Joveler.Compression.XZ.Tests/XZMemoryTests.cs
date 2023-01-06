﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Joveler.Compression.XZ.Tests
{
    [TestClass]
    [TestCategory("Joveler.Compression.XZ")]
    public class XZMemoryTests
    {
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

            ulong single = XZMemory.EncoderMemUsage(level, extreme);
            ulong multi1 = XZMemory.ThreadedEncoderMemUsage(level, extreme, 1);
            ulong multi2 = XZMemory.ThreadedEncoderMemUsage(level, extreme, 2);
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
            void PrintMemUsage(ulong memUsage)
            {
                char extremeChar = extreme ? 'e' : ' ';
                uint purePreset = (uint)level;
                Console.WriteLine($"Decoder Mem Usage (p{purePreset}{extremeChar}) = {memUsage / (1024 * 1024) + 1}MB ({memUsage}B)");
            }

            ulong usage = XZMemory.DecoderMemUsage(level, extreme);
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