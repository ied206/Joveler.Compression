/*
    Derived from Zstandard header files (BSD 2-Clause)
    Copyright (c) 2016-present, Yann Collet, Facebook, Inc. All rights reserved.

    C# Wrapper written by Hajin Jang
    Copyright (C) 2020-2022 Hajin Jang

    Redistribution and use in source and binary forms, with or without modification,
    are permitted provided that the following conditions are met:

     * Redistributions of source code must retain the above copyright notice, this
       list of conditions and the following disclaimer.

     * Redistributions in binary form must reproduce the above copyright notice,
       this list of conditions and the following disclaimer in the documentation
       and/or other materials provided with the distribution.

     * Neither the name Facebook nor the names of its contributors may be used to
       endorse or promote products derived from this software without specific
       prior written permission.

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

namespace Joveler.Compression.Zstd.Tests
{
    [TestClass]
    public class ZstdStreamTests
    {
        #region Compress
        [TestMethod]
        [TestCategory("Joveler.Compression.Zstd")]
        public void Compress()
        {

            CompressTemplate("A.pdf", ZstdStream.MaxCompressionLevel(), false, false);
            CompressTemplate("B.txt", ZstdStream.DefaultCompressionLevel(), true, false);
            CompressTemplate("C.bin", ZstdStream.MinCompressionLevel(), false, false);
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.Zstd")]
        public void CompressSpan()
        {
            CompressTemplate("A.pdf", ZstdStream.MaxCompressionLevel(), false, true);
            CompressTemplate("B.txt", ZstdStream.DefaultCompressionLevel(), true, true);
            CompressTemplate("C.bin", ZstdStream.MinCompressionLevel(), false, true);
        }

        private static void CompressTemplate(string sampleFileName, int compLevel, bool enableContentSize, bool useSpan)
        {
            if (sampleFileName == null)
                throw new ArgumentNullException(nameof(sampleFileName));

            string destDir = Path.GetTempFileName();
            File.Delete(destDir);
            Directory.CreateDirectory(destDir);
            try
            {
                string tempDecompFile = Path.Combine(destDir, Path.GetFileName(sampleFileName));
                string tempZstdFile = tempDecompFile + ".zst";

                string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);
                using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ZstdCompressOptions compOpts = new ZstdCompressOptions()
                    {
                        CompressionLevel = compLevel,
                        LeaveOpen = true,
                    };
                    if (enableContentSize)
                        compOpts.ContentSize = (ulong)sampleFs.Length;

                    using (FileStream zstdCompFs = new FileStream(tempZstdFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (ZstdStream zs = new ZstdStream(zstdCompFs, compOpts))
                    {
#if !NETFRAMEWORK
                        if (useSpan)
                        {
                            byte[] buffer = new byte[1024 * 1024];

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

                        zs.Flush();

                        Assert.AreEqual(sampleFs.Length, zs.TotalIn);
                        Assert.AreEqual(zstdCompFs.Length, zs.TotalOut);
                    }
                }

                Assert.IsTrue(TestHelper.RunZstd(tempZstdFile, tempDecompFile) == 0);

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
            // DecompressTemplate("A.pdf.zst", "A.pdf", false); // -12
            // DecompressTemplate("B.txt.zst", "B.txt", false); // -9
            DecompressTemplate("C.bin.zst", "C.bin", false); // -1
        }

        [TestMethod]
        public void DecompressSpan()
        {
            DecompressTemplate("A.pdf.zst", "A.pdf", true); // -12
            DecompressTemplate("B.txt.zst", "B.txt", true); // -9
            DecompressTemplate("C.bin.zst", "C.bin", true); // -1
        }

        private static void DecompressTemplate(string lz4FileName, string originFileName, bool useSpan)
        {
            byte[] decompDigest;
            byte[] originDigest;

            ZstdDecompressOptions decompOpts = new ZstdDecompressOptions();

            string lz4File = Path.Combine(TestSetup.SampleDir, lz4FileName);
            string originFile = Path.Combine(TestSetup.SampleDir, originFileName);
            using (MemoryStream decompMs = new MemoryStream())
            {
                using (FileStream compFs = new FileStream(lz4File, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (ZstdStream zs = new ZstdStream(compFs, decompOpts))
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

                    decompMs.Flush();

                    Assert.AreEqual(compFs.Length, zs.TotalIn);
                    Assert.AreEqual(decompMs.Length, zs.TotalOut);
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
