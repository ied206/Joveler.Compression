/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

    C# tests by Hajin Jang
    Copyright (C) 2017-2019 Hajin Jang

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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
// ReSharper disable CommentTypo

namespace Joveler.Compression.ZLib.Tests
{
    [TestClass]
    public class ChecksumTests
    {
        #region Crc32Stream
        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Crc32Stream_1()
        {
            string filePath = Path.Combine(TestSetup.SampleDir, "ex1.jpg");
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream ms = new MemoryStream())
            using (Crc32Stream crc = new Crc32Stream(ms))
            {
                fs.CopyTo(crc);
                Assert.IsTrue(crc.Checksum == 0x1961D0C6);
            }
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Crc32Stream_2()
        {
            string filePath = Path.Combine(TestSetup.SampleDir, "ex2.jpg");
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream ms = new MemoryStream())
            using (Crc32Stream crc = new Crc32Stream(ms))
            {
                fs.CopyTo(crc);
                Assert.IsTrue(crc.Checksum == 0x7641A243);
            }
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Crc32Stream_3()
        {
            string filePath = Path.Combine(TestSetup.SampleDir, "ex3.jpg");
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream ms = new MemoryStream())
            using (Crc32Stream crc = new Crc32Stream(ms))
            {
                fs.CopyTo(crc);
                Assert.IsTrue(crc.Checksum == 0x63D4D64B);
            }
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Crc32Stream_4()
        {
            using (MemoryStream ms = new MemoryStream())
            using (Crc32Stream crc = new Crc32Stream(ms))
            {
                byte[] bin = Encoding.UTF8.GetBytes("ABCDEF");
                crc.Write(bin, 0, bin.Length);
                Assert.IsTrue(crc.Checksum == 0xBB76FE69);
            }
        }
        #endregion

        #region Crc32Checksum
        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Crc32Checksum_1()
        {
            void Template(string path, uint checksum)
            {
                string filePath = Path.Combine(TestSetup.SampleDir, path);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Crc32Checksum crc = new Crc32Checksum();
                    crc.Append(fs);
                    Assert.IsTrue(crc.Checksum == checksum);
                }
            }

            Template("ex1.jpg", 0x1961D0C6);
            Template("ex2.jpg", 0x7641A243);
            Template("ex3.jpg", 0x63D4D64B);
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Crc32Checksum_4()
        {
            Crc32Checksum crc = new Crc32Checksum();

            byte[] buf = Encoding.UTF8.GetBytes("ABC");
            crc.Append(buf, 0, buf.Length);
            Assert.IsTrue(crc.Checksum == 0xA3830348); // ABC

            buf = Encoding.UTF8.GetBytes("DEF");
            crc.Append(buf, 0, buf.Length);
            Assert.IsTrue(crc.Checksum == 0xBB76FE69); // ABCDEF
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Crc32Checksum_5()
        {
            byte[] buf = Encoding.UTF8.GetBytes("ABC");
            uint checksum = Crc32Checksum.Crc32(buf, 0, buf.Length);
            Assert.IsTrue(checksum == 0xA3830348); // ABC

            buf = Encoding.UTF8.GetBytes("DEF");
            checksum = Crc32Checksum.Crc32(checksum, buf, 0, buf.Length);
            Assert.IsTrue(checksum == 0xBB76FE69); // ABCDEF
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Crc32Checksum_6()
        {
            byte[] sample = Encoding.UTF8.GetBytes("ABCDEF");

            uint checksum = Crc32Checksum.Crc32(sample, 1, 3);
            Assert.IsTrue(checksum == 0x26BA19F3); // BCD

            checksum = Crc32Checksum.Crc32(sample.AsSpan().Slice(1, 3));
            Assert.IsTrue(checksum == 0x26BA19F3); // BCD
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Crc32Checksum_7()
        {
            string filePath = Path.Combine(TestSetup.SampleDir, "ex3.jpg");
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                uint checksum = Crc32Checksum.Crc32(fs);
                Assert.IsTrue(checksum == 0x63D4D64B);
            }
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Crc32Checksum_8()
        {
            byte[] sample1 = Encoding.UTF8.GetBytes("ABC");
            byte[] sample2 = Encoding.UTF8.GetBytes("DEF");

            using (MemoryStream ms1 = new MemoryStream(sample1))
            using (MemoryStream ms2 = new MemoryStream(sample2))
            {
                uint checksum = Crc32Checksum.Crc32(ms1);
                Assert.IsTrue(checksum == 0xA3830348); // ABC
                checksum = Crc32Checksum.Crc32(checksum, ms2);
                Assert.IsTrue(checksum == 0xBB76FE69); // ABCDEF
            }
        }
        #endregion

        #region Adler32Stream
        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Adler32Stream_1()
        {
            string filePath = Path.Combine(TestSetup.SampleDir, "ex1.jpg");
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream ms = new MemoryStream())
            using (Adler32Stream adler = new Adler32Stream(ms))
            {
                fs.CopyTo(adler);
                Assert.IsTrue(adler.Checksum == 0xD77C7044);
            }
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Adler32Stream_2()
        {
            string filePath = Path.Combine(TestSetup.SampleDir, "ex2.jpg");
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream ms = new MemoryStream())
            using (Adler32Stream adler = new Adler32Stream(ms))
            {
                fs.CopyTo(adler);
                Assert.IsTrue(adler.Checksum == 0x9B97EDAD);
            }
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Adler32Stream_3()
        {
            string filePath = Path.Combine(TestSetup.SampleDir, "ex3.jpg");
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (MemoryStream ms = new MemoryStream())
            using (Adler32Stream adler = new Adler32Stream(ms))
            {
                fs.CopyTo(adler);
                Assert.IsTrue(adler.Checksum == 0x94B04C6F);
            }
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Adler32Stream_4()
        {
            using (MemoryStream ms = new MemoryStream())
            using (Adler32Stream adler = new Adler32Stream(ms))
            {
                byte[] bin = Encoding.UTF8.GetBytes("ABCDEF");
                adler.Write(bin, 0, bin.Length);
                Assert.IsTrue(adler.Checksum == 0x057E0196);
            }
        }
        #endregion

        #region Adler32Checksum
        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Adler32Checksum_1()
        {
            void Template(string path, uint checksum)
            {
                string filePath = Path.Combine(TestSetup.SampleDir, path);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Adler32Checksum adler = new Adler32Checksum();
                    adler.Append(fs);
                    Assert.IsTrue(adler.Checksum == checksum);
                }
            }

            Template("ex1.jpg", 0xD77C7044);
            Template("ex2.jpg", 0x9B97EDAD);
            Template("ex3.jpg", 0x94B04C6F);
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Adler32Checksum_2()
        {
            Adler32Checksum adler = new Adler32Checksum();

            byte[] buf = Encoding.UTF8.GetBytes("ABC");
            adler.Append(buf, 0, buf.Length);
            Assert.IsTrue(adler.Checksum == 0x018D00C7); // ABC

            buf = Encoding.UTF8.GetBytes("DEF");
            adler.Append(buf, 0, buf.Length);
            Assert.IsTrue(adler.Checksum == 0x057E0196); // ABCDEF
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Adler32Checksum_3()
        {
            byte[] buf = Encoding.UTF8.GetBytes("ABC");
            uint checksum = Adler32Checksum.Adler32(buf, 0, buf.Length);
            Assert.IsTrue(checksum == 0x018D00C7); // ABC

            buf = Encoding.UTF8.GetBytes("DEF");
            checksum = Adler32Checksum.Adler32(checksum, buf, 0, buf.Length);
            Assert.IsTrue(checksum == 0x057E0196); // ABCDEF
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Adler32Checksum_4()
        {
            byte[] sample = Encoding.UTF8.GetBytes("ABCDEF");

            uint checksum = Adler32Checksum.Adler32(sample, 1, 3);
            Assert.IsTrue(checksum == 0x019300CA); // BCD

            checksum = Adler32Checksum.Adler32(sample.AsSpan().Slice(1, 3));
            Assert.IsTrue(checksum == 0x019300CA); // BCD
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Adler32Checksum_5()
        {
            string filePath = Path.Combine(TestSetup.SampleDir, "ex3.jpg");
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                uint checksum = Adler32Checksum.Adler32(fs);
                Assert.IsTrue(checksum == 0x94B04C6F);
            }
        }

        [TestMethod]
        [TestCategory("Joveler.Compression.ZLib")]
        public void Adler32Checksum_6()
        {
            byte[] sample1 = Encoding.UTF8.GetBytes("ABC");
            byte[] sample2 = Encoding.UTF8.GetBytes("DEF");

            using (MemoryStream ms1 = new MemoryStream(sample1))
            using (MemoryStream ms2 = new MemoryStream(sample2))
            {
                uint checksum = Adler32Checksum.Adler32(ms1);
                Assert.IsTrue(checksum == 0x018D00C7); // ABC
                checksum = Adler32Checksum.Adler32(checksum, ms2);
                Assert.IsTrue(checksum == 0x057E0196); // ABCDEF
            }
        }
        #endregion
    }
}
