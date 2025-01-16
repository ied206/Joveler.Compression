﻿/*
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
using System.IO;
using System.Linq;

namespace Joveler.Compression.LZ4.Tests
{
    [TestClass]
    public class LZ4FrameStreamTests
    {
        #region Compress
        [TestMethod]
        public void Compress()
        {
            foreach (bool useSpan in new bool[] { true, false })
            {
                foreach (bool enableContentSize in new bool[] { true, false })
                {
                    foreach (bool autoFlush in new bool[] { true, false })
                    {
                        CompressTemplate("A.pdf", LZ4CompLevel.Fast, FrameBlockSizeId.Max64KB, -1, autoFlush, enableContentSize, useSpan);
                        CompressTemplate("B.txt", LZ4CompLevel.High, FrameBlockSizeId.Default, -1, autoFlush, enableContentSize, useSpan);
                        CompressTemplate("C.bin", LZ4CompLevel.VeryHigh, FrameBlockSizeId.Max256KB, -1, autoFlush, enableContentSize, useSpan);
                    }
                }
            }           
        }

        [TestMethod]
        public void CompressParallel()
        {
            foreach (bool useSpan in new bool[] { true, false })
            {
                foreach (bool enableContentSize in new bool[] { true, false })
                {
                    CompressTemplate("A.pdf", LZ4CompLevel.Fast, FrameBlockSizeId.Max64KB, 1, false, enableContentSize, useSpan);
                    CompressTemplate("B.txt", LZ4CompLevel.High, FrameBlockSizeId.Default, 2, false, enableContentSize, useSpan);
                    CompressTemplate("C.bin", LZ4CompLevel.VeryHigh, FrameBlockSizeId.Max256KB, 3, false, enableContentSize, useSpan);
                }
            }           
        }

        private static void CompressTemplate(string sampleFileName, LZ4CompLevel compLevel, FrameBlockSizeId blockSizeId, int threads, bool autoFlush, bool enableContentSize, bool useSpan)
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
                            AutoFlush = autoFlush,
                            BlockSizeId = blockSizeId,
                            LeaveOpen = true,
                        };
                        if (enableContentSize)
                            compOpts.ContentSize = (ulong)sampleFs.Length;

                        lzs = new LZ4FrameStream(lz4CompFs, compOpts);
                    }
                    else
                    {
                        LZ4FrameParallelCompressOptions pcompOpts = new LZ4FrameParallelCompressOptions()
                        {
                            Level = compLevel,
                            LeaveOpen = true,
                            BlockSizeId = blockSizeId,
                            Threads = threads
                        };
                        if (enableContentSize)
                            pcompOpts.ContentSize = (ulong)sampleFs.Length;

                        lzs = new LZ4FrameStream(lz4CompFs, pcompOpts);
                    }
                    
                    using (lzs)
                    {
#if !NETFRAMEWORK
                        if (useSpan)
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
