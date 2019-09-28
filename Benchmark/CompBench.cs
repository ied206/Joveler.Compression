using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    #region CompBench
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

        // XZPreset
        public Dictionary<string, uint> XZPresetDict = new Dictionary<string, uint>(StringComparer.Ordinal)
        {
            ["Fastest"] = Joveler.Compression.XZ.XZStream.MinimumPreset,
            ["Default"] = Joveler.Compression.XZ.XZStream.DefaultPreset,
            ["Best"] = Joveler.Compression.XZ.XZStream.MaximumPreset,
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

        [GlobalSetup]
        public void GlobalSetup()
        {
            Program.NativeGlobalInit();

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
        public double Native_LZ4()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(rawData))
                using (Joveler.Compression.LZ4.LZ4FrameStream lzs = new Joveler.Compression.LZ4.LZ4FrameStream(ms, Joveler.Compression.LZ4.LZ4Mode.Compress, NativeLZ4LevelDict[Level], true))
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
        public double Managed_LZ4()
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
        public double Native_ZLib()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(rawData))
                using (Joveler.Compression.ZLib.ZLibStream zs = new Joveler.Compression.ZLib.ZLibStream(ms, Joveler.Compression.ZLib.ZLibMode.Compress, NativeZLibLevelDict[Level], true))
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
        public double Managed_ZLib()
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
        public double Native_XZ()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                Joveler.Compression.XZ.XZCompressOptions compOpts = new Joveler.Compression.XZ.XZCompressOptions
                {
                    Preset = XZPresetDict[Level]
                };
                Joveler.Compression.XZ.XZStreamOptions advOpts = new Joveler.Compression.XZ.XZStreamOptions
                {
                    LeaveOpen = true,
                };
                using (MemoryStream rms = new MemoryStream(rawData))
                using (Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(ms, compOpts, advOpts))
                {
                    rms.CopyTo(xzs);
                }

                ms.Flush();
                compLen = ms.Position;
            }

            CompRatio = (double)compLen / rawData.Length;
            return CompRatio;
        }
    }
    #endregion
}
