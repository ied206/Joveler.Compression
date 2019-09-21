using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Joveler.Compression.ZLib.Tests
{
    [TestClass]
    public class GZipStreamTests
    {
        #region GZipStream - Compress
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
            string tempDecompFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string tempArchiveFile = tempDecompFile + ".gz";
            try
            {
                string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);
                using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream archiveFs = new FileStream(tempArchiveFile, FileMode.Create, FileAccess.Write, FileShare.None))
                using (GZipStream zs = new GZipStream(archiveFs, ZLibMode.Compress, level, true))
                {
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
                    {
                        sampleFs.CopyTo(zs);
                    }

                    zs.Flush();

                    Assert.AreEqual(sampleFs.Length, zs.TotalIn);
                    Assert.AreEqual(archiveFs.Length, zs.TotalOut);
                }

                int ret = TestHelper.RunPigz(tempArchiveFile);
                Assert.IsTrue(ret == 0);

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

        private static void DecompressTemplate(string fileName, bool useSpan)
        {
            string compPath = Path.Combine(TestSetup.SampleDir, fileName + ".gz");
            string decompPath = Path.Combine(TestSetup.SampleDir, fileName);
            using (MemoryStream decompMs = new MemoryStream())
            using (FileStream decompFs = new FileStream(decompPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream compFs = new FileStream(compPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (GZipStream zs = new GZipStream(compFs, ZLibMode.Decompress))
                {
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
