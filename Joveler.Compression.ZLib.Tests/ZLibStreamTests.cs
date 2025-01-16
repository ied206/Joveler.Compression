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
using System.Buffers;
using System.IO;
using System.Linq;

namespace Joveler.Compression.ZLib.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ZLibStreamUpCdeclTests : ZLibStreamTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.UpstreamCdecl;
    }

    [TestClass]
    [DoNotParallelize]
    public class ZLibStreamUpStdcallTests : ZLibStreamTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.UpstreamStdcall;
    }

    [TestClass]
    [DoNotParallelize]
    public class ZLibStreamNgCdeclTests : ZLibStreamTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.ZLibNgCdecl;
    }

    #region ZLibStreamTestsBase
    public abstract class ZLibStreamTestsBase : ZLibTestBase
    {
        #region ZLibStream - Compress
        [TestMethod]
        public void Compress()
        {
            const bool useSpan = false;
            foreach (bool testFlush in new bool[] { true, false })
            {
                CompressTemplate("ex1.jpg", ZLibCompLevel.Default, threads: -1, testFlush, useSpan);
                CompressTemplate("ex2.jpg", ZLibCompLevel.BestCompression, threads: -1, testFlush, useSpan);
                CompressTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, threads: -1, testFlush, useSpan);
                CompressTemplate("C.bin", ZLibCompLevel.Level7, threads: -1, testFlush, useSpan);
                CompressTemplate("ooffice.dll", ZLibCompLevel.BestCompression, threads: -1, testFlush, useSpan);
            }
        }

        [TestMethod]
        public void CompressSpan()
        {
            const bool useSpan = true;
            foreach (bool testFlush in new bool[] { true, false })
            {
                CompressTemplate("ex1.jpg", ZLibCompLevel.Default, threads: -1, testFlush, useSpan);
                CompressTemplate("ex2.jpg", ZLibCompLevel.BestCompression, threads: -1, testFlush, useSpan);
                CompressTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, threads: -1, testFlush, useSpan);
                CompressTemplate("C.bin", ZLibCompLevel.Level7, threads: -1, testFlush, useSpan);
                CompressTemplate("ooffice.dll", ZLibCompLevel.BestCompression, threads: -1, testFlush, useSpan);
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public void CompressParallel()
        {
            const bool useSpan = false;
            foreach (bool testFlush in new bool[] { true, false })
            {
                CompressTemplate("ex1.jpg", ZLibCompLevel.Default, threads: 2, testFlush, useSpan);
                CompressTemplate("ex2.jpg", ZLibCompLevel.BestCompression, threads: 1, testFlush, useSpan);
                CompressTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, threads: 3, testFlush, useSpan);
                CompressTemplate("C.bin", ZLibCompLevel.Level7, threads: 4, testFlush, useSpan);
                CompressTemplate("ooffice.dll", ZLibCompLevel.BestCompression, threads: Environment.ProcessorCount + 4, testFlush, useSpan); // Stress Test
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public void CompressParallelSpan()
        {
            const bool useSpan = true;
            foreach (bool testFlush in new bool[] { true, false })
            {
                CompressTemplate("ex1.jpg", ZLibCompLevel.Default, threads: 2, testFlush, useSpan);
                CompressTemplate("ex2.jpg", ZLibCompLevel.BestCompression, threads: 1, testFlush, useSpan);
                CompressTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, threads: 3, testFlush, useSpan);
                CompressTemplate("C.bin", ZLibCompLevel.Level7, threads: 4, testFlush, useSpan);
                CompressTemplate("ooffice.dll", ZLibCompLevel.BestCompression, threads: Environment.ProcessorCount + 4, testFlush, useSpan); // Stress Test
            }
        }

        private static void CompressTemplate(string sampleFileName, ZLibCompLevel level, int threads, bool flush, bool useSpan)
        {
            string tempDecompFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string tempArchiveFile = tempDecompFile + ".gz";
            try
            {
                string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);
                using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream archiveFs = new FileStream(tempArchiveFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    ZLibStream zs;
                    if (threads < 0)
                    {
                        ZLibCompressOptions compOpts = new ZLibCompressOptions()
                        {
                            Level = level,
                            LeaveOpen = true,
                        };
                        zs = new ZLibStream(archiveFs, compOpts);
                    }
                    else
                    {
                        ZLibParallelCompressOptions pcompOpts = new ZLibParallelCompressOptions()
                        {
                            Level = level,
                            LeaveOpen = true,
                            Threads = threads,
                        };
                        zs = new ZLibStream(archiveFs, pcompOpts);
                    }

                    using (zs)
                    {
                        if (flush)
                            zs.Flush();

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

                        if (flush)
                            zs.Flush();
                    }

                    Console.WriteLine($"[RAW]        expected=[{sampleFs.Length,7}] actual=[{zs.TotalIn,7}]");
                    Console.WriteLine($"[Compressed] sample  =[{archiveFs.Length,7}] actual=[{zs.TotalOut,7}]");
                    Assert.AreEqual(sampleFs.Length, zs.TotalIn);
                }

                int ret = TestHelper.RunPigz(tempArchiveFile);
                Assert.AreEqual(0, ret);

                byte[] decompDigest;
                byte[] originDigest;
                using (FileStream fs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    originDigest = TestHelper.SHA256Digest(fs);
                }

                using (FileStream fs = new FileStream(tempDecompFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    decompDigest = TestHelper.SHA256Digest(fs);
                }

                Assert.IsTrue(originDigest.SequenceEqual(decompDigest));
            }
            finally
            {
                if (File.Exists(tempArchiveFile))
                    File.Delete(tempArchiveFile);
                if (File.Exists(tempDecompFile))
                    File.Delete(tempDecompFile);
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public void MemDiagCompress()
        {
            MemDiagCompressTemplate("ex1.jpg", ZLibCompLevel.Default, threads: -1);
            MemDiagCompressTemplate("ex2.jpg", ZLibCompLevel.BestCompression, threads: -1);
            MemDiagCompressTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, threads: -1);
            MemDiagCompressTemplate("C.bin", ZLibCompLevel.Level7, threads: -1);
            MemDiagCompressTemplate("ooffice.dll", ZLibCompLevel.BestCompression, threads: -1);
        }

        [TestMethod]
        [DoNotParallelize]
        public void MemDiagCompressParallel()
        {
            MemDiagCompressTemplate("ex1.jpg", ZLibCompLevel.Default, threads: 2);
            MemDiagCompressTemplate("ex2.jpg", ZLibCompLevel.BestCompression, threads: 1);
            MemDiagCompressTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, threads: 3);
            MemDiagCompressTemplate("C.bin", ZLibCompLevel.Level7, threads: 4);
            MemDiagCompressTemplate("ooffice.dll", ZLibCompLevel.BestCompression, threads: Environment.ProcessorCount + 4); // Stress Test
        }

        private static void MemDiagCompressTemplate(string sampleFileName, ZLibCompLevel level, int threads)
        {
            long beforeMemUsage = GC.GetTotalMemory(true);

            try
            {
                string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);

                using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream compMs = new MemoryStream())
                {
                    ArrayPool<byte> pool = ArrayPool<byte>.Create();

                    ZLibStream zs;
                    if (threads < 0)
                    {
                        ZLibCompressOptions compOpts = new ZLibCompressOptions()
                        {
                            Level = level,
                            LeaveOpen = true,
                            BufferPool = pool,
                        };
                        zs = new ZLibStream(compMs, compOpts);
                    }
                    else
                    {
                        ZLibParallelCompressOptions pcompOpts = new ZLibParallelCompressOptions()
                        {
                            Level = level,
                            LeaveOpen = true,
                            Threads = threads,
                            BufferPool = pool,
                        };
                        zs = new ZLibStream(compMs, pcompOpts);
                    }

                    using (zs)
                    {
                        sampleFs.CopyTo(zs);
                    }
                }
            }
            finally
            {
                long afterMemUsage = GC.GetTotalMemory(true);

                Console.WriteLine($"[Before] {beforeMemUsage,7}");
                Console.WriteLine($"[After ] {afterMemUsage,7}");
            }
        }

        [TestMethod]
        public void CompressParallelException()
        {
            CompressParallelExceptionTemplate("ex1.jpg", ZLibCompLevel.Default, threads: 2);
            CompressParallelExceptionTemplate("ex2.jpg", ZLibCompLevel.BestCompression, threads: 1);
            CompressParallelExceptionTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, threads: 3);
            CompressParallelExceptionTemplate("C.bin", ZLibCompLevel.Level7, threads: 4);
            CompressParallelExceptionTemplate("ooffice.dll", ZLibCompLevel.BestCompression, threads: Environment.ProcessorCount + 4); // Stress Test
        }

        private static void CompressParallelExceptionTemplate(string sampleFileName, ZLibCompLevel level, int threads)
        {
            bool exceptThrown = false;

            string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);

            using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream compMs = new MemoryStream())
            {
                ZLibParallelCompressOptions pcompOpts = new ZLibParallelCompressOptions()
                {
                    Level = level,
                    LeaveOpen = true,
                    Threads = threads,
                };

                try
                {
                    using (ZLibStream zs = new ZLibStream(compMs, pcompOpts))
                    {
                        sampleFs.CopyTo(zs);
                        compMs.Dispose();
                    } // zs.Dispose() must throw exception.
                }
                catch (AggregateException ex)
                {
                    exceptThrown = true;
                    Console.WriteLine(ex);
                }
            }

            Assert.IsTrue(exceptThrown);
        }
        #endregion

        #region Decompress
        [TestMethod]
        public void Decompress()
        {
            DecompressTemplate("ex1.jpg", false);
            DecompressTemplate("ex2.jpg", false);
            DecompressTemplate("ex3.jpg", false);
            DecompressTemplate("C.bin", false);
            DecompressTemplate("ooffice.dll", false);
        }

        [TestMethod]
        public void DecompressSpan()
        {
            DecompressTemplate("ex1.jpg", true);
            DecompressTemplate("ex2.jpg", true);
            DecompressTemplate("ex3.jpg", true);
            DecompressTemplate("C.bin", true);
            DecompressTemplate("ooffice.dll", true);
        }

        private static void DecompressTemplate(string fileName, bool useSpan)
        {
            string compPath = Path.Combine(TestSetup.SampleDir, fileName + ".zz");
            string decompPath = Path.Combine(TestSetup.SampleDir, fileName);

            ZLibDecompressOptions decompOpts = new ZLibDecompressOptions();

            using (MemoryStream decompMs = new MemoryStream())
            using (FileStream decompFs = new FileStream(decompPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream compFs = new FileStream(compPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (ZLibStream zs = new ZLibStream(compFs, decompOpts))
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
