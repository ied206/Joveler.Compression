/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2019 Hajin Jang

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
            CompressTemplate("A.pdf", false, 1, 7);
            CompressTemplate("B.txt", false, 1, XZStream.DefaultPreset);
            CompressTemplate("C.bin", false, 1, 1);
        }

        [TestMethod]
        public void CompressSpan()
        {
            CompressTemplate("A.pdf", true, 1, 7);
            CompressTemplate("B.txt", true, 1, XZStream.DefaultPreset);
            CompressTemplate("C.bin", true, 1, 1);
        }

        [TestMethod]
        public void CompressMulti()
        {
            CompressTemplate("A.pdf", false, 2, 7);
            CompressTemplate("B.txt", false, 2, 3);
            CompressTemplate("C.bin", false, Environment.ProcessorCount, 1);
        }

        private static void CompressTemplate(string sampleFileName, bool useSpan, int threads, uint preset)
        {
            string destDir = Path.GetTempFileName();
            File.Delete(destDir);
            Directory.CreateDirectory(destDir);
            try
            {
                string tempDecompFile = Path.Combine(destDir, Path.GetFileName(sampleFileName));
                string tempXzFile = tempDecompFile + ".xz";

                string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);
                using (FileStream xzCompFs = new FileStream(tempXzFile, FileMode.Create, FileAccess.Write, FileShare.None))
                using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (XZStream xzs = new XZStream(xzCompFs, LzmaMode.Compress, preset, threads, true))
                {
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
            Template("A.xz", "A.pdf", false);
            Template("B9.xz", "B.txt", false);
            Template("B1.xz", "B.txt", false);
            Template("C.xz", "C.bin", false);
        }

        [TestMethod]
        public void DecompressSpan()
        {
            Template("A.xz", "A.pdf", true);
            Template("B9.xz", "B.txt", true);
            Template("B1.xz", "B.txt", true);
            Template("C.xz", "C.bin", true);
        }

        private static void Template(string xzFileName, string originFileName, bool useSpan)
        {
            byte[] decompDigest;
            byte[] originDigest;

            string xzFile = Path.Combine(TestSetup.SampleDir, xzFileName);
            string originFile = Path.Combine(TestSetup.SampleDir, originFileName);
            using (MemoryStream decompMs = new MemoryStream())
            {
                using (FileStream compFs = new FileStream(xzFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (XZStream xz = new XZStream(compFs, LzmaMode.Decompress))
                {
                    if (useSpan)
                    {
                        byte[] buffer = new byte[64 * 1024];

                        int bytesRead;
                        do
                        {
                            bytesRead = xz.Read(buffer.AsSpan());
                            decompMs.Write(buffer.AsSpan(0, bytesRead));
                        } while (0 < bytesRead);
                    }
                    else
                    {
                        xz.CopyTo(decompMs);
                    }

                    decompMs.Flush();
                    xz.GetProgress(out ulong finalIn, out ulong finalOut);

                    Assert.AreEqual(compFs.Length, xz.TotalIn);
                    Assert.AreEqual(decompMs.Length, xz.TotalOut);
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

        #region GetProgress
        #endregion
    }
}
