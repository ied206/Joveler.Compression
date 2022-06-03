using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    #region DecompBench
    [Config(typeof(BenchConfig))]
    public class DecompBench
    {
        private string _sampleDir;
        private string _destDir;

        // SrcFiles
        [ParamsSource(nameof(SrcFileNames))]
        public string SrcFileName { get; set; }
        public IReadOnlyList<string> SrcFileNames { get; set; } = new string[]
        {
            "Banner.bmp",
            "Banner.svg",
            "Type4.txt",
        };
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

        [GlobalSetup]
        public void GlobalSetup()
        {
            Program.NativeGlobalInit();

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

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            if (Directory.Exists(_destDir))
                Directory.Delete(_destDir);
            Program.NativeGlobalCleanup();
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.LZ4)]
        public long LZ4_Native()
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

        [Benchmark]
        [BenchmarkCategory(BenchConfig.LZ4)]
        public long LZ4_Managed()
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

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public long ZLib_Native()
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

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public long ZLib_Managed()
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

        [Benchmark]
        [BenchmarkCategory(BenchConfig.XZ)]
        public long XZ_Native()
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

        [Benchmark]
        [BenchmarkCategory(BenchConfig.XZ)]
        public long XZ_Managed()
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

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZSTD)]
        public long ZSTD_Native()
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

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZSTD)]
        public long ZSTD_Managed()
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
    }
    #endregion
}
