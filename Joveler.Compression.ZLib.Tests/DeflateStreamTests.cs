/*
    C# tests by Hajin Jang
    Copyright (C) 2017-2020 Hajin Jang

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
    public class DeflateStreamTests
    {
        #region Compress
        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Compress()
        {
            CompressTemplate("ex1.jpg", ZLibCompLevel.Default, false);
            CompressTemplate("ex2.jpg", ZLibCompLevel.BestCompression, false);
            CompressTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, false);
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void CompressSpan()
        {
            CompressTemplate("ex1.jpg", ZLibCompLevel.Default, true);
            CompressTemplate("ex2.jpg", ZLibCompLevel.BestCompression, true);
            CompressTemplate("ex3.jpg", ZLibCompLevel.BestSpeed, true);
        }

        private static void CompressTemplate(string sampleFileName, ZLibCompLevel level, bool useSpan)
        {
            string filePath = Path.Combine(TestSetup.SampleDir, sampleFileName);

            ZLibCompressOptions compOpts = new ZLibCompressOptions()
            {
                Level = level,
                LeaveOpen = true,
            };

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream compMs = new MemoryStream())
            using (MemoryStream decompMs = new MemoryStream())
            {
                using (DeflateStream zs = new DeflateStream(compMs, compOpts))
                {
#if !NETFRAMEWORK
                    if (useSpan)
                    {
                        byte[] buffer = new byte[64 * 1024];
                        int bytesRead;
                        do
                        {

                            bytesRead = fs.Read(buffer.AsSpan());
                            zs.Write(buffer.AsSpan(0, bytesRead));
                        } while (0 < bytesRead);
                    }
                    else
#endif
                    {
                        fs.CopyTo(zs);
                    }
                }

                fs.Position = 0;
                compMs.Position = 0;

                // Decompress compMs with BCL DeflateStream
                using (System.IO.Compression.DeflateStream zs = new System.IO.Compression.DeflateStream(compMs, CompressionMode.Decompress, true))
                {
                    zs.CopyTo(decompMs);
                }

                decompMs.Position = 0;

                // Compare SHA256 Digest
                byte[] decompDigest = TestHelper.SHA256Digest(decompMs);
                byte[] fileDigest = TestHelper.SHA256Digest(fs);
                Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
            }
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
}
