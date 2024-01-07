// #define SHORT_TEST

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Benchmark
{
    #region CompRatioColumn
    public class CompRatioColumn : ReturnValueColumn
    {
        public override string Id { get; protected set; } = $"{nameof(CompRatioColumn)}.CompRatio";
        public override string ColumnName { get; protected set; } = "CompRatio";
        public override string Legend => $"Compression ratio of the configured algorithm.";

        public CompRatioColumn()
        {
        }


        public override bool LoadParams(object instance, BenchmarkCase benchmarkCase)
        {
            const string srcFileNameKey = nameof(CompBench.SrcFileName);
            const string levelKey = nameof(CompBench.Level);

            Descriptor descriptor = benchmarkCase.Descriptor;

            // Get parameters from benchmarkCase
            object srcFileNameVal = benchmarkCase.Parameters.Items.First(x => x.Name.Equals(srcFileNameKey, StringComparison.Ordinal)).Value;
            object levelVal = benchmarkCase.Parameters.Items.First(x => x.Name.Equals(levelKey, StringComparison.Ordinal)).Value;
            if (srcFileNameVal is not string srcFileNameStr)
                return false;
            if (levelVal is not string levelStr)
                return false;

            // Set parameters to benchmark instances
            PropertyInfo srcFileNameProp = descriptor.Type.GetProperty(srcFileNameKey);
            srcFileNameProp.SetValue(instance, srcFileNameStr);
            PropertyInfo levelProp = descriptor.Type.GetProperty(levelKey);
            levelProp.SetValue(instance, levelStr);
            return true;
        }

        public override string ParseReturnObject(object ret) => ParseDouble(ret);
    }

    public class CompRatioConfig : BenchConfig
    {
        public CompRatioConfig() : base()
        {
            // Columns
            AddColumn(new CompRatioColumn());
        }
    }
    #endregion

    #region CompBench
    [Config(typeof(CompRatioConfig))]
#if SHORT_TEST
    [ShortRunJob]
#endif
    public class CompBench
    {
        #region Fields and Properties
        private string _sampleDir;
        private string _destDir;
        #endregion

        #region Parameterization
        // SrcFiles
        [ParamsSource(nameof(SrcFileNames))]
        public string SrcFileName { get; set; }
#if SHORT_TEST
        public IReadOnlyList<string> SrcFileNames { get; set; } = new List<string>()
        {
            "Banner.svg"
        };
#else
        public IReadOnlyList<string> SrcFileNames { get; set; } = new List<string>(BenchSamples.SampleFileNames);
#endif

        /// <summary>
        /// Cache raw source files to memory to minimize I/O bottleneck.
        /// </summary>
        public Dictionary<string, byte[]> SrcFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        // Levels
        [ParamsSource(nameof(Levels))]
        public string Level { get; set; }
#if SHORT_TEST
        public IReadOnlyList<string> Levels { get; set; } = new string[]
        {
            "Default",
        };
#else
        public IReadOnlyList<string> Levels { get; set; } = new List<string>(BenchSamples.Levels);
#endif


        // ZLibCompLevel
        public Dictionary<string, Joveler.Compression.ZLib.ZLibCompLevel> NativeZLibLevelDict = new Dictionary<string, Joveler.Compression.ZLib.ZLibCompLevel>(StringComparer.Ordinal)
        {
            ["Fastest"] = Joveler.Compression.ZLib.ZLibCompLevel.BestSpeed,
            ["Default"] = Joveler.Compression.ZLib.ZLibCompLevel.Default,
            ["Best"] = Joveler.Compression.ZLib.ZLibCompLevel.BestCompression,
        };

        public Dictionary<string, System.IO.Compression.CompressionLevel> BclZLibLevelDict = new Dictionary<string, System.IO.Compression.CompressionLevel>(StringComparer.Ordinal)
        {
            ["Fastest"] = System.IO.Compression.CompressionLevel.Fastest,
            ["Default"] = System.IO.Compression.CompressionLevel.Optimal,
            ["Best"] = System.IO.Compression.CompressionLevel.SmallestSize,
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
        #endregion

        #region Setup and Cleanup
        private void GlobalSetup()
        {
            _sampleDir = Program.SampleDir;

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

        [GlobalSetup(Targets = new string[] { nameof(ZstdSingleNativeJoveler), nameof(ZstdMultiNativeJoveler) })]
        public void ZstdSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.Zstd);

            ZstdLevelDict["Fatest"] = Joveler.Compression.Zstd.ZstdStream.MinCompressionLevel();
            ZstdLevelDict["Best"] = Joveler.Compression.Zstd.ZstdStream.MaxCompressionLevel();

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
        private double ZLibNativeJoveler()
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
            return (double)compLen / rawData.Length;
        }

        [Benchmark(Description = "zlib-ng (n_Joveler)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public double ZLibNgNativeJoveler()
        {
            return ZLibNativeJoveler();
        }

        [Benchmark(Description = "zlib (n_Joveler)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public double ZLibUpNativeJoveler()
        {
            return ZLibNativeJoveler();
        }

        [Benchmark(Description = "zlib (n_BCL)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public double ZLibNativeBcl()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                System.IO.Compression.CompressionLevel level = BclZLibLevelDict[Level];

                using (MemoryStream rms = new MemoryStream(rawData))
                using (System.IO.Compression.ZLibStream zs = new System.IO.Compression.ZLibStream(ms, level, true))
                {
                    rms.CopyTo(zs);
                }

                ms.Flush();
                compLen = ms.Position;
            }
            return (double)compLen / rawData.Length;
        }

        [Benchmark(Description = "zlib (m_SharpCompress)")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public double ZLibManagedSharpCompress()
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
            return (double)compLen / rawData.Length;
        }
        #endregion

        #region Benchmark - xz-utils
        [Benchmark(Description = "xz (n_Joveler)")]
        [BenchmarkCategory(BenchConfig.XZ)]
        public double XZSingleNativeJoveler()
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
            return (double)compLen / rawData.Length;
        }

        [Benchmark(Description = "xz-T1 (n_Joveler)")]
        [BenchmarkCategory(BenchConfig.XZ)]
        public double XZMultiNativeJoveler()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                Joveler.Compression.XZ.XZCompressOptions compOpts = new Joveler.Compression.XZ.XZCompressOptions
                {
                    // Do not run "Best" profile. It will take up a lot of memory.
                    Level = XZLevelDict["Default"],
                    LeaveOpen = true,
                };

                // LZMA2 threaded compression with -9 option takes a lot of memory.
                // To prevent memory starvation and make test results consistent, test only 1 threads.
                Joveler.Compression.XZ.XZThreadedCompressOptions threadOpts = new Joveler.Compression.XZ.XZThreadedCompressOptions
                {
                    Threads = 1,
                };

                using (MemoryStream rms = new MemoryStream(rawData))
                using (Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(ms, compOpts, threadOpts))
                {
                    rms.CopyTo(xzs);
                }

                ms.Flush();
                compLen = ms.Position;
            }
            return (double)compLen / rawData.Length;
        }
        #endregion

        #region Benchmark - lz4
        [Benchmark(Description = "lz4 (n_Joveler)")]
        [BenchmarkCategory(BenchConfig.LZ4)]
        public double LZ4NativeJoveler()
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
            return (double)compLen / rawData.Length;
        }

        [Benchmark(Description = "lz4 (m_K4os)")]
        [BenchmarkCategory(BenchConfig.LZ4)]
        public double LZ4ManagedK4os()
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
            return (double)compLen / rawData.Length;
        }
        #endregion

        #region Benchmark - zstd
        [Benchmark(Description = "zstd (m_Joveler)")]
        [BenchmarkCategory(BenchConfig.Zstd)]
        public double ZstdSingleNativeJoveler()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                Joveler.Compression.Zstd.ZstdCompressOptions compOpts = new Joveler.Compression.Zstd.ZstdCompressOptions
                {
                    CompressionLevel = ZstdLevelDict[Level],
                    MTWorkers = 0,
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
            return (double)compLen / rawData.Length;
        }

        [Benchmark(Description = "zstd-T1 (m_Joveler)")]
        [BenchmarkCategory(BenchConfig.Zstd)]
        public double ZstdMultiNativeJoveler()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                Joveler.Compression.Zstd.ZstdCompressOptions compOpts = new Joveler.Compression.Zstd.ZstdCompressOptions
                {
                    CompressionLevel = ZstdLevelDict[Level],
                    MTWorkers = 1,
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
            return (double)compLen / rawData.Length;
        }

        [Benchmark(Description = "zstd (m_ZstdSharp)")]
        [BenchmarkCategory(BenchConfig.Zstd)]
        public double ZstdManagedZstdSharp()
        {
            long compLen;
            byte[] rawData = SrcFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(rawData))
                using (ZstdSharp.CompressionStream zs = new ZstdSharp.CompressionStream(ms, ZstdLevelDict[Level], 0, true))
                {
                    rms.CopyTo(zs);
                }

                ms.Flush();
                compLen = ms.Position;
            }
            return (double)compLen / rawData.Length;
        }
        #endregion
    }
    #endregion
}
