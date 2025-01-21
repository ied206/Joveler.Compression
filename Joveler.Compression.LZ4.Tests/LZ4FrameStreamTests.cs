/*
    Written by Hajin Jang (BSD 2-Clause)
    Copyright (C) 2018-present Hajin Jang

    Redistribution and use in source and binary forms, with or without modification,
    are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this
      list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice, this
      list of conditions and the following disclaimer in the documentation and/or
      other materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
    WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
    ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
    (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
    LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
    ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
    (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
    SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Joveler.Compression.LZ4.Tests
{
    [TestClass]
    public class LZ4FrameStreamTests
    {
        private class CompressTestOptions
        {
            public bool StreamFlush { get; set; } = false;
            public bool AutoFlush { get; set; } = true;
            public bool EnableContentSize { get; set; } = false;
            public bool UseSpan { get; set; } = false;

            public static IReadOnlyList<CompressTestOptions> CreateTestSet()
            {
                bool[] bArr = { true, false };
                List<CompressTestOptions> testOptsList = new List<CompressTestOptions>();
                foreach (bool streamFlush in bArr)
                {
                    foreach (bool autoFlush in bArr)
                    {
                        foreach (bool enableContentSize in bArr)
                        {
                            foreach (bool useSpan in bArr)
                            {
                                CompressTestOptions testOpts = new CompressTestOptions
                                {
                                    StreamFlush = streamFlush,
                                    AutoFlush = autoFlush,
                                    EnableContentSize = enableContentSize,
                                    UseSpan = useSpan,
                                };
                                testOptsList.Add(testOpts);
                            }
                        }
                    }
                }
                return testOptsList;
            }
        }

        #region Compress
        [TestMethod]
        public void Compress()
        {
            IReadOnlyList<CompressTestOptions> testOptsList = CompressTestOptions.CreateTestSet();

            foreach (CompressTestOptions testOpts in testOptsList)
            {
                CompressTemplate("A.pdf", LZ4CompLevel.Fast, FrameBlockSizeId.Max64KB, -1, testOpts);
                CompressTemplate("B.txt", LZ4CompLevel.High, FrameBlockSizeId.Default, -1, testOpts);
                CompressTemplate("C.bin", LZ4CompLevel.VeryHigh, FrameBlockSizeId.Max256KB, -1, testOpts);
            }           
        }

        [TestMethod]
        [DoNotParallelize]
        public void CompressParallel()
        {
            IReadOnlyList<CompressTestOptions> testOptsList = CompressTestOptions.CreateTestSet();

            foreach (CompressTestOptions testOpts in testOptsList)
            {
                CompressTemplate("A.pdf", LZ4CompLevel.Fast, FrameBlockSizeId.Max64KB, 1, testOpts);
                CompressTemplate("B.txt", LZ4CompLevel.High, FrameBlockSizeId.Default, 2, testOpts);
                CompressTemplate("C.bin", LZ4CompLevel.VeryHigh, FrameBlockSizeId.Max256KB, 3, testOpts);
                // Stress test
                CompressTemplate("C.bin", LZ4CompLevel.VeryHigh, FrameBlockSizeId.Max64KB, Environment.ProcessorCount + 4, testOpts);
            }
        }

        private static void CompressTemplate(string sampleFileName, LZ4CompLevel compLevel, FrameBlockSizeId blockSizeId, int threads, CompressTestOptions testOpts)
        {
            if (sampleFileName == null)
                throw new ArgumentNullException(nameof(sampleFileName));

            string destDir = Path.GetTempFileName();
            File.Delete(destDir);
            Directory.CreateDirectory(destDir);
            try
            {
                string tempDecompFile = Path.Combine(destDir, Path.GetFileName(sampleFileName));
                string tempLz4File = tempDecompFile + ".lz4";

                string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);
                string sampleLz4File = sampleFile + ".lz4";
                using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream lz4CompFs = new FileStream(tempLz4File, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    LZ4FrameStream lzs;
                    if (threads < 0)
                    {
                        LZ4FrameCompressOptions compOpts = new LZ4FrameCompressOptions()
                        {
                            Level = compLevel,
                            AutoFlush = testOpts.AutoFlush,
                            BlockSizeId = blockSizeId,
                            LeaveOpen = true,
                        };
                        if (testOpts.EnableContentSize)
                            compOpts.ContentSize = (ulong)sampleFs.Length;

                        lzs = new LZ4FrameStream(lz4CompFs, compOpts);
                    }
                    else
                    {
                        LZ4FrameParallelCompressOptions pcompOpts = new LZ4FrameParallelCompressOptions()
                        {
                            Level = compLevel,
                            BlockSizeId = blockSizeId,
                            LeaveOpen = true,
                            Threads = threads,
                        };
                        if (testOpts.EnableContentSize)
                            pcompOpts.ContentSize = (ulong)sampleFs.Length;

                        lzs = new LZ4FrameStream(lz4CompFs, pcompOpts);
                    }
                    
                    using (lzs)
                    {
                        if (testOpts.StreamFlush)
                            lzs.Flush();

#if !NETFRAMEWORK
                        if (testOpts.UseSpan)
                        {
                            byte[] buffer = new byte[64 * 1024];

                            int bytesRead;
                            do
                            {
                                bytesRead = sampleFs.Read(buffer.AsSpan());
                                lzs.Write(buffer.AsSpan(0, bytesRead));
                            } while (0 < bytesRead);
                        }
                        else
#endif
                        {
                            sampleFs.CopyTo(lzs);
                        }

                        if (testOpts.StreamFlush)
                            lzs.Flush();
                    }

                    Console.WriteLine($"[RAW]        expected=[{sampleFs.Length,7}] actual=[{lzs.TotalIn,7}]");
                    Console.WriteLine($"[Compressed] sample  =[{new FileInfo(sampleLz4File).Length,7}] actual=[{lzs.TotalOut,7}]");
                    Assert.AreEqual(sampleFs.Length, lzs.TotalIn);
                }

                Assert.IsTrue(TestHelper.RunLZ4(tempLz4File, tempDecompFile) == 0);

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
                if (Directory.Exists(destDir))
                    Directory.Delete(destDir, true);
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public void MemDiagCompress()
        {
            MemDiagCompressTemplate("A.pdf", LZ4CompLevel.Fast, FrameBlockSizeId.Max64KB, -1);
            MemDiagCompressTemplate("B.txt", LZ4CompLevel.High, FrameBlockSizeId.Default, -1);
            MemDiagCompressTemplate("C.bin", LZ4CompLevel.VeryHigh, FrameBlockSizeId.Max256KB, -1);
        }

        [TestMethod]
        [DoNotParallelize]
        public void MemDiagCompressParallel()
        {
            MemDiagCompressTemplate("A.pdf", LZ4CompLevel.Fast, FrameBlockSizeId.Max64KB, 1);
            MemDiagCompressTemplate("B.txt", LZ4CompLevel.High, FrameBlockSizeId.Default, 2);
            MemDiagCompressTemplate("C.bin", LZ4CompLevel.VeryHigh, FrameBlockSizeId.Max256KB, 3);
            // Stress test
            MemDiagCompressTemplate("C.bin", LZ4CompLevel.VeryHigh, FrameBlockSizeId.Max64KB, Environment.ProcessorCount + 4);
        }

        private static void MemDiagCompressTemplate(string sampleFileName, LZ4CompLevel compLevel, FrameBlockSizeId blockSizeId, int threads)
        {
            long beforeMemUsage = GC.GetTotalMemory(true);

            try
            {
                string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);

                using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream compMs = new MemoryStream())
                {
                    ArrayPool<byte> pool = ArrayPool<byte>.Create();

                    LZ4FrameStream lzs;
                    if (threads < 0)
                    {
                        LZ4FrameCompressOptions compOpts = new LZ4FrameCompressOptions()
                        {
                            Level = compLevel,
                            AutoFlush = false,
                            BlockSizeId = blockSizeId,
                            LeaveOpen = true,
                            BufferPool = pool,
                        };

                        lzs = new LZ4FrameStream(compMs, compOpts);
                    }
                    else
                    {
                        LZ4FrameParallelCompressOptions pcompOpts = new LZ4FrameParallelCompressOptions()
                        {
                            Level = compLevel,
                            BlockSizeId = blockSizeId,
                            LeaveOpen = true,
                            Threads = threads,
                            BufferPool = pool,
                        };
                        lzs = new LZ4FrameStream(compMs, pcompOpts);
                    }

                    using (lzs)
                    {
                        sampleFs.CopyTo(lzs);
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
            CompressParallelExceptionTemplate("A.pdf", LZ4CompLevel.Fast, FrameBlockSizeId.Max64KB, 1);
            CompressParallelExceptionTemplate("B.txt", LZ4CompLevel.High, FrameBlockSizeId.Default, 2);
            CompressParallelExceptionTemplate("C.bin", LZ4CompLevel.VeryHigh, FrameBlockSizeId.Max256KB, 3);
            // Stress test
            CompressParallelExceptionTemplate("C.bin", LZ4CompLevel.VeryHigh, FrameBlockSizeId.Max64KB, Environment.ProcessorCount + 4);
        }

        private static void CompressParallelExceptionTemplate(string sampleFileName, LZ4CompLevel compLevel, FrameBlockSizeId blockSizeId, int threads)
        {
            bool exceptThrown = false;

            string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);

            using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream compMs = new MemoryStream())
            {
                LZ4FrameParallelCompressOptions pcompOpts = new LZ4FrameParallelCompressOptions()
                {
                    Level = compLevel,
                    BlockSizeId = blockSizeId,
                    LeaveOpen = true,
                    Threads = threads,
                };

                try
                {
                    using (LZ4FrameStream lzs = new LZ4FrameStream(compMs, pcompOpts))
                    {
                        sampleFs.CopyTo(lzs);
                        compMs.Dispose();
                    } // lzs.Dispose() must throw exception.
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
            DecompressTemplate("A.pdf.lz4", "A.pdf", false); // -12
            DecompressTemplate("B.txt.lz4", "B.txt", false); // -9
            DecompressTemplate("C.bin.lz4", "C.bin", false); // -1
        }

        [TestMethod]
        public void DecompressSpan()
        {
            DecompressTemplate("A.pdf.lz4", "A.pdf", true); // -12
            DecompressTemplate("B.txt.lz4", "B.txt", true); // -9
            DecompressTemplate("C.bin.lz4", "C.bin", true); // -1
        }

        private static void DecompressTemplate(string lz4FileName, string originFileName, bool useSpan)
        {
            byte[] decompDigest;
            byte[] originDigest;

            LZ4FrameDecompressOptions decompOpts = new LZ4FrameDecompressOptions();

            string lz4File = Path.Combine(TestSetup.SampleDir, lz4FileName);
            string originFile = Path.Combine(TestSetup.SampleDir, originFileName);
            using (MemoryStream decompMs = new MemoryStream())
            {
                using (FileStream compFs = new FileStream(lz4File, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (LZ4FrameStream lzs = new LZ4FrameStream(compFs, decompOpts))
                {
#if !NETFRAMEWORK
                    if (useSpan)
                    {
                        byte[] buffer = new byte[64 * 1024];

                        int bytesRead;
                        do
                        {
                            bytesRead = lzs.Read(buffer.AsSpan());
                            decompMs.Write(buffer.AsSpan(0, bytesRead));
                        } while (0 < bytesRead);
                    }
                    else
#endif
                    {
                        lzs.CopyTo(decompMs);
                    }

                    decompMs.Flush();

                    Assert.AreEqual(compFs.Length, lzs.TotalIn);
                    Assert.AreEqual(decompMs.Length, lzs.TotalOut);
                }
                decompMs.Position = 0;

                decompDigest = TestHelper.SHA256Digest(decompMs);
            }

            using (FileStream originFs = new FileStream(originFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                originDigest = TestHelper.SHA256Digest(originFs);
            }

            Assert.IsTrue(decompDigest.SequenceEqual(originDigest));
        }
        #endregion
    }
}
