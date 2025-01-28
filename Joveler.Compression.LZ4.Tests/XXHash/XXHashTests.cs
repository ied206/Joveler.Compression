/*
    Written by Hajin Jang (BSD 2-Clause)
    Copyright (C) 2025-present Hajin Jang

    Redistribution and use in source and binary forms, with or without modification,
    are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this
      list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice, this
      list of conditions and the following disclaimer in the documentation and/or
      other materials provided with the distribution.

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

using Joveler.Compression.LZ4.XXHash;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Joveler.Compression.LZ4.Tests.XXHash
{
    [TestClass]
    public class XXHashTests
    {
        #region Template
        private enum TestKind
        {
            Array,
            Span,
            Stream,
        }

        private static void CheckTemplate<T>(XXHashStreamBase<T> xxhStream, string fileName, TestKind kind, T expected) where T : unmanaged
        {
            xxhStream.Reset();
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
                                    xxhStream.Write(buffer, 0, bytesRead);
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
                                    xxhStream.Write(buffer.AsSpan(0, bytesRead));
                                }
                                while (0 < bytesRead);
                            }
                            break;
                        case TestKind.Stream:
                            fs.CopyTo(xxhStream);
                            break;
                    }
                }

                Console.WriteLine($"(Check) Expected   hash of {fileName} : 0x{expected:X16}");
                Console.WriteLine($"(Check) Calculated hash of {fileName} : 0x{xxhStream.HashValue:X16}");
                Assert.AreEqual(expected, xxhStream.HashValue);
            }
            finally
            {
                xxhStream.Reset();
            }
        }

        private static void HashAlgorithmTemplate(HashAlgorithm hash, string fileName, byte[] expectedBytes)
        {
            byte[] actualBytes;
            string filePath = Path.Combine(TestSetup.SampleDir, fileName);
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[fs.Length];
                int bytesRead = fs.Read(buffer, 0, buffer.Length);
                actualBytes = hash.ComputeHash(buffer, 0, bytesRead);
            }

            Console.WriteLine($"(Hash) Expected   hash of {fileName} : 0x{BitConverter.ToString(expectedBytes).Replace("-", string.Empty)}");
            Console.WriteLine($"(Hash) Calculated hash of {fileName} : 0x{BitConverter.ToString(actualBytes).Replace("-", string.Empty)}");
            Assert.IsTrue(expectedBytes.SequenceEqual(actualBytes));
        }

        private static void StreamTemplate<T>(XXHashStreamBase<T> xxhStream, string fileName, T expected, byte[] expectedBytesLE, byte[] expectedBytesBE) where T : unmanaged
        {
            string filePath = Path.Combine(TestSetup.SampleDir, fileName);
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.CopyTo(xxhStream);
            }
            T actualVal = xxhStream.HashValue;
            byte[] actualBytesLE = xxhStream.HashBytesLE;
            byte[] actualBytesBE = xxhStream.HashBytesBE;
            byte[] actualBufLE = new byte[xxhStream.HashValueSize];
            byte[] actualBufBE = new byte[xxhStream.HashValueSize];
            xxhStream.GetHashBytesLE(actualBufLE);
            xxhStream.GetHashBytesBE(actualBufBE);

            Assert.AreEqual(Marshal.SizeOf(typeof(T)), xxhStream.HashValueSize);
            Console.WriteLine($"(Check) Expected   hash of {fileName} : 0x{expected:X16}");
            Console.WriteLine($"(Check) Calculated hash of {fileName} : 0x{actualVal:X16}");
            Assert.AreEqual(expected, actualVal);
            Assert.IsTrue(actualBytesLE.SequenceEqual(expectedBytesLE));
            Assert.IsTrue(actualBytesBE.SequenceEqual(expectedBytesBE));
            Assert.IsTrue(actualBufLE.SequenceEqual(expectedBytesLE));
            Assert.IsTrue(actualBufBE.SequenceEqual(expectedBytesBE));

            bool exceptThrown = false;
            try
            {
                byte[] emptyBuf = Array.Empty<byte>();
                xxhStream.Reset();
                xxhStream.GetHashBytesLE(emptyBuf);
            }
            catch (Exception)
            {
                exceptThrown = true;
            }
            Assert.IsTrue(exceptThrown);
            
        }
        #endregion

        #region XXH32
        [TestMethod]
        public void XXH32()
        {
            (string FileName, uint Checksum)[] samples =
            {
                ("A.pdf", 0xda4e798cu),
                ("B.txt", 0x1d17bcd7u),
                ("C.bin", 0x0c1b3891u),
            };

            XXH32Stream xxh32 = new XXH32Stream();
            foreach ((string fileName, uint expected) in samples)
            {
                byte[] expectedBytesLE = new byte[xxh32.HashValueSize];
                BinaryPrimitives.WriteUInt32LittleEndian(expectedBytesLE, expected);

                byte[] expectedBytesBE = new byte[xxh32.HashValueSize];
                BinaryPrimitives.WriteUInt32BigEndian(expectedBytesBE, expected);

                foreach (TestKind kind in Enum.GetValues(typeof(TestKind)))
                {
                    CheckTemplate(xxh32, fileName, kind, expected);
                }

                using (XXH32Algorithm hash = new XXH32Algorithm(ByteOrder.LittleEndian))
                {
                    HashAlgorithmTemplate(hash, fileName, expectedBytesLE);
                }

                using (XXH32Algorithm hash = new XXH32Algorithm(ByteOrder.BigEndian))
                {
                    HashAlgorithmTemplate(hash, fileName, expectedBytesBE);
                }

                using (XXH32Stream hashStream = new XXH32Stream())
                {
                    StreamTemplate(hashStream, fileName, expected, expectedBytesLE, expectedBytesBE);
                }
            }
        }
        #endregion

        #region XXH64
        [TestMethod]
        public void XXH64()
        {
            (string FileName, ulong Checksum)[] samples = 
            {
                ("A.pdf", 0xc1937d274a057194Lu),
                ("B.txt", 0x3eac76f46cd95d27Lu),
                ("C.bin", 0xfb90abed73542d2aLu),
            };

            XXH64Stream xxh64 = new XXH64Stream();
            foreach ((string fileName, ulong expected) in samples)
            {
                byte[] expectedBytesLE = new byte[xxh64.HashValueSize];
                BinaryPrimitives.WriteUInt64LittleEndian(expectedBytesLE, expected);

                byte[] expectedBytesBE = new byte[xxh64.HashValueSize];
                BinaryPrimitives.WriteUInt64BigEndian(expectedBytesBE, expected);

                foreach (TestKind kind in Enum.GetValues(typeof(TestKind)))
                {
                    CheckTemplate(xxh64, fileName, kind, expected);
                }

                using (XXH64Algorithm hash = new XXH64Algorithm(ByteOrder.LittleEndian))
                {
                    HashAlgorithmTemplate(hash, fileName, expectedBytesLE);
                }

                using (XXH64Algorithm hash = new XXH64Algorithm(ByteOrder.BigEndian))
                {
                    HashAlgorithmTemplate(hash, fileName, expectedBytesBE);
                }

                using (XXH64Stream hashStream = new XXH64Stream())
                {
                    StreamTemplate(hashStream, fileName, expected, expectedBytesLE, expectedBytesBE);
                }
            }
        }
        #endregion
    }
}
