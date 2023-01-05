/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2020 Hajin Jang

    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Joveler.Compression.XZ.Tests
{
    [TestClass]
    [TestCategory("Joveler.Compression.XZ")]
    public class XZInitTests
    {
        [TestMethod]
        public void Version()
        {
            Version verInst = XZInit.Version();
            Console.WriteLine($"liblzma Version (Version) = {verInst}");

            string verStr = XZInit.VersionString();
            Console.WriteLine($"liblzma Version (String)  = {verStr}");
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
            void PrintMemUsage(ulong memUsage)
            {
                char extremeChar = extreme ? 'e' : ' ';
                uint purePreset = (uint)level;
                Console.WriteLine($"Decoder Mem Usage (p{purePreset}{extremeChar}) = {memUsage / (1024 * 1024) + 1}MB ({memUsage}B)");
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
