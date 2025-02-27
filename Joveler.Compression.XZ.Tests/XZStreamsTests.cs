﻿/*
    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2023 Hajin Jang

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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Joveler.Compression.XZ.Tests
{
    [TestClass]
    [TestCategory("Joveler.Compression.XZ")]
    public class XZStreamsTests
    {
        #region XZ Compress
        [TestMethod]
        public void XZCompressSingle()
        {
            XZCompressTemplate("A.pdf", false, true, -1, LzmaCompLevel.Level7, false);
            XZCompressTemplate("B.txt", false, true, -1, LzmaCompLevel.Default, false);
            XZCompressTemplate("C.bin", false, true, -1, LzmaCompLevel.Level1, true);
            XZCompressTemplate("C.bin", false, false, -1, (LzmaCompLevel)255, false);
        }

        [TestMethod]
        public void XZCompressSingleSpan()
        {
            XZCompressTemplate("A.pdf", true, true, -1, LzmaCompLevel.Level7, false);
            XZCompressTemplate("B.txt", true, true, -1, LzmaCompLevel.Default, false);
            XZCompressTemplate("C.bin", true, true, -1, LzmaCompLevel.Level1, true);
            XZCompressTemplate("C.bin", true, false, -1, (LzmaCompLevel)255, false);
        }

        [TestMethod]
        public void XZCompressMulti()
        {
            int maxThreads = Environment.ProcessorCount;
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                case Architecture.Arm:
                    maxThreads = 2;
                    break;
            }
            XZCompressTemplate("A.pdf", false, true, 1, LzmaCompLevel.Level7, false);
            XZCompressTemplate("A.pdf", false, true, 2, LzmaCompLevel.Default, false);
            XZCompressTemplate("B.txt", false, true, 2, LzmaCompLevel.Level3, true);
            XZCompressTemplate("C.bin", false, true, maxThreads, LzmaCompLevel.Level1, false);
            XZCompressTemplate("C.bin", false, false, maxThreads, (LzmaCompLevel)255, false);
        }

        private static void XZCompressTemplate(string sampleFileName, bool useSpan, bool success, int threads, LzmaCompLevel level, bool extreme)
        {
            string destDir = Path.GetTempFileName();
            File.Delete(destDir);
            Directory.CreateDirectory(destDir);
            try
            {
                string tempDecompFile = Path.Combine(destDir, Path.GetFileName(sampleFileName));
                string tempXzFile = tempDecompFile + ".xz";

                XZCompressOptions compOpts = new XZCompressOptions
                {
                    Level = level,
                    ExtremeFlag = extreme,
                    LeaveOpen = true,
                };

                string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);

                using (FileStream xzCompFs = new FileStream(tempXzFile, FileMode.Create, FileAccess.Write, FileShare.None))
                using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    XZStream xzs = null;
                    try
                    {
                        if (threads == -1)
                        { // Single-thread compression
                            xzs = new XZStream(xzCompFs, compOpts);
                        }
                        else if (0 < threads)
                        { // Multi-thread compression
                            XZParallelCompressOptions threadOpts = new XZParallelCompressOptions
                            {
                                Threads = threads,
                            };
                            xzs = new XZStream(xzCompFs, compOpts, threadOpts);
                        }
                        else
                        {
                            Assert.Fail($"threads [{threads}] is not a valid test value.");
                        }

#if !NETFRAMEWORK
                        if (useSpan)
                        {
                            byte[] buffer = new byte[64 * 1024];

                            int bytesRead;
                            do
                            {
                                bytesRead = sampleFs.Read(buffer.AsSpan());
                                xzs.Write(buffer.AsSpan(0, bytesRead));
                            } while (0 < bytesRead);
                        }
                        else
#endif
                        {
                            sampleFs.CopyTo(xzs);
                        }

                        xzs.Flush();
                        xzs.GetProgress(out ulong finalIn, out ulong finalOut);

                        Assert.AreEqual(sampleFs.Length, xzs.TotalIn);
                        Assert.AreEqual(xzCompFs.Length, xzs.TotalOut);
                        Assert.AreEqual((ulong)sampleFs.Length, finalIn);
                        Assert.AreEqual((ulong)xzCompFs.Length, finalOut);
                    }
                    finally
                    {
                        xzs?.Dispose();
                        xzs = null;
                    }
                }

                Assert.IsTrue(TestHelper.RunXZ(tempXzFile) == 0);

                byte[] decompDigest;
                byte[] originDigest;
                using (FileStream fs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (HashAlgorithm hash = SHA256.Create())
                    {
                        originDigest = hash.ComputeHash(fs);
                    }
                }

                using (FileStream fs = new FileStream(tempDecompFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (HashAlgorithm hash = SHA256.Create())
                    {
                        decompDigest = hash.ComputeHash(fs);
                    }
                }

                Assert.IsTrue(originDigest.SequenceEqual(decompDigest));
                Assert.IsTrue(success);
            }
            catch (Exception)
            {
                Assert.IsFalse(success);
            }
            finally
            {
                if (Directory.Exists(destDir))
                    Directory.Delete(destDir, true);
            }
        }
        #endregion

        #region XZ Decompress
        [TestMethod]
        public void XZDecompressSingle()
        {
            XZDecompressTemplate("A.xz", "A.pdf", -1, false);
            XZDecompressTemplate("B9.xz", "B.txt", -1, false);
            XZDecompressTemplate("B1.xz", "B.txt", -1, false);
            XZDecompressTemplate("C.xz", "C.bin", -1, false);
        }

        [TestMethod]
        public void XZDecompressSingleSpan()
        {
            XZDecompressTemplate("A.xz", "A.pdf", -1, true);
            XZDecompressTemplate("B9.xz", "B.txt", -1, true);
            XZDecompressTemplate("B1.xz", "B.txt", -1, true);
            XZDecompressTemplate("C.xz", "C.bin", -1, true);
        }

        [TestMethod]
        public void XZDecompressMulti()
        {
            XZDecompressTemplate("A_mt16.xz", "A.pdf", 1, true);
            XZDecompressTemplate("B9_mt16.xz", "B.txt", 2, true);
            XZDecompressTemplate("B1_mt16.xz", "B.txt", Environment.ProcessorCount, true);
            XZDecompressTemplate("C_mt16.xz", "C.bin", Environment.ProcessorCount, true);
        }

        private static void XZDecompressTemplate(string xzFileName, string originFileName, int threads, bool useSpan)
        {
            byte[] decompDigest;
            byte[] originDigest;

            string xzFile = Path.Combine(TestSetup.SampleDir, xzFileName);
            string originFile = Path.Combine(TestSetup.SampleDir, originFileName);
            using (MemoryStream decompMs = new MemoryStream())
            {
                XZDecompressOptions decompOpts = new XZDecompressOptions();

                XZStream xzs = null;
                try
                {
                    using (FileStream compFs = new FileStream(xzFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (threads == -1)
                        { // Single-thread compression
                            xzs = new XZStream(compFs, decompOpts);
                        }
                        else if (0 < threads)
                        { // Multi-thread compression
                            XZParallelDecompressOptions threadOpts = new XZParallelDecompressOptions
                            {
                                Threads = threads,
                            };
                            xzs = new XZStream(compFs, decompOpts, threadOpts);
                        }
                        else
                        {
                            Assert.Fail($"threads [{threads}] is not a valid test value.");
                        }

#if !NETFRAMEWORK
                        if (useSpan)
                        {
                            byte[] buffer = new byte[64 * 1024];

                            int bytesRead;
                            do
                            {
                                bytesRead = xzs.Read(buffer.AsSpan());
                                decompMs.Write(buffer.AsSpan(0, bytesRead));
                            } while (0 < bytesRead);
                        }
                        else
#endif
                        {
                            xzs.CopyTo(decompMs);
                        }

#if LZMA_MEM_ENABLE
                        ulong memUsage = xzs.GetDecompresMemUsage();
                        Console.WriteLine($"{xzFileName} ({threads}t) MEM: requires {memUsage / (1024 * 1024) + 1}MB ({memUsage}B)");
#endif

                        decompMs.Flush();
                        xzs.GetProgress(out ulong finalIn, out ulong finalOut);

                        Assert.AreEqual(compFs.Length, xzs.TotalIn);
                        Assert.AreEqual(decompMs.Length, xzs.TotalOut);
                        Assert.AreEqual((ulong)compFs.Length, finalIn);
                        Assert.AreEqual((ulong)decompMs.Length, finalOut);
                    }
                }
                finally
                {
                    xzs?.Dispose();
                    xzs = null;
                }

                decompMs.Position = 0;

                using (HashAlgorithm hash = SHA256.Create())
                {
                    decompDigest = hash.ComputeHash(decompMs);
                }
            }

            using (FileStream originFs = new FileStream(originFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (HashAlgorithm hash = SHA256.Create())
                {
                    originDigest = hash.ComputeHash(originFs);
                }
            }

            Assert.IsTrue(decompDigest.SequenceEqual(originDigest));
        }
        #endregion

        #region Legacy LZMA Decompress
        [TestMethod]
        public void LzmaAloneDecompressSingle()
        {
            LzmaAloneDecompressTemplate("A.lzma", "A.pdf");
            LzmaAloneDecompressTemplate("B9.lzma", "B.txt");
            LzmaAloneDecompressTemplate("B1.lzma", "B.txt");
            LzmaAloneDecompressTemplate("C.lzma", "C.bin");
        }

        private static void LzmaAloneDecompressTemplate(string lzmaFileName, string originFileName)
        {
            byte[] decompDigest;
            byte[] originDigest;

            string xzFile = Path.Combine(TestSetup.SampleDir, lzmaFileName);
            string originFile = Path.Combine(TestSetup.SampleDir, originFileName);
            using (MemoryStream decompMs = new MemoryStream())
            {
                XZDecompressOptions decompOpts = new XZDecompressOptions();

                using (FileStream compFs = new FileStream(xzFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (LzmaAloneStream lzs = new LzmaAloneStream(compFs, decompOpts))
                {
                    lzs.CopyTo(decompMs);

                    decompMs.Flush();
                    lzs.GetProgress(out ulong finalIn, out ulong finalOut);

                    Assert.AreEqual(compFs.Length, lzs.TotalIn);
                    Assert.AreEqual(decompMs.Length, lzs.TotalOut);
                    Assert.AreEqual((ulong)compFs.Length, finalIn);
                    Assert.AreEqual((ulong)decompMs.Length, finalOut);
                }

                decompMs.Position = 0;

                using (HashAlgorithm hash = SHA256.Create())
                {
                    decompDigest = hash.ComputeHash(decompMs);
                }
            }

            using (FileStream originFs = new FileStream(originFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (HashAlgorithm hash = SHA256.Create())
                {
                    originDigest = hash.ComputeHash(originFs);
                }
            }

            Assert.IsTrue(decompDigest.SequenceEqual(originDigest));
        }
        #endregion

        #region LZip Decompress
        [TestMethod]
        public void LZipDecompressSingle()
        {
            LZipDecompressTemplate("A.lz", "A.pdf");
            LZipDecompressTemplate("B9.lz", "B.txt");
            LZipDecompressTemplate("B1.lz", "B.txt");
            LZipDecompressTemplate("C.lz", "C.bin");
        }

        private static void LZipDecompressTemplate(string lzmaFileName, string originFileName)
        {
            byte[] decompDigest;
            byte[] originDigest;

            string xzFile = Path.Combine(TestSetup.SampleDir, lzmaFileName);
            string originFile = Path.Combine(TestSetup.SampleDir, originFileName);
            using (MemoryStream decompMs = new MemoryStream())
            {
                XZDecompressOptions decompOpts = new XZDecompressOptions();

                using (FileStream compFs = new FileStream(xzFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (LZipStream lzs = new LZipStream(compFs, decompOpts))
                {
                    lzs.CopyTo(decompMs);

                    decompMs.Flush();
                    lzs.GetProgress(out ulong finalIn, out ulong finalOut);

                    Assert.AreEqual(compFs.Length, lzs.TotalIn);
                    Assert.AreEqual(decompMs.Length, lzs.TotalOut);
                    Assert.AreEqual((ulong)compFs.Length, finalIn);
                    Assert.AreEqual((ulong)decompMs.Length, finalOut);
                }

                decompMs.Position = 0;

                using (HashAlgorithm hash = SHA256.Create())
                {
                    decompDigest = hash.ComputeHash(decompMs);
                }
            }

            using (FileStream originFs = new FileStream(originFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (HashAlgorithm hash = SHA256.Create())
                {
                    originDigest = hash.ComputeHash(originFs);
                }
            }

            Assert.IsTrue(decompDigest.SequenceEqual(originDigest));
        }
        #endregion

        #region Auto Decompress
        [TestMethod]
        public void AutoDecompressSingle()
        {
            AutoDecompressTemplate("A.lzma", "A.pdf");
            AutoDecompressTemplate("B9.xz", "B.txt");
            AutoDecompressTemplate("B1_mt16.xz", "B.txt");
            AutoDecompressTemplate("C.lz", "C.bin");
        }

        private static void AutoDecompressTemplate(string lzmaFileName, string originFileName)
        {
            byte[] decompDigest;
            byte[] originDigest;

            string xzFile = Path.Combine(TestSetup.SampleDir, lzmaFileName);
            string originFile = Path.Combine(TestSetup.SampleDir, originFileName);
            using (MemoryStream decompMs = new MemoryStream())
            {
                XZDecompressOptions decompOpts = new XZDecompressOptions();

                using (FileStream compFs = new FileStream(xzFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (LzmaAutoStream lzs = new LzmaAutoStream(compFs, decompOpts))
                {
                    lzs.CopyTo(decompMs);

                    decompMs.Flush();
                    lzs.GetProgress(out ulong finalIn, out ulong finalOut);

                    Assert.AreEqual(compFs.Length, lzs.TotalIn);
                    Assert.AreEqual(decompMs.Length, lzs.TotalOut);
                    Assert.AreEqual((ulong)compFs.Length, finalIn);
                    Assert.AreEqual((ulong)decompMs.Length, finalOut);
                }

                decompMs.Position = 0;

                using (HashAlgorithm hash = SHA256.Create())
                {
                    decompDigest = hash.ComputeHash(decompMs);
                }
            }

            using (FileStream originFs = new FileStream(originFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (HashAlgorithm hash = SHA256.Create())
                {
                    originDigest = hash.ComputeHash(originFs);
                }
            }

            Assert.IsTrue(decompDigest.SequenceEqual(originDigest));
        }
        #endregion

        #region Abort Compress
        [TestMethod]
        public void AbortCompress()
        {
            AbortCompressTemplate("A.pdf", -1);
            AbortCompressTemplate("B.txt", -1);
            AbortCompressTemplate("C.bin", -1);

            AbortCompressTemplate("A.pdf", 1);
            AbortCompressTemplate("B.txt", 2);
            AbortCompressTemplate("C.bin", 2);
        }

        private static void AbortCompressTemplate(string sampleFileName, int threads)
        {
            XZCompressOptions compOpts = new XZCompressOptions
            {
                Level = LzmaCompLevel.Default,
            };

            string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);

            foreach (bool doAbort in new bool[] { false, true })
            {
                using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream rms = new MemoryStream())
                {
                    XZStream xzs = null;
                    try
                    {
                        if (threads == -1)
                        { // Single-thread compression
                            xzs = new XZStream(rms, compOpts);
                        }
                        else if (0 < threads)
                        { // Multi-thread compression
                            XZParallelCompressOptions threadOpts = new XZParallelCompressOptions
                            {
                                Threads = threads,
                            };
                            xzs = new XZStream(rms, compOpts, threadOpts);
                        }
                        else
                        {
                            Assert.Fail($"threads [{threads}] is not a valid test value.");
                        }

                        sampleFs.CopyTo(xzs);

                        DateTime before = DateTime.Now;
                        if (doAbort)
                            xzs.Abort();
                        else
                            xzs.Close();
                        DateTime after = DateTime.Now;
                        TimeSpan abortElapsed = after - before;
                        Console.WriteLine($"{sampleFileName}, {threads} = {(doAbort ? "Abort" : "Close")}() took {abortElapsed.TotalMilliseconds:0.000}ms");

                        if (doAbort == false)
                            continue;

                        // Internal xz resources are now freed. Every compress operation will fail.
                        sampleFs.Position = 0;
                        bool hadThrown = false;
                        try
                        {
                            sampleFs.CopyTo(xzs);
                        }
                        catch (XZException e)
                        {
                            Assert.AreEqual(LzmaRet.ProgError, e.ReturnCode);
                            hadThrown = true;
                        }
                        Assert.IsTrue(hadThrown);
                    }
                    finally
                    {
                        xzs?.Dispose();
                        xzs = null;
                    }
                }
            }
        }
        #endregion

        #region Abort Decompress
        [TestMethod]
        public void AbortDecompress()
        {
            AbortDecompressTemplate("A.xz", -1);
            AbortDecompressTemplate("B9.xz", -1);
            AbortDecompressTemplate("C.xz", -1);

            AbortDecompressTemplate("A_mt16.xz", 1);
            AbortDecompressTemplate("B1_mt16.xz", 2);
            AbortDecompressTemplate("C.xz", 2);
        }

        private static void AbortDecompressTemplate(string sampleFileName, int threads)
        {
            string xzFile = Path.Combine(TestSetup.SampleDir, sampleFileName);

            XZDecompressOptions decompOpts = new XZDecompressOptions();

            foreach (bool doAbort in new bool[] { false, true })
            {
                XZStream xzs = null;
                try
                {
                    using (FileStream compFs = new FileStream(xzFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (threads == -1)
                        { // Single-thread compression
                            xzs = new XZStream(compFs, decompOpts);
                        }
                        else if (0 < threads)
                        { // Multi-thread compression
                            XZParallelDecompressOptions threadOpts = new XZParallelDecompressOptions
                            {
                                Threads = threads,
                            };
                            xzs = new XZStream(compFs, decompOpts, threadOpts);
                        }
                        else
                        {
                            Assert.Fail($"threads [{threads}] is not a valid test value.");
                        }

                        long firstReadLen = compFs.Length / 2;
                        byte[] firstBuffer = new byte[firstReadLen];
                        int bytesRead = xzs.Read(firstBuffer, 0, firstBuffer.Length);

                        DateTime before = DateTime.Now;
                        if (doAbort)
                            xzs.Abort();
                        else
                            xzs.Close();
                        DateTime after = DateTime.Now;
                        TimeSpan abortElapsed = after - before;
                        Console.WriteLine($"{sampleFileName}, {threads} = {(doAbort ? "Abort" : "Close")}() took {abortElapsed.TotalMilliseconds:0.000}ms");

                        if (doAbort == false)
                            continue;

                        // Internal xz resources are now freed. Every decompress operation will fail.
                        bool hadThrown = false;
                        try
                        {
                            byte[] buffer = new byte[64 * 1024];
                            do
                            {
                                bytesRead = xzs.Read(buffer, 0, buffer.Length);
                            } while (0 < bytesRead);
                        }
                        catch (XZException e)
                        {
                            Assert.AreEqual(LzmaRet.ProgError, e.ReturnCode);
                            hadThrown = true;
                        }
                        Assert.IsTrue(hadThrown);
                    }
                }
                finally
                {
                    xzs?.Dispose();
                    xzs = null;
                }
                Assert.IsNull(xzs);
            }


        }
        #endregion
    }
}
