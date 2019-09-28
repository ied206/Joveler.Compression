﻿using Joveler.Compression.XZ.Checksum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Joveler.Compression.XZ.Tests.Checksum
{
    [TestClass]
    [TestCategory("Joveler.Compression.XZ")]
    public class ChecksumTests
    {
        #region Template
        private enum TestKind
        {
            Array,
            Span,
            Stream,
        }

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

        private void HashAlgorithmTemplate<T>(HashAlgorithm hash, string fileName, T expected)
        {
            byte[] checksum;
            string filePath = Path.Combine(TestSetup.SampleDir, fileName);
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[fs.Length];
                int bytesRead = fs.Read(buffer, 0, buffer.Length);
                checksum = hash.ComputeHash(buffer, 0, bytesRead);
            }

            if (checksum.Length == 8)
            {
                ulong actual = BitConverter.ToUInt64(checksum);
                Console.WriteLine($"(Hash) Expected   checksum of {fileName} : 0x{expected:X16}");
                Console.WriteLine($"(Hash) Calculated checksum of {fileName} : 0x{actual:X16}");
                Assert.AreEqual(expected, actual);
            }
            else if (checksum.Length == 4)
            {
                uint actual = BitConverter.ToUInt32(checksum);
                Console.WriteLine($"(Hash) Expected   checksum of {fileName} : 0x{expected:X16}");
                Console.WriteLine($"(Hash) Calculated checksum of {fileName} : 0x{actual:X16}");
                Assert.AreEqual(expected, actual);
            }
            else
            {
                Assert.Fail();
            }
        }

        private void ResetTemplate<T>(BaseChecksum<T> check, string firstFileName, string secondFileName)
        {
            try
            {
                // Get first cheksum
                check.Reset();
                string firstFilePath = Path.Combine(TestSetup.SampleDir, firstFileName);
                using (FileStream fs = new FileStream(firstFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    check.Append(fs);
                }
                T firstCheck = check.Checksum;

                // Get concat cheksum
                string secondFilePath = Path.Combine(TestSetup.SampleDir, secondFileName);
                using (FileStream fs = new FileStream(secondFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    check.Append(fs);
                }
                T concatCheck = check.Checksum;

                // Reset and get concat checksum again
                check.Reset(firstCheck);
                using (FileStream fs = new FileStream(secondFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    check.Append(fs);
                }
                T actualCheck = check.Checksum;

                Console.WriteLine($"(Check) Expected   checksum : 0x{concatCheck:X16}");
                Console.WriteLine($"(Check) Calculated checksum : 0x{actualCheck:X16}");
                Assert.AreEqual(concatCheck, actualCheck);
            }
            finally
            {
                check.Reset();
            }
        }
        #endregion

        #region Crc32
        [TestMethod]
        public void Crc32()
        {
            (string FileName, uint Checksum)[] samples = new (string, uint)[]
            {
                ("A.pdf", 0x07A6FCC5u),
                ("B.txt", 0x675845AEu),
                ("C.bin", 0x70047868u),
            };

            Crc32Checksum crc = new Crc32Checksum();
            foreach ((string fileName, uint checksum) in samples)
            {
                foreach (TestKind kind in Enum.GetValues(typeof(TestKind)))
                {
                    CheckTemplate(crc, fileName, kind, checksum);
                }

                using Crc32Algorithm hash = new Crc32Algorithm();
                HashAlgorithmTemplate(hash, fileName, checksum);
            }

            ResetTemplate(crc, samples[0].FileName, samples[1].FileName);
        }
        #endregion

        #region Crc64
        [TestMethod]
        public void Crc64()
        {
            (string FileName, ulong Checksum)[] samples = new (string, ulong)[]
            {
                ("A.pdf", 0x70DAC0EC5A353DCELu),
                ("B.txt", 0x221708D24F085975Lu),
                ("C.bin", 0x56C3415F06F17315Lu),
            };

            Crc64Checksum crc = new Crc64Checksum();
            foreach ((string fileName, ulong checksum) in samples)
            {
                foreach (TestKind kind in Enum.GetValues(typeof(TestKind)))
                    CheckTemplate(crc, fileName, kind, checksum);

                using Crc64Algorithm hash = new Crc64Algorithm();
                HashAlgorithmTemplate(hash, fileName, checksum);
            }

            ResetTemplate(crc, samples[0].FileName, samples[1].FileName);
        }
        #endregion
    }
}
