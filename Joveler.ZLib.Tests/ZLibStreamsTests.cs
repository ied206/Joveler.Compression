/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

    C# Wrapper based on zlibnet v1.3.3 (https://zlibnet.codeplex.com/)
    Copyright (C) @hardon (https://www.codeplex.com/site/users/view/hardon)
    Copyright (C) 2017-2018 Hajin Jang

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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Joveler.ZLib.Tests
{
    [TestClass]
    public class ZLibStreamsTests
    {
        #region DeflateStream - Compress
        [TestMethod]
        [TestCategory("DeflateStream")]
        public void DeflateStream_Compress_1()
        {
            void Template(string sampleFileName, ZLibCompLevel level)
            {
                string filePath = Path.Combine(TestSetup.SampleDir, sampleFileName);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream compMs = new MemoryStream())
                using (MemoryStream decompMs = new MemoryStream())
                {
                    using (DeflateStream zs = new DeflateStream(compMs, ZLibMode.Compress, level, true))
                    {
                        fs.CopyTo(zs);
                    }

                    fs.Position = 0;
                    compMs.Position = 0;

                    // Decompress compMs again
                    using (DeflateStream zs = new DeflateStream(compMs, ZLibMode.Decompress, true))
                    {
                        zs.CopyTo(decompMs);
                    }

                    decompMs.Position = 0;

                    // Compare SHA256 Digest
                    byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                    byte[] fileDigest = TestSetup.SHA256Digest(fs);
                    Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
                }
            }

            Template("ex1.jpg", ZLibCompLevel.Default);
            Template("ex2.jpg", ZLibCompLevel.BestCompression);
            Template("ex3.jpg", ZLibCompLevel.BestSpeed);
        }

        [TestMethod]
        [TestCategory("DeflateStream")]
        public void DeflateStream_Compress_2()
        {
            byte[] input = Encoding.UTF8.GetBytes("ABCDEF");
            using (MemoryStream compMs = new MemoryStream())
            using (MemoryStream decompMs = new MemoryStream())
            {
                using (DeflateStream zs = new DeflateStream(compMs, ZLibMode.Compress, ZLibCompLevel.Default, true))
                {
                    zs.Write(input, 0, input.Length);
                }

                compMs.Position = 0;
                // 73-74-72-76-71-75-03-00

                // Decompress compMs again
                using (DeflateStream zs = new DeflateStream(compMs, ZLibMode.Decompress, true))
                {
                    zs.CopyTo(decompMs);
                }

                decompMs.Position = 0;

                byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                byte[] inputDigest = TestSetup.SHA256Digest(input);

                Assert.IsTrue(decompDigest.SequenceEqual(inputDigest));
            }
        }
        #endregion

        #region DeflateStream - Decompress
        [TestMethod]
        [TestCategory("DeflateStream")]
        public void DeflateStream_Decompress_1()
        {
            void Template(string sampleFileName)
            {
                string compPath = Path.Combine(TestSetup.SampleDir, sampleFileName + ".deflate");
                string decompPath = Path.Combine(TestSetup.SampleDir, sampleFileName);
                using (MemoryStream decompMs = new MemoryStream())
                using (FileStream decompFs = new FileStream(decompPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream compFs = new FileStream(compPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (DeflateStream zs = new DeflateStream(compFs, ZLibMode.Decompress))
                    {
                        zs.CopyTo(decompMs);
                    }

                    decompMs.Position = 0;

                    // Compare SHA256 Digest
                    byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                    byte[] fileDigest = TestSetup.SHA256Digest(decompFs);
                    Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
                }
            }

            Template("ex1.jpg");
            Template("ex2.jpg");
            Template("ex3.jpg");
        }

        [TestMethod]
        [TestCategory("DeflateStream")]
        public void DeflateStream_Decompress_2()
        {
            byte[] input = new byte[] { 0x73, 0x74, 0x72, 0x76, 0x71, 0x75, 0x03, 0x00 };
            using (MemoryStream decompMs = new MemoryStream())
            {
                using (MemoryStream inputMs = new MemoryStream(input))
                using (DeflateStream zs = new DeflateStream(inputMs, ZLibMode.Decompress))
                {
                    zs.CopyTo(decompMs);
                }

                decompMs.Position = 0;

                byte[] plaintext = Encoding.UTF8.GetBytes("ABCDEF");
                byte[] decomp = decompMs.ToArray();

                Assert.IsTrue(decomp.SequenceEqual(plaintext));
            }
        }
        #endregion

        #region ZLibStream - Compress
        [TestMethod]
        [TestCategory("ZLibStream")]
        public void ZLibStream_Compress_1()
        {
            void Template(string sampleFileName, ZLibCompLevel level)
            {
                string tempDecompFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                string tempArchiveFile = tempDecompFile + ".zz";
                try
                {
                    string sampleFile = Path.Combine(TestSetup.SampleDir, sampleFileName);
                    using (FileStream sampleFs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (FileStream archiveFs = new FileStream(tempArchiveFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (ZLibStream zs = new ZLibStream(archiveFs, ZLibMode.Compress, level, true))
                    {
                        sampleFs.CopyTo(zs);
                        zs.Flush();

                        Assert.AreEqual(sampleFs.Length, zs.TotalIn);
                        Assert.AreEqual(archiveFs.Length, zs.TotalOut);
                    }

                    int ret = TestSetup.RunPigz(tempArchiveFile);
                    Assert.IsTrue(ret == 0);

                    byte[] decompDigest;
                    byte[] originDigest;
                    using (FileStream fs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        HashAlgorithm hash = SHA256.Create();
                        originDigest = hash.ComputeHash(fs);
                    }

                    using (FileStream fs = new FileStream(tempDecompFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        HashAlgorithm hash = SHA256.Create();
                        decompDigest = hash.ComputeHash(fs);
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

            Template("ex1.jpg", ZLibCompLevel.Default);
            Template("ex2.jpg", ZLibCompLevel.BestCompression);
            Template("ex3.jpg", ZLibCompLevel.BestSpeed);
        }

        [TestMethod]
        [TestCategory("ZLibStream")]
        public void ZLibStream_Compress_2()
        {
            byte[] input = Encoding.UTF8.GetBytes("ABCDEF");
            using (MemoryStream compMs = new MemoryStream())
            using (MemoryStream decompMs = new MemoryStream())
            {
                using (ZLibStream zs = new ZLibStream(compMs, ZLibMode.Compress, ZLibCompLevel.Default, true))
                {
                    zs.Write(input, 0, input.Length);
                }

                compMs.Position = 0;
                // 78-9C-73-74-72-76-71-75-03-00-05-7E-01-96

                // Decompress compMs again
                using (ZLibStream zs = new ZLibStream(compMs, ZLibMode.Decompress, true))
                {
                    zs.CopyTo(decompMs);
                }

                decompMs.Position = 0;

                byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                byte[] inputDigest = TestSetup.SHA256Digest(input);

                Assert.IsTrue(decompDigest.SequenceEqual(inputDigest));
            }
        }
        #endregion

        #region ZLibStream - Decompress
        [TestMethod]
        [TestCategory("ZLibStream")]
        public void ZLibStream_Decompress_1()
        {
            void Template(string fileName)
            {
                string compPath = Path.Combine(TestSetup.SampleDir, fileName + ".zz");
                string decompPath = Path.Combine(TestSetup.SampleDir, fileName);
                using (MemoryStream decompMs = new MemoryStream())
                using (FileStream decompFs = new FileStream(decompPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream compFs = new FileStream(compPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (ZLibStream zs = new ZLibStream(compFs, ZLibMode.Decompress))
                    {
                        zs.CopyTo(decompMs);
                    }

                    decompMs.Position = 0;

                    // Compare SHA256 Digest
                    byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                    byte[] fileDigest = TestSetup.SHA256Digest(decompFs);
                    Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
                }
            }

            Template("ex1.jpg");
            Template("ex2.jpg");
            Template("ex3.jpg");
        }

        [TestMethod]
        [TestCategory("ZLibStream")]
        public void ZLibStream_Decompress_2()
        {
            byte[] input = new byte[] { 0x78, 0x9C, 0x73, 0x74, 0x72, 0x76, 0x71, 0x75, 0x03, 0x00, 0x05, 0x7E, 0x01, 0x96 };
            using (MemoryStream decompMs = new MemoryStream())
            {
                using (MemoryStream inputMs = new MemoryStream(input))
                using (ZLibStream zs = new ZLibStream(inputMs, ZLibMode.Decompress))
                {
                    zs.CopyTo(decompMs);
                }

                decompMs.Position = 0;

                byte[] plaintext = Encoding.UTF8.GetBytes("ABCDEF");
                byte[] decomp = decompMs.ToArray();

                Assert.IsTrue(decomp.SequenceEqual(plaintext));
            }
        }
        #endregion

        #region GZipStream - Compress
        [TestMethod]
        [TestCategory("GZipStream")]
        public void GZipStream_Compress_1()
        {
            void Template(string sampleFileName, ZLibCompLevel level)
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
                        sampleFs.CopyTo(zs);
                        zs.Flush();

                        Assert.AreEqual(sampleFs.Length, zs.TotalIn);
                        Assert.AreEqual(archiveFs.Length, zs.TotalOut);
                    }

                    int ret = TestSetup.RunPigz(tempArchiveFile);
                    Assert.IsTrue(ret == 0);

                    byte[] decompDigest;
                    byte[] originDigest;
                    using (FileStream fs = new FileStream(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        HashAlgorithm hash = SHA256.Create();
                        originDigest = hash.ComputeHash(fs);
                    }

                    using (FileStream fs = new FileStream(tempDecompFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        HashAlgorithm hash = SHA256.Create();
                        decompDigest = hash.ComputeHash(fs);
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

            Template("ex1.jpg", ZLibCompLevel.Default);
            Template("ex2.jpg", ZLibCompLevel.BestCompression);
            Template("ex3.jpg", ZLibCompLevel.BestSpeed);
        }

        [TestMethod]
        [TestCategory("GZipStream")]
        public void GZipStream_Compress_2()
        {
            byte[] input = Encoding.UTF8.GetBytes("ABCDEF");
            using (MemoryStream compMs = new MemoryStream())
            using (MemoryStream decompMs = new MemoryStream())
            {
                using (GZipStream zs = new GZipStream(compMs, ZLibMode.Compress, ZLibCompLevel.Default, true))
                {
                    zs.Write(input, 0, input.Length);
                }

                compMs.Position = 0;
                // 1F-8B-08-00-00-00-00-00-00-0A-73-74-72-76-71-75-03-00-69-FE-76-BB-06-00-00-00

                // Decompress compMs again
                using (GZipStream zs = new GZipStream(compMs, ZLibMode.Decompress, true))
                {
                    zs.CopyTo(decompMs);
                }

                decompMs.Position = 0;

                byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                byte[] inputDigest = TestSetup.SHA256Digest(input);

                Assert.IsTrue(decompDigest.SequenceEqual(inputDigest));
            }
        }
        #endregion

        #region GZipStream - Decompress
        [TestMethod]
        [TestCategory("GZipStream")]
        public void GZipStream_Decompress_1()
        {
            void Template(string fileName)
            {
                string compPath = Path.Combine(TestSetup.SampleDir, fileName + ".gz");
                string decompPath = Path.Combine(TestSetup.SampleDir, fileName);
                using (MemoryStream decompMs = new MemoryStream())
                using (FileStream decompFs = new FileStream(decompPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream compFs = new FileStream(compPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (GZipStream zs = new GZipStream(compFs, ZLibMode.Decompress))
                    {
                        zs.CopyTo(decompMs);
                    }

                    decompMs.Position = 0;

                    // Compare SHA256 Digest
                    byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                    byte[] fileDigest = TestSetup.SHA256Digest(decompFs);
                    Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
                }
            }

            Template("ex1.jpg");
            Template("ex2.jpg");
            Template("ex3.jpg");
        }

        [TestMethod]
        [TestCategory("GZipStream")]
        public void GZipStream_Decompress_2()
        {
            byte[] input = new byte[] { 0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x73, 0x74, 0x72, 0x76, 0x71, 0x75, 0x03, 0x00, 0x69, 0xFE, 0x76, 0xBB, 0x06, 0x00, 0x00, 0x00 };
            using (MemoryStream decompMs = new MemoryStream())
            {
                using (MemoryStream inputMs = new MemoryStream(input))
                using (GZipStream zs = new GZipStream(inputMs, ZLibMode.Decompress))
                {
                    zs.CopyTo(decompMs);
                }

                decompMs.Position = 0;

                byte[] plaintext = Encoding.UTF8.GetBytes("ABCDEF");
                byte[] decomp = decompMs.ToArray();

                Assert.IsTrue(decomp.SequenceEqual(plaintext));
            }
        }
        #endregion
    }
}
