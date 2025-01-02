/*
    C# tests by Hajin Jang
    Copyright (C) 2017-present Hajin Jang

    zlib license

    This software is provided 'as-is', without any express or implied
    warranty.  In no event will the authors be held liable for any damages
    arising from the use of this software.

    Permission is granted to anyone to use this software for any purpose,
    including commercial applications, and to alter it and redistribute it
    freely, subject to the following restrictions:

    1. The origin of this software must not be misrepresented; you must not
       claim that you wrote the original software. If you use this software
       in a product, an acknowledgment in the product documentation would be
       appreciated but is not required.
    2. Altered source versions must be plainly marked as such, and must not be
       misrepresented as being the original software.
    3. This notice may not be removed or altered from any source distribution.
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Joveler.Compression.ZLib.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class DeflateStreamUpCdeclTests : DeflateStreamTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.UpstreamCdecl;
    }

    [TestClass]
    [DoNotParallelize]
    public class DeflateStreamUpStdcallTests : DeflateStreamTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.UpstreamStdcall;
    }

    [TestClass]
    [DoNotParallelize]
    public class DeflateStreamNgCdeclTests : DeflateStreamTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.ZLibNgCdecl;
    }

    #region DeflateStreamTestsBase
    public abstract class DeflateStreamTestsBase : ZLibTestBase
    {
        #region Compress
        [TestMethod]
        public void Compress()
        {
            CompressTemplate("ex1.jpg", ZLibCompLevel.Default, threads: -1, useSpan: false);
            CompressTemplate("ex2.jpg", ZLibCompLevel.BestCompression, threads: -1, useSpan: false);
            CompressTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, threads: -1, useSpan: false);
            CompressTemplate("C.bin", ZLibCompLevel.Level7, threads: -1, useSpan: false);
        }

        [TestMethod]
        public void CompressSpan()
        {
            CompressTemplate("ex1.jpg", ZLibCompLevel.Default, threads: -1, useSpan: true);
            CompressTemplate("ex2.jpg", ZLibCompLevel.BestCompression, threads: -1, useSpan: true);
            CompressTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, threads: -1, useSpan: true);
            CompressTemplate("C.bin", ZLibCompLevel.Level7, threads: -1, useSpan: true);
        }

        [TestMethod]
        public void CompressParallel()
        {
            CompressTemplate("ex1.jpg", ZLibCompLevel.Default, threads: 2, useSpan: false);
            CompressTemplate("ex2.jpg", ZLibCompLevel.BestCompression, threads: 1, useSpan: false);
            CompressTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, threads: 3, useSpan: false);
            CompressTemplate("C.bin", ZLibCompLevel.Level7, threads: 4, useSpan: false);
        }

        private static void CompressTemplate(string sampleFileName, ZLibCompLevel level, int threads, bool useSpan)
        {
            byte[] originDigest;
            byte[] decompDigest;

            string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);
            using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream compMs = new MemoryStream())
            using (MemoryStream decompMs = new MemoryStream())
            {
                DeflateStream zs;
                if (threads < 0)
                {
                    ZLibCompressOptions compOpts = new ZLibCompressOptions()
                    {
                        Level = level,
                        LeaveOpen = true,
                    };
                    zs = new DeflateStream(compMs, compOpts);
                }
                else
                {
                    ZLibParallelCompressOptions pcompOpts = new ZLibParallelCompressOptions()
                    {
                        Level = level,
                        LeaveOpen = true,
                        Threads = threads,
                    };
                    zs = new DeflateStream(compMs, pcompOpts);
                }

                using (zs)
                {
#if !NETFRAMEWORK
                    if (useSpan)
                    {
                        byte[] buffer = new byte[64 * 1024];
                        int bytesRead;
                        do
                        {
                            bytesRead = sampleFs.Read(buffer.AsSpan());
                            zs.Write(buffer.AsSpan(0, bytesRead));
                        } while (0 < bytesRead);
                    }
                    else
#endif
                    {
                        sampleFs.CopyTo(zs);
                    }
                    
                    if (threads < 0)
                    {
                        zs.Flush();

                        Console.WriteLine($"[RAW]        expected=[{sampleFs.Length,7}] actual=[{zs.TotalIn,7}]");

                        Assert.AreEqual(sampleFs.Length, zs.TotalIn);
                    }
                }

                if (0 <= threads)
                {
                    Console.WriteLine($"[RAW]        {sampleFs.Length,7}");
                    Console.WriteLine($"[Compressed] {compMs.Length,7}");
                }
                

                // Decompress with BCL DeflateStream
                compMs.Position = 0;
                using (System.IO.Compression.DeflateStream bclStream = new System.IO.Compression.DeflateStream(compMs, CompressionMode.Decompress))
                {
                    bclStream.CopyTo(decompMs);
                }

                sampleFs.Position = 0;
                originDigest = TestHelper.SHA256Digest(sampleFs);

                decompMs.Position = 0;
                decompDigest = TestHelper.SHA256Digest(decompMs);
            }

            Assert.IsTrue(originDigest.SequenceEqual(decompDigest));
        }
        #endregion

        #region Decompress
        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Decompress()
        {
            DecompressTemplate("ex1.jpg", false);
            DecompressTemplate("ex2.jpg", false);
            DecompressTemplate("ex3.jpg", false);
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void DecompressSpan()
        {
            DecompressTemplate("ex1.jpg", true);
            DecompressTemplate("ex2.jpg", true);
            DecompressTemplate("ex3.jpg", true);
        }

        private static void DecompressTemplate(string sampleFileName, bool useSpan)
        {
            string compPath = Path.Combine(TestSetup.SampleDir, sampleFileName + ".deflate");
            string decompPath = Path.Combine(TestSetup.SampleDir, sampleFileName);

            ZLibDecompressOptions decompOpts = new ZLibDecompressOptions();

            using (MemoryStream decompMs = new MemoryStream())
            using (FileStream decompFs = new FileStream(decompPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream compFs = new FileStream(compPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (DeflateStream zs = new DeflateStream(compFs, decompOpts))
                {
#if !NETFRAMEWORK
                    if (useSpan)
                    {
                        byte[] buffer = new byte[64 * 1024];
                        int bytesRead;
                        do
                        {
                            bytesRead = zs.Read(buffer.AsSpan());
                            decompMs.Write(buffer.AsSpan(0, bytesRead));
                        } while (0 < bytesRead);
                    }
                    else
#endif
                    {
                        zs.CopyTo(decompMs);
                    }
                }

                decompMs.Position = 0;

                // Compare SHA256 Digest
                byte[] decompDigest = TestHelper.SHA256Digest(decompMs);
                byte[] fileDigest = TestHelper.SHA256Digest(decompFs);
                Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
            }
        }
        #endregion
    }
    #endregion
}
