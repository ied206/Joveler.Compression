/*
    C# tests by Hajin Jang
    Copyright (C) 2017-present Hajin Jang

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

using Joveler.Compression.ZLib.Checksum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Joveler.Compression.ZLib.Tests.Checksum
{
    [TestClass]
    [DoNotParallelize]
    public class ChecksumUpCdeclTests : ChecksumTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.UpstreamCdecl;
    }

    [TestClass]
    [DoNotParallelize]
    public class ChecksumUpStdcallTests : ChecksumTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.UpstreamStdcall;
    }

    [TestClass]
    [DoNotParallelize]
    public class ChecksumNgCdeclTests : ChecksumTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.ZLibNgCdecl;
    }

    public abstract class ChecksumTestsBase : ZLibTestBase
    {
        #region Template
        private enum TestKind
        {
            Array,
            Span,
            Stream,
        }

        private static void CheckTemplate<T>(ChecksumBase<T> check, string fileName, TestKind kind, T expected) where T : unmanaged
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

        private static void HashAlgorithmTemplate(HashAlgorithm hash, string fileName, ulong expected)
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
                ulong actual = BitConverter.ToUInt64(checksum, 0);
                Console.WriteLine($"(Hash) Expected   checksum of {fileName} : 0x{expected:X16}");
                Console.WriteLine($"(Hash) Calculated checksum of {fileName} : 0x{actual:X16}");
                Assert.AreEqual(expected, actual);
            }
            else if (checksum.Length == 4)
            {
                uint actual = BitConverter.ToUInt32(checksum, 0);
                Console.WriteLine($"(Hash) Expected   checksum of {fileName} : 0x{expected:X8}");
                Console.WriteLine($"(Hash) Calculated checksum of {fileName} : 0x{actual:X8}");
                Assert.AreEqual((uint)expected, actual);
            }
            else
            {
                Assert.Fail();
            }
        }

        private void ResetTemplate<T>(ChecksumBase<T> check, string firstFileName, string secondFileName) where T : unmanaged
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
            (string FileName, uint Checksum)[] samples =
            {
                ("ex1.jpg", 0x1961D0C6u),
                ("ex2.jpg", 0x7641A243u),
                ("ex3.jpg", 0x63D4D64Bu),
            };

            Crc32Checksum crc32 = new Crc32Checksum();
            foreach ((string fileName, uint checksum) in samples)
            {
                foreach (TestKind kind in Enum.GetValues(typeof(TestKind)))
                {
                    CheckTemplate(crc32, fileName, kind, checksum);
                }

                using (Crc32Algorithm hash = new Crc32Algorithm())
                {
                    HashAlgorithmTemplate(hash, fileName, checksum);
                }
            }

            ResetTemplate(crc32, samples[0].FileName, samples[1].FileName);
        }

        [TestMethod]
        public void Crc32Combine()
        {
            UTF8Encoding enc = new UTF8Encoding(false);

            const string firstStr = "1234567890";
            const string secondStr = "Joveler.Compression";
            byte[] firstBytes = enc.GetBytes(firstStr);
            byte[] secondBytes = enc.GetBytes(secondStr);
            const uint firstChecksum = 0x261daee5u;
            const uint secondChecksum = 0xda4161e5u;
            const uint combinedChecksum = 0xbe17cafdu;

            Crc32Checksum check1 = new Crc32Checksum();
            check1.Append(firstBytes);
            Console.WriteLine($"(Hash) Expected   checksum of \"{firstStr}\" : 0x{firstChecksum:X8}");
            Console.WriteLine($"(Hash) Calculated checksum of \"{firstStr}\" : 0x{check1.Checksum:X8}");
            Assert.AreEqual(firstChecksum, check1.Checksum);
            check1.Append(secondBytes);
            Assert.AreEqual(combinedChecksum, check1.Checksum);

            Crc32Checksum check2 = new Crc32Checksum();
            check2.Append(secondBytes);
            Console.WriteLine($"(Hash) Expected   checksum of \"{secondStr}\" : 0x{secondChecksum:X8}");
            Console.WriteLine($"(Hash) Calculated checksum of \"{secondStr}\" : 0x{check2.Checksum:X8}");
            Assert.AreEqual(secondChecksum, check2.Checksum);

            Crc32Checksum check3 = new Crc32Checksum();
            check3.Reset(firstChecksum);
            check3.Combine(check2.Checksum, secondBytes.Length);
            Console.WriteLine($"(Hash) Expected   checksum of \"{firstStr}{secondStr}\" : 0x{combinedChecksum:X8}");
            Console.WriteLine($"(Hash) Calculated checksum of \"{firstStr}{secondStr}\" : 0x{check3.Checksum:X8}");
            Assert.AreEqual(combinedChecksum, check3.Checksum);
        }
        #endregion

        #region Adler32
        [TestMethod]
        public void Adler32()
        {
            (string FileName, uint Checksum)[] samples = 
            {
                ("ex1.jpg", 0xD77C7044u),
                ("ex2.jpg", 0x9B97EDADu),
                ("ex3.jpg", 0x94B04C6Fu),
            };

            Adler32Checksum adler32 = new Adler32Checksum();
            foreach ((string fileName, uint checksum) in samples)
            {
                foreach (TestKind kind in Enum.GetValues(typeof(TestKind)))
                {
                    CheckTemplate(adler32, fileName, kind, checksum);
                }

                using (Adler32Algorithm hash = new Adler32Algorithm())
                {
                    HashAlgorithmTemplate(hash, fileName, checksum);
                }
            }

            ResetTemplate(adler32, samples[0].FileName, samples[1].FileName);
        }

        [TestMethod]
        public void Adler32Combine()
        {
            UTF8Encoding enc = new UTF8Encoding(false);

            const string firstStr = "1234567890";
            const string secondStr = "Joveler.Compression";
            byte[] firstBytes = enc.GetBytes(firstStr);
            byte[] secondBytes = enc.GetBytes(secondStr);
            const uint firstChecksum = 0x0b2c020eu;
            const uint secondChecksum = 0x49eb0798u;
            const uint combinedChecksum = 0x7c0e09a5u;

            Adler32Checksum check1 = new Adler32Checksum();
            check1.Append(firstBytes);
            Console.WriteLine($"(Hash) Expected   checksum of \"{firstStr}\" : 0x{firstChecksum:X8}");
            Console.WriteLine($"(Hash) Calculated checksum of \"{firstStr}\" : 0x{check1.Checksum:X8}");
            Assert.AreEqual(firstChecksum, check1.Checksum);
            check1.Append(secondBytes);
            Assert.AreEqual(combinedChecksum, check1.Checksum);

            Adler32Checksum check2 = new Adler32Checksum();
            check2.Append(secondBytes);
            Console.WriteLine($"(Hash) Expected   checksum of \"{secondStr}\" : 0x{secondChecksum:X8}");
            Console.WriteLine($"(Hash) Calculated checksum of \"{secondStr}\" : 0x{check2.Checksum:X8}");
            Assert.AreEqual(secondChecksum, check2.Checksum);

            Adler32Checksum check3 = new Adler32Checksum();
            check3.Reset(firstChecksum);
            check3.Combine(check2.Checksum, secondBytes.Length);
            Console.WriteLine($"(Hash) Expected   checksum of \"{firstStr}{secondStr}\" : 0x{combinedChecksum:X8}");
            Console.WriteLine($"(Hash) Calculated checksum of \"{firstStr}{secondStr}\" : 0x{check3.Checksum:X8}");
            Assert.AreEqual(combinedChecksum, check3.Checksum);
        }
        #endregion
    }
}
