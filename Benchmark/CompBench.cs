using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    #region CompBench
    [Config(typeof(BenchConfig))]
    public class CompBench
    {
        private string _sampleDir;
        private string _destDir;

        public double CompRatio { get; set; }

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

        // ZLibCompLevel
        public Dictionary<string, Joveler.Compression.ZLib.ZLibCompLevel> NativeZLibLevelDict = new Dictionary<string, Joveler.Compression.ZLib.ZLibCompLevel>(StringComparer.Ordinal)
        {
            ["Fastest"] = Joveler.Compression.ZLib.ZLibCompLevel.BestSpeed,
            ["Default"] = Joveler.Compression.ZLib.ZLibCompLevel.Default,
            ["Best"] = Joveler.Compression.ZLib.ZLibCompLevel.BestCompression,
        };

        public Dictionary<string, SharpCompress.Compressors.Deflate.CompressionLevel> ManagedZLibLevelDict = new Dictionary<string, SharpCompress.Compressors.Deflate.CompressionLevel>(StringComparer.Ordinal)
        {
            ["Fastest"] = SharpCompress.Compressors.Deflate.CompressionLevel.BestSpeed,
            ["Default"] = SharpCompress.Compressors.Deflate.CompressionLevel.Default,
            ["Best"] = SharpCompress.Compressors.Deflate.CompressionLevel.BestCompression,
        };

        // XZLevel
        public Dictionary<string, Joveler.Compression.XZ.LzmaCompLevel> XZLevelDict = new Dictionary<string, Joveler.Compression.XZ.LzmaCompLevel>(StringComparer.Ordinal)
        {
            ["Fastest"] = Joveler.Compression.XZ.LzmaCompLevel.Level0,
            ["Default"] = Joveler.Compression.XZ.LzmaCompLevel.Default,
            ["Best"] = Joveler.Compression.XZ.LzmaCompLevel.Level9,
        };

        // LZ4CompLevel
        public Dictionary<string, Joveler.Compression.LZ4.LZ4CompLevel> NativeLZ4LevelDict = new Dictionary<string, Joveler.Compression.LZ4.LZ4CompLevel>(StringComparer.Ordinal)
        {
            ["Fastest"] = Joveler.Compression.LZ4.LZ4CompLevel.Fast,
            ["Default"] = Joveler.Compression.LZ4.LZ4CompLevel.High,
            ["Best"] = Joveler.Compression.LZ4.LZ4CompLevel.VeryHigh, // LZ4-HC
        };

        // ManagedLZ4CompLevel
        public Dictionary<string, K4os.Compression.LZ4.LZ4Level> ManagedLZ4LevelDict = new Dictionary<string, K4os.Compression.LZ4.LZ4Level>(StringComparer.Ordinal)
        {
            ["Fastest"] = K4os.Compression.LZ4.LZ4Level.L00_FAST,
            ["Default"] = K4os.Compression.LZ4.LZ4Level.L09_HC,
            ["Best"] = K4os.Compression.LZ4.LZ4Level.L12_MAX, // LZ4-HC
        };

        // ZstdLevel
        public Dictionary<string, int> ZstdLevelDict = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Fastest"] = 1,
            ["Default"] = 3,
            ["Best"] = 22,
        };

        [GlobalSetup]
        public void GlobalSetup()
        {
            Program.NativeGlobalInit();

            ZstdLevelDict["Fatest"] = Joveler.Compression.Zstd.ZstdStream.MinCompressionLevel();
            ZstdLevelDict["Best"] = Joveler.Compression.Zstd.ZstdStream.MaxCompressionLevel();

            _sampleDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "Samples"));

            _destDir = Path.GetTempFileName();
            File.Delete(_destDir);
            Directory.CreateDirectory(_destDir);

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

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            if (Directory.Exists(_destDir))
                Directory.Delete(_destDir);
            Program.NativeGlobalCleanup();
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.LZ4)]
        public double LZ4_Native()
        {
            Joveler.Compression.LZ4.LZ4FrameCompressOptions compOpts = new Joveler.Compression.LZ4.LZ4FrameCompressOptions()
            {
                Level = NativeLZ4LevelDict[Level],
                AutoFlush = false,
                LeaveOpen = true,
            };

            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(rawData))
                using (Joveler.Compression.LZ4.LZ4FrameStream lzs = new Joveler.Compression.LZ4.LZ4FrameStream(ms, compOpts))
                {
                    rms.CopyTo(lzs);
                }

                ms.Flush();
                compLen = ms.Position;
            }

            CompRatio = (double)compLen / rawData.Length;
            return CompRatio;
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.LZ4)]
        public double LZ4_Managed()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(rawData))
                using (K4os.Compression.LZ4.Streams.LZ4EncoderStream lzs = K4os.Compression.LZ4.Streams.LZ4Stream.Encode(ms, ManagedLZ4LevelDict[Level], 0, true))
                {
                    rms.CopyTo(lzs);
                }

                ms.Flush();
                compLen = ms.Position;
            }

            CompRatio = (double)compLen / rawData.Length;
            return CompRatio;
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public double ZLib_Native()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                Joveler.Compression.ZLib.ZLibCompressOptions compOpts = new Joveler.Compression.ZLib.ZLibCompressOptions()
                {
                    Level = NativeZLibLevelDict[Level],
                    LeaveOpen = true,
                };

                using (MemoryStream rms = new MemoryStream(rawData))
                using (Joveler.Compression.ZLib.ZLibStream zs = new Joveler.Compression.ZLib.ZLibStream(ms, compOpts))
                {
                    rms.CopyTo(zs);
                }

                ms.Flush();
                compLen = ms.Position;
            }

            CompRatio = (double)compLen / rawData.Length;
            return CompRatio;
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public double ZLib_Managed()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                using MemoryStream rms = new MemoryStream(rawData);
                using SharpCompress.Compressors.Deflate.ZlibStream zs = new SharpCompress.Compressors.Deflate.ZlibStream(ms, SharpCompress.Compressors.CompressionMode.Compress, ManagedZLibLevelDict[Level]);
                rms.CopyTo(zs);

                ms.Flush();
                compLen = ms.Position;
            }

            CompRatio = (double)compLen / rawData.Length;
            return CompRatio;
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.XZ)]
        public double XZ_Native()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                Joveler.Compression.XZ.XZCompressOptions compOpts = new Joveler.Compression.XZ.XZCompressOptions
                {
                    Level = XZLevelDict[Level],
                    LeaveOpen = true,
                };

                using (MemoryStream rms = new MemoryStream(rawData))
                using (Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(ms, compOpts))
                {
                    rms.CopyTo(xzs);
                }

                ms.Flush();
                compLen = ms.Position;
            }

            CompRatio = (double)compLen / rawData.Length;
            return CompRatio;
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZSTD)]
        public double ZSTD_Native()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                Joveler.Compression.Zstd.ZstdCompressOptions compOpts = new Joveler.Compression.Zstd.ZstdCompressOptions
                {
                    CompressionLevel = ZstdLevelDict[Level],
                    LeaveOpen = true,
                };

                using (MemoryStream rms = new MemoryStream(rawData))
                using (Joveler.Compression.Zstd.ZstdStream zs = new Joveler.Compression.Zstd.ZstdStream(ms, compOpts))
                {
                    rms.CopyTo(zs);
                }

                ms.Flush();
                compLen = ms.Position;
            }

            CompRatio = (double)compLen / rawData.Length;
            return CompRatio;
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZSTD)]
        public double ZSTD_Managed()
        {
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(rawData))
                using (ZstdSharp.CompressionStream zs = new ZstdSharp.CompressionStream(ms, ZstdLevelDict[Level]))
                {
                    rms.CopyTo(zs);
                }
            }

            return 0;
        }
    }
    #endregion
}
