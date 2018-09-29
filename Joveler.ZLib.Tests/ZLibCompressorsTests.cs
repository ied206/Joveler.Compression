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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text;

namespace Joveler.ZLib.Tests
{
    [TestClass]
    public class ZLibCompressorsTests
    {
        #region DeflateCompressor - Compress
        [TestMethod]
        [TestCategory("DeflateCompressor")]
        public void DeflateStream_Compressor_1()
        {
            void Template(string fileName, ZLibCompLevel level)
            {
                string filePath = Path.Combine(TestSetup.SampleDir, fileName);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream compMs = DeflateCompressor.Compress(fs))
                using (MemoryStream decompMs = DeflateCompressor.Decompress(compMs))
                {
                    // Compare SHA256 Digest
                    fs.Position = 0;
                    byte[] fileDigest = TestSetup.SHA256Digest(fs);
                    byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                    Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
                }
            }

            Template("ex1.jpg", ZLibCompLevel.Default);
            Template("ex2.jpg", ZLibCompLevel.BestCompression);
            Template("ex3.jpg", ZLibCompLevel.BestSpeed);
        }

        [TestMethod]
        [TestCategory("DeflateCompressor")]
        public void DeflateStream_Compressor_2()
        {
            byte[] input = Encoding.UTF8.GetBytes("ABCDEF");

            // Compress first,
            // 73-74-72-76-71-75-03-00
            byte[] compBytes = DeflateCompressor.Compress(input);

            // then Decompress.
            byte[] decompBytes = DeflateCompressor.Decompress(compBytes);

            // Comprare SHA256 Digest
            byte[] inputDigest = TestSetup.SHA256Digest(input);
            byte[] decompDigest = TestSetup.SHA256Digest(decompBytes);
            Assert.IsTrue(decompDigest.SequenceEqual(inputDigest));
        }
        #endregion

        #region DeflateCompressor - Decompress
        [TestMethod]
        [TestCategory("DeflateCompressor")]
        public void DeflateStream_Decompressor_1()
        {
            void Template(string fileName)
            {
                string compPath = Path.Combine(TestSetup.SampleDir, fileName + ".deflate");
                string decompPath = Path.Combine(TestSetup.SampleDir, fileName);
                using (FileStream decompFs = new FileStream(decompPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream compFs = new FileStream(compPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream decompMs = DeflateCompressor.Decompress(compFs))
                {
                    // Compare SHA256 Digest
                    byte[] fileDigest = TestSetup.SHA256Digest(decompFs);
                    byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                    Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
                }
            }

            Template("ex1.jpg");
            Template("ex2.jpg");
            Template("ex3.jpg");
        }

        [TestMethod]
        [TestCategory("DeflateCompressor")]
        public void DeflateStream_Decompressor_2()
        {
            byte[] input = new byte[] { 0x73, 0x74, 0x72, 0x76, 0x71, 0x75, 0x03, 0x00 };
            byte[] plaintext = Encoding.UTF8.GetBytes("ABCDEF");
            byte[] decompBytes = DeflateCompressor.Decompress(input);
            Assert.IsTrue(decompBytes.SequenceEqual(plaintext));
        }
        #endregion

        #region ZLibCompressor - Compress
        [TestMethod]
        [TestCategory("ZLibCompressor")]
        public void ZLibStream_Compressor_1()
        {
            void Template(string fileName, ZLibCompLevel level)
            {
                string filePath = Path.Combine(TestSetup.SampleDir, fileName);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream compMs = ZLibCompressor.Compress(fs))
                using (MemoryStream decompMs = ZLibCompressor.Decompress(compMs))
                {
                    // Compare SHA256 Digest
                    fs.Position = 0;
                    byte[] fileDigest = TestSetup.SHA256Digest(fs);
                    byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                    Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
                }
            }

            Template("ex1.jpg", ZLibCompLevel.Default);
            Template("ex2.jpg", ZLibCompLevel.BestCompression);
            Template("ex3.jpg", ZLibCompLevel.BestSpeed);
        }

        [TestMethod]
        [TestCategory("ZLibCompressor")]
        public void ZLibStream_Compressor_2()
        {
            byte[] input = Encoding.UTF8.GetBytes("ABCDEF");

            // Compress first,
            // 78-9C-73-74-72-76-71-75-03-00-05-7E-01-96
            byte[] compBytes = ZLibCompressor.Compress(input);

            // then Decompress.
            byte[] decompBytes = ZLibCompressor.Decompress(compBytes);

            // Comprare SHA256 Digest
            byte[] inputDigest = TestSetup.SHA256Digest(input);
            byte[] decompDigest = TestSetup.SHA256Digest(decompBytes);
            Assert.IsTrue(decompDigest.SequenceEqual(inputDigest));
        }
        #endregion

        #region ZLibCompressor - Decompress
        [TestMethod]
        [TestCategory("ZLibCompressor")]
        public void ZLibStream_Decompressor_1()
        {
            void Template(string fileName)
            {
                string compPath = Path.Combine(TestSetup.SampleDir, fileName + ".zz");
                string decompPath = Path.Combine(TestSetup.SampleDir, fileName);
                using (FileStream decompFs = new FileStream(decompPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream compFs = new FileStream(compPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream decompMs = ZLibCompressor.Decompress(compFs))
                {
                    // Compare SHA256 Digest
                    byte[] fileDigest = TestSetup.SHA256Digest(decompFs);
                    byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                    Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
                }
            }

            Template("ex1.jpg");
            Template("ex2.jpg");
            Template("ex3.jpg");
        }

        [TestMethod]
        [TestCategory("ZLibCompressor")]
        public void ZLibStream_Decompressor_2()
        {
            byte[] input = new byte[] { 0x78, 0x9C, 0x73, 0x74, 0x72, 0x76, 0x71, 0x75, 0x03, 0x00, 0x05, 0x7E, 0x01, 0x96 };
            byte[] plaintext = Encoding.UTF8.GetBytes("ABCDEF");
            byte[] decompBytes = ZLibCompressor.Decompress(input);
            Assert.IsTrue(decompBytes.SequenceEqual(plaintext));
        }
        #endregion

        #region GZipCompressor - Compress
        [TestMethod]
        [TestCategory("GZipCompressor")]
        public void GZipStream_Compressor_1()
        {
            void Template(string fileName, ZLibCompLevel level)
            {
                string filePath = Path.Combine(TestSetup.SampleDir, fileName);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream compMs = GZipCompressor.Compress(fs))
                using (MemoryStream decompMs = GZipCompressor.Decompress(compMs))
                {
                    // Compare SHA256 Digest
                    fs.Position = 0;
                    byte[] fileDigest = TestSetup.SHA256Digest(fs);
                    byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                    Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
                }
            }

            Template("ex1.jpg", ZLibCompLevel.Default);
            Template("ex2.jpg", ZLibCompLevel.BestCompression);
            Template("ex3.jpg", ZLibCompLevel.BestSpeed);
        }

        [TestMethod]
        [TestCategory("GZipCompressor")]
        public void GZipStream_Compressor_2()
        {
            byte[] input = Encoding.UTF8.GetBytes("ABCDEF");

            // Compress first,
            // 1F-8B-08-00-00-00-00-00-00-0A-73-74-72-76-71-75-03-00-69-FE-76-BB-06-00-00-00
            byte[] compBytes = GZipCompressor.Compress(input);

            // then Decompress
            byte[] decompBytes = GZipCompressor.Decompress(compBytes);

            // Comprare SHA256 Digest
            byte[] inputDigest = TestSetup.SHA256Digest(input);
            byte[] decompDigest = TestSetup.SHA256Digest(decompBytes);
            Assert.IsTrue(decompDigest.SequenceEqual(inputDigest));
        }
        #endregion

        #region GZipCompressor - Decompress
        [TestMethod]
        [TestCategory("GZipCompressor")]
        public void GZipStream_Decompressor_1()
        {
            void Template(string fileName)
            {
                string compPath = Path.Combine(TestSetup.SampleDir, fileName + ".gz");
                string decompPath = Path.Combine(TestSetup.SampleDir, fileName);
                using (FileStream decompFs = new FileStream(decompPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream compFs = new FileStream(compPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream decompMs = GZipCompressor.Decompress(compFs))
                {
                    // Compare SHA256 Digest
                    byte[] fileDigest = TestSetup.SHA256Digest(decompFs);
                    byte[] decompDigest = TestSetup.SHA256Digest(decompMs);
                    Assert.IsTrue(decompDigest.SequenceEqual(fileDigest));
                }
            }

            Template("ex1.jpg");
            Template("ex2.jpg");
            Template("ex3.jpg");
        }

        [TestMethod]
        [TestCategory("GZipCompressor")]
        public void GZipStream_Decompressor_2()
        {
            byte[] input = new byte[] { 0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x73, 0x74, 0x72, 0x76, 0x71, 0x75, 0x03, 0x00, 0x69, 0xFE, 0x76, 0xBB, 0x06, 0x00, 0x00, 0x00 };
            byte[] plaintext = Encoding.UTF8.GetBytes("ABCDEF");
            byte[] decompBytes = GZipCompressor.Decompress(input);
            Assert.IsTrue(decompBytes.SequenceEqual(plaintext));
        }
        #endregion
    }
}
