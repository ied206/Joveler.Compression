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
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Joveler.Compression.XZ.Tests
{
    [TestClass]
    [TestCategory("Joveler.Compression.XZ")]
    public class XZStreamTests
    {
        #region Compress
        [TestMethod]
        public void Compress()
        {
            CompressTemplate("A.pdf", false, true, -1, LzmaCompLevel.Level7, false);
            CompressTemplate("B.txt", false, true, -1, LzmaCompLevel.Default, false);
            CompressTemplate("C.bin", false, true, -1, LzmaCompLevel.Level1, true);
            CompressTemplate("C.bin", false, false, -1, (LzmaCompLevel)255, false);
        }

        [TestMethod]
        public void CompressSpan()
        {
            CompressTemplate("A.pdf", true, true, -1, LzmaCompLevel.Level7, false);
            CompressTemplate("B.txt", true, true, -1, LzmaCompLevel.Default, false);
            CompressTemplate("C.bin", true, true, -1, LzmaCompLevel.Level1, true);
            CompressTemplate("C.bin", true, false, -1, (LzmaCompLevel)255, false);
        }

        [TestMethod]
        public void CompressMulti()
        {
            CompressTemplate("A.pdf", false, true, 1, LzmaCompLevel.Level7, false);
            CompressTemplate("A.pdf", false, true, 2, LzmaCompLevel.Default, false);
            CompressTemplate("B.txt", false, true, 2, LzmaCompLevel.Level3, true);
            CompressTemplate("C.bin", false, true, Environment.ProcessorCount, LzmaCompLevel.Level1, false);
            CompressTemplate("C.bin", false, false, Environment.ProcessorCount, (LzmaCompLevel)255, false);
        }

        private static void CompressTemplate(string sampleFileName, bool useSpan, bool success, int threads, LzmaCompLevel level, bool extreme)
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
                            XZThreadedCompressOptions threadOpts = new XZThreadedCompressOptions
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

        #region Decompress
        [TestMethod]
        public void Decompress()
        {
            Template("A.xz", "A.pdf", -1, false);
            Template("B9.xz", "B.txt", -1, false);
            Template("B1.xz", "B.txt", -1, false);
            Template("C.xz", "C.bin", -1, false);
        }

        [TestMethod]
        public void DecompressSpan()
        {
            Template("A.xz", "A.pdf", -1, true);
            Template("B9.xz", "B.txt", -1, true);
            Template("B1.xz", "B.txt", -1, true);
            Template("C.xz", "C.bin", -1, true);
        }

        [TestMethod]
        public void DecompressMulti()
        {
            Template("A_mt16.xz", "A.pdf", 1, true);
            Template("B9_mt16.xz", "B.txt", 2, true);
            Template("B1_mt16.xz", "B.txt", Environment.ProcessorCount, true);
            Template("C_mt16.xz", "C.bin", Environment.ProcessorCount, true);
        }

        private static void Template(string xzFileName, string originFileName, int threads, bool useSpan)
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
                            XZThreadedDecompressOptions threadOpts = new XZThreadedDecompressOptions
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

        #region GetProgress
        #endregion
    }
}
