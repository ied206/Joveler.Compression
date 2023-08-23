using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    [Config(typeof(BenchConfig))]
    public class HashBench
    {
        #region Fields and Properties
        private string _sampleDir;

        // SrcFiles
        [ParamsSource(nameof(SrcFileNames))]
        public string SrcFileName { get; set; }
        public IReadOnlyList<string> SrcFileNames { get; set; } = new string[]
        {
            "Banner.bmp", // From PEBakery EncodedFile tests
            "Banner.svg", // From PEBakery EncodedFile tests
            "Type4.txt", // From PEBakery EncodedFile tests
            "bible_en_utf8.txt", // From Canterbury Corpus
            "bible_kr_cp949.txt", // Public Domain (개역한글)
            "bible_kr_utf8.txt", // Public Domain (개역한글)
            "bible_kr_utf16le.txt", // Public Domain (개역한글)
            "ooffice.dll", // From silesia corpus
            "reymont.pdf", // From silesia corpus
            "world192.txt", // From Canterbury corpus
        };
        public Dictionary<string, byte[]> SrcFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        #endregion

        #region Setup and Cleanup
        private void GlobalSetup()
        {
            _sampleDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "Samples"));

            foreach (string srcFileName in SrcFileNames)
            {
                string srcFile = Path.Combine(_sampleDir, "Raw", srcFileName);
                using MemoryStream ms = new MemoryStream();
                using (FileStream fs = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.CopyTo(ms);
                }

                SrcFiles[srcFileName] = ms.ToArray();
            }
        }

        [GlobalSetup(Targets = new string[] { nameof(Adler32_ZLibNgNativeJoveler), nameof(CRC32_ZLibNgNativeJoveler) })]
        public void ZLibNgSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.ZLibNg);

            GlobalSetup();
        }

        [GlobalSetup(Targets = new string[] { nameof(Adler32_ZLibUpNativeJoveler), nameof(CRC32_ZLibUpNativeJoveler) })]
        public void ZLibUpSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.ZLibUp);

            GlobalSetup();
        }
        
        [GlobalSetup(Targets = new string[] { nameof(CRC32_XZNativeJoveler), nameof(CRC64_XZNativeJoveler) })]
        public void XZSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.XZ);

            GlobalSetup();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            Program.NativeGlobalCleanup();
        }
        #endregion

        #region xxHash32
#pragma warning disable IDE1006 // 명명 스타일
        [Benchmark(Description = "xxHash32 (m_K4os)")]
        [BenchmarkCategory(BenchConfig.LZ4)]
        public uint xxHash32_ManagedK4os()
        {
            byte[] compData = SrcFiles[SrcFileName];
            K4os.Hash.xxHash.XXH32 xxh32 = new K4os.Hash.xxHash.XXH32();
            xxh32.Update(compData);
            return xxh32.Digest();
        }
#pragma warning restore IDE1006 // 명명 스타일
        #endregion

        #region xxHash64
#pragma warning disable IDE1006 // 명명 스타일
        [Benchmark(Description = "xxHash64 (m_K4os)")]
        [BenchmarkCategory(BenchConfig.LZ4)]
        public ulong xxHash64_ManagedK4os()
        {
            byte[] compData = SrcFiles[SrcFileName];
            K4os.Hash.xxHash.XXH64 xxh64 = new K4os.Hash.xxHash.XXH64();
            xxh64.Update(compData);
            return xxh64.Digest();
        }
#pragma warning restore IDE1006 // 명명 스타일
        #endregion

        #region Adler32
        private uint Adler32_ZLibNativeJoveler()
        {
            byte[] compData = SrcFiles[SrcFileName];
            Joveler.Compression.ZLib.Checksum.Adler32Checksum crc32 = new Joveler.Compression.ZLib.Checksum.Adler32Checksum();
            return crc32.Append(compData);
        }

        [Benchmark(Description = "adler32 (zlib-ng, n_Joveler)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public uint Adler32_ZLibNgNativeJoveler() => Adler32_ZLibNativeJoveler();

        [Benchmark(Description = "adler32 (zlib, n_Joveler)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public uint Adler32_ZLibUpNativeJoveler() => Adler32_ZLibNativeJoveler();
        #endregion

        #region CRC32
        
        private uint CRC32_ZLibNativeJoveler()
        {
            byte[] compData = SrcFiles[SrcFileName];
            Joveler.Compression.ZLib.Checksum.Crc32Checksum crc32 = new Joveler.Compression.ZLib.Checksum.Crc32Checksum();
            return crc32.Append(compData);
        }

        [Benchmark(Description = "crc32 (zlib-ng, n_Joveler)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public uint CRC32_ZLibNgNativeJoveler() => CRC32_ZLibNativeJoveler();

        [Benchmark(Description = "crc32 (zlib, n_Joveler)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public uint CRC32_ZLibUpNativeJoveler() => CRC32_ZLibNativeJoveler();

        [Benchmark(Description = "crc32 (xz, n_Joveler)")]
        [BenchmarkCategory(BenchConfig.XZ)]
        public uint CRC32_XZNativeJoveler()
        {
            byte[] compData = SrcFiles[SrcFileName];
            Joveler.Compression.XZ.Checksum.Crc32Checksum crc32 = new Joveler.Compression.XZ.Checksum.Crc32Checksum();
            return crc32.Append(compData);
        }

        [Benchmark(Description = "crc32 (m_Force)")]
        [BenchmarkCategory(BenchConfig.ZLib, BenchConfig.XZ)]
        public byte[] CRC32_ManagedForce()
        {
            byte[] compData = SrcFiles[SrcFileName];
            using Force.Crc32.Crc32Algorithm crc32 = new Force.Crc32.Crc32Algorithm();
            return crc32.ComputeHash(compData);
        }

        [Benchmark(Description = "crc32 (m_K4os)")]
        [BenchmarkCategory(BenchConfig.ZLib, BenchConfig.XZ)]
        public uint CRC32_ManagedK4os()
        {
            byte[] compData = SrcFiles[SrcFileName];
            K4os.Hash.Crc.Crc32 crc32 = new K4os.Hash.Crc.Crc32();
            crc32.Update(compData);
            return crc32.Digest();
        }
        #endregion

        #region CRC64
        [Benchmark(Description = "crc64 (xz, n_Joveler)")]
        [BenchmarkCategory(BenchConfig.XZ)]
        public ulong CRC64_XZNativeJoveler()
        {
            byte[] compData = SrcFiles[SrcFileName];
            Joveler.Compression.XZ.Checksum.Crc64Checksum crc64 = new Joveler.Compression.XZ.Checksum.Crc64Checksum();
            return crc64.Append(compData);
        }
        #endregion
    }
}
