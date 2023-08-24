using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    #region DecompBench
    [Config(typeof(ParamOrderConfig))]
    public class DecompBench
    {
        #region Fields and Properties
        private string _sampleDir;
        private string _destDir;
        #endregion

        #region Parameterization
        // SrcFiles
        [ParamsSource(nameof(SrcFileNames))]
        public string SrcFileName { get; set; }
        public IReadOnlyList<string> SrcFileNames { get; set; } = new List<string>(BenchSamples.SampleFileNames);
        /// <summary>
        /// Cache raw source files to memory to minimize I/O bottleneck.
        /// </summary>
        public Dictionary<string, byte[]> SrcFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        // Levels
        [ParamsSource(nameof(Levels))]
        public string Level { get; set; }
        public IReadOnlyList<string> Levels { get; set; } = new string[]
        {
            "Fastest",
            "Default",
            "Best",
        };
        #endregion

        #region Setup and Cleanup
        private void GlobalSetup()
        {
            _sampleDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "Samples"));

            _destDir = Path.GetTempFileName();
            File.Delete(_destDir);
            Directory.CreateDirectory(_destDir);

            foreach (string level in Levels)
            {
                foreach (string srcFileName in SrcFileNames)
                {
                    foreach (string ext in new string[] { ".zz", ".xz", ".lz4", ".zst" })
                    {
                        string srcFile = Path.Combine(_sampleDir, level, srcFileName + ext);
                        using MemoryStream ms = new MemoryStream();
                        using (FileStream fs = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            fs.CopyTo(ms);
                        }

                        SrcFiles[$"{level}_{srcFileName}{ext}"] = ms.ToArray();
                    }
                }
            }
        }

        [GlobalSetup(Targets = new string[] { nameof(ZLibNgNativeJoveler) })]
        public void ZLibNgSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.ZLibNg);

            GlobalSetup();
        }

        [GlobalSetup(Targets = new string[] { nameof(ZLibUpNativeJoveler) })]
        public void ZLibUpSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.ZLibUp);

            GlobalSetup();
        }

        [GlobalSetup(Targets = new string[] { nameof(XZSingleNativeJoveler), nameof(XZMultiNativeJoveler) })]
        public void XZSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.XZ);

            GlobalSetup();
        }

        [GlobalSetup(Targets = new string[] { nameof(LZ4NativeJoveler) })]
        public void LZ4Setup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.LZ4);

            GlobalSetup();
        }

        [GlobalSetup(Targets = new string[] { nameof(ZstdNativeJoveler) })]
        public void ZstdSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.Zstd);

            GlobalSetup();
        }

        [GlobalSetup]
        public void ManagedSetup()
        {
            GlobalSetup();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            if (Directory.Exists(_destDir))
                Directory.Delete(_destDir);
            Program.NativeGlobalCleanup();
        }
        #endregion

        #region Benchmark - zlib
        public long ZLibNativeJoveler()
        {
            Joveler.Compression.ZLib.ZLibDecompressOptions decompOpts = new Joveler.Compression.ZLib.ZLibDecompressOptions();

            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.zz"];
            using MemoryStream ms = new MemoryStream();
            using (MemoryStream rms = new MemoryStream(compData))
            using (Joveler.Compression.ZLib.ZLibStream zs = new Joveler.Compression.ZLib.ZLibStream(rms, decompOpts))
            {
                zs.CopyTo(ms);
            }

            ms.Flush();
            return ms.Length;
        }

        [Benchmark(Description = "zlib-ng (n_Joveler)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public long ZLibNgNativeJoveler()
        {
            return ZLibNativeJoveler();
        }

        [Benchmark(Description = "zlib (n_Joveler)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public long ZLibUpNativeJoveler()
        {
            return ZLibNativeJoveler();
        }

        [Benchmark(Description = "zlib (n_BCL)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public long ZLibNativeBcl()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.zz"];
            using MemoryStream ms = new MemoryStream();
            using (MemoryStream rms = new MemoryStream(compData))
            using (System.IO.Compression.ZLibStream zs = new System.IO.Compression.ZLibStream(rms, System.IO.Compression.CompressionMode.Decompress, true))
            {
                zs.CopyTo(ms);
            }

            ms.Flush();
            return ms.Length;
        }

        [Benchmark(Description = "zlib (m_SharpCompress)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public long ZLibManagedSharpCompress()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.zz"];
            using MemoryStream ms = new MemoryStream();
            using (MemoryStream rms = new MemoryStream(compData))
            using (SharpCompress.Compressors.Deflate.ZlibStream zs = new SharpCompress.Compressors.Deflate.ZlibStream(rms, SharpCompress.Compressors.CompressionMode.Decompress))
            {
                zs.CopyTo(ms);
            }

            ms.Flush();
            return ms.Length;
        }
        #endregion

        #region Benchmark - xz-utils
        [Benchmark(Description = "xz (n_Joveler)")]
        [BenchmarkCategory(BenchConfig.XZ)]
        public long XZSingleNativeJoveler()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.xz"];
            using MemoryStream ms = new MemoryStream();
            Joveler.Compression.XZ.XZDecompressOptions decompOpts = new Joveler.Compression.XZ.XZDecompressOptions();
            using (MemoryStream rms = new MemoryStream(compData))
            using (Joveler.Compression.XZ.XZStream zs = new Joveler.Compression.XZ.XZStream(rms, decompOpts))
            {
                zs.CopyTo(ms);
            }

            ms.Flush();
            return ms.Length;
        }

        [Benchmark(Description = "xz-T0 (n_Joveler)")]
        [BenchmarkCategory(BenchConfig.XZ)]
        public long XZMultiNativeJoveler()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.xz"];
            using MemoryStream ms = new MemoryStream();
            Joveler.Compression.XZ.XZDecompressOptions decompOpts = new Joveler.Compression.XZ.XZDecompressOptions();
            Joveler.Compression.XZ.XZThreadedDecompressOptions threadOpts = new Joveler.Compression.XZ.XZThreadedDecompressOptions()
            {
                Threads = Environment.ProcessorCount,
            };
            using (MemoryStream rms = new MemoryStream(compData))
            using (Joveler.Compression.XZ.XZStream zs = new Joveler.Compression.XZ.XZStream(rms, decompOpts, threadOpts))
            {
                zs.CopyTo(ms);
            }

            ms.Flush();
            return ms.Length;
        }

        [Benchmark(Description = "xz (m_SharpCompress)")]
        [BenchmarkCategory(BenchConfig.XZ)]
        public long XZManagedSharpCompress()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.xz"];
            using MemoryStream ms = new MemoryStream();
            using (MemoryStream rms = new MemoryStream(compData))
            using (SharpCompress.Compressors.Xz.XZStream zs = new SharpCompress.Compressors.Xz.XZStream(rms))
            {
                zs.CopyTo(ms);
            }

            ms.Flush();
            return ms.Length;
        }
        #endregion

        #region Benchmark - lz4
        [Benchmark(Description = "lz4 (n_Joveler)")]
        [BenchmarkCategory(BenchConfig.LZ4)]
        public long LZ4NativeJoveler()
        {
            Joveler.Compression.LZ4.LZ4FrameDecompressOptions decompOpts = new Joveler.Compression.LZ4.LZ4FrameDecompressOptions();

            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.lz4"];
            using MemoryStream ms = new MemoryStream();
            using (MemoryStream rms = new MemoryStream(compData))
            using (Joveler.Compression.LZ4.LZ4FrameStream zs = new Joveler.Compression.LZ4.LZ4FrameStream(rms, decompOpts))
            {
                zs.CopyTo(ms);
            }

            ms.Flush();
            return ms.Length;
        }

        [Benchmark(Description = "lz4 (m_K4os)")]
        [BenchmarkCategory(BenchConfig.LZ4)]
        public long LZ4ManagedK4os()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.lz4"];
            using MemoryStream ms = new MemoryStream();
            using (MemoryStream rms = new MemoryStream(compData))
            using (K4os.Compression.LZ4.Streams.LZ4DecoderStream zs = K4os.Compression.LZ4.Streams.LZ4Stream.Decode(rms, 0))
            {
                zs.CopyTo(ms);
            }

            ms.Flush();
            return ms.Length;
        }
        #endregion

        #region Benchmark - zstd
        [Benchmark(Description = "zstd (n_Joveler)")]
        [BenchmarkCategory(BenchConfig.Zstd)]
        public long ZstdNativeJoveler()
        {
            Joveler.Compression.Zstd.ZstdDecompressOptions decompOpts = new Joveler.Compression.Zstd.ZstdDecompressOptions();

            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.zst"];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(compData))
                using (Joveler.Compression.Zstd.ZstdStream zs = new Joveler.Compression.Zstd.ZstdStream(rms, decompOpts))
                {
                    zs.CopyTo(ms);
                }

                ms.Flush();
                return ms.Length;
            }
        }

        [Benchmark(Description = "zstd (m_ZstdSharp)")]
        [BenchmarkCategory(BenchConfig.Zstd)]
        public long ZstdManagedZstdSharp()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.zst"];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(compData))
                using (ZstdSharp.DecompressionStream zs = new ZstdSharp.DecompressionStream(rms))
                {
                    zs.CopyTo(ms);
                }

                ms.Flush();
                return ms.Length;
            }
        }
        #endregion
    }
    #endregion
}
