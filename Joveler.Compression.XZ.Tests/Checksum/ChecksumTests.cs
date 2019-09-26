using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Joveler.Compression.XZ.Checksum;
using System.Security.Cryptography;

namespace Joveler.Compression.XZ.Tests.Checksum
{
    [TestClass]
    [TestCategory("Joveler.Compression.XZ")]
    public class ChecksumTests
    {
        #region (private) TestKind
        private enum TestKind
        {
            Array,
            Span,
            Stream,
        }
        #endregion

        #region Template
        private void CheckTemplate<T>(BaseChecksum<T> check, string fileName, TestKind kind, T expected)
        {
            check.Reset();
            try
            {
                string filePath = Path.Combine(TestSetup.SampleDir, fileName);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    switch (kind)
                    {
                        case TestKind.Array:
                            {
                                int bytesRead = 0;
                                byte[] buffer = new byte[64 * 1024];
                                do
                                {
                                    bytesRead = fs.Read(buffer, 0, buffer.Length);
                                    check.Append(buffer, 0, bytesRead);
                                }
                                while (0 < bytesRead);
                            }
                            break;
                        case TestKind.Span:
                            {
                                int bytesRead = 0;
                                byte[] buffer = new byte[64 * 1024];
                                do
                                {
                                    bytesRead = fs.Read(buffer, 0, buffer.Length);
                                    check.Append(buffer.AsSpan(0, bytesRead));
                                }
                                while (0 < bytesRead);
                            }
                            break;
                        case TestKind.Stream:
                            check.Append(fs);
                            break;
                    }
                }

                Console.WriteLine($"(Check) Expected   checksum of {fileName} : 0x{expected:X16}");
                Console.WriteLine($"(Check) Calculated checksum of {fileName} : 0x{check.Checksum:X16}");
                Assert.AreEqual(expected, check.Checksum);
            }
            finally
            {
                check.Reset();
            }
        }

        private void HashAlgorithmTemplate<T>(HashAlgorithm hash, string fileName, TestKind kind, T expected)
        {
            /*
            hash.Clear();
            try
            {
                byte[] checksum;
                string filePath = Path.Combine(TestSetup.SampleDir, fileName);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] buffer = new byte[fs.Length];
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);
                    checksum = hash.ComputeHash(buffer, 0, bytesRead);
                }

                StringBuilder b = new StringBuilder(8);
                foreach (byte c in checksum)
                    b.Append(c.ToString("X2"));
                string checkStr = b.ToString();

                foreach (byte e in BitConverter.GetBytes(expected))
                    b.Append(c.ToString("X2"));
                string expectStr = b.ToString();

                Console.WriteLine($"(Hash) Expected   checksum of {fileName} : 0x{expected:X16}");
                Console.WriteLine($"(Hash) Calculated checksum of {fileName} : 0x{checkStr:X16}");
                Assert.AreEqual(expected, checkStr);
            }
            finally
            {
                hash.Clear();
            }
            */
        }
        #endregion

        #region Crc32
        [TestMethod]
        public void Crc32Checksum()
        {
            Crc32Checksum crc32 = new Crc32Checksum();
            CheckTemplate(crc32, "A.pdf", TestKind.Array, 0x07A6FCC5u);
            CheckTemplate(crc32, "B.txt", TestKind.Span, 0x675845AEu);
            CheckTemplate(crc32, "C.bin", TestKind.Stream, 0x70047868u);
        }
        #endregion

        #region Crc64
        [TestMethod]
        public void Crc64Checksum()
        {
            Crc64Checksum crc64 = new Crc64Checksum();
            CheckTemplate(crc64, "A.pdf", TestKind.Array, 0x70DAC0EC5A353DCELu);
            CheckTemplate(crc64, "B.txt", TestKind.Span, 0x221708D24F085975Lu);
            CheckTemplate(crc64, "C.bin", TestKind.Stream, 0x56C3415F06F17315Lu);
            //ComputeTemplate(crc64, "A.pdf", TestKind.Array, 0x70DAC0EC5A353DCELu);
            //ComputeTemplate(crc64, "B.txt", TestKind.Span, 0x221708D24F085975Lu);
            //ComputeTemplate(crc64, "C.bin", TestKind.Stream, 0x56C3415F06F17315Lu);
        }
        #endregion
    }
}
