using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Joveler.Compression.XZ.Checksum;

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
        private void AppendTemplate<T>(BaseChecksum<T> check, string fileName, TestKind kind, T expected)
        {
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

                Console.WriteLine($"(Append) Expected   checksum of {fileName} : 0x{expected:X16}");
                Console.WriteLine($"(Append) Calculated checksum of {fileName} : 0x{check.Checksum:X16}");
                Assert.AreEqual(expected, check.Checksum);
            }
            finally
            {
                check.Reset();
            }
        }

        private void ComputeTemplate<T>(BaseChecksum<T> check, string fileName, TestKind kind, T expected)
        {
            T checksum = check.InitChecksum;

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
                                checksum = check.Compute(checksum, buffer, 0, bytesRead);
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
                                checksum = check.Compute(checksum, buffer.AsSpan(0, bytesRead));
                            }
                            while (0 < bytesRead);
                        }
                        break;
                    case TestKind.Stream:
                        checksum = check.Compute(checksum, fs);
                        break;
                }
            }

            Console.WriteLine($"(Compute) Expected   checksum of {fileName} : 0x{expected:X16}");
            Console.WriteLine($"(Compute) Calculated checksum of {fileName} : 0x{check.Checksum:X16}");;
            Assert.AreEqual(expected, checksum);
        }
        #endregion

        #region Crc32
        [TestMethod]
        public void Crc32Checksum()
        {
            Crc32Checksum crc32 = new Crc32Checksum();
            AppendTemplate(crc32, "A.pdf", TestKind.Array, 0x07A6FCC5u);
            AppendTemplate(crc32, "B.txt", TestKind.Span, 0x675845AEu);
            AppendTemplate(crc32, "C.bin", TestKind.Stream, 0x70047868u);
            ComputeTemplate(crc32, "A.pdf", TestKind.Array, 0x07A6FCC5u);
            ComputeTemplate(crc32, "B.txt", TestKind.Span, 0x675845AEu);
            ComputeTemplate(crc32, "C.bin", TestKind.Stream, 0x70047868u);
        }
        #endregion

        #region Crc64
        [TestMethod]
        public void Crc64Checksum()
        {
            Crc64Checksum crc64 = new Crc64Checksum();
            AppendTemplate(crc64, "A.pdf", TestKind.Array, 0x70DAC0EC5A353DCELu);
            AppendTemplate(crc64, "B.txt", TestKind.Span, 0x221708D24F085975Lu);
            AppendTemplate(crc64, "C.bin", TestKind.Stream, 0x56C3415F06F17315Lu);
            ComputeTemplate(crc64, "A.pdf", TestKind.Array, 0x70DAC0EC5A353DCELu);
            ComputeTemplate(crc64, "B.txt", TestKind.Span, 0x221708D24F085975Lu);
            ComputeTemplate(crc64, "C.bin", TestKind.Stream, 0x56C3415F06F17315Lu);
        }
        #endregion
    }
}
