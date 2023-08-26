using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Benchmark
{
    #region BufferRatioColumn
    public class BufferRatioColumn : ReturnValueColumn
    {
        public override string Id { get; protected set; } = $"{nameof(BufferRatioColumn)}.CompRatio";
        public override string ColumnName { get; protected set; } = "CompRatio";
        public override string Legend => $"Compression ratio of the configured algorithm.";

        public BufferRatioColumn()
        {
        }


        public override bool LoadParams(object instance, BenchmarkCase benchmarkCase)
        {
            const string bufferSizeKey = nameof(BufferSizeBench.BufferSize);
            const string srcFileNameKey = nameof(BufferSizeBench.SrcFileName);
            const string modeKey = nameof(BufferSizeBench.Mode);

            Descriptor descriptor = benchmarkCase.Descriptor;

            // Get parameters from benchmarkCase
            object bufferSizeVal = benchmarkCase.Parameters.Items.First(x => x.Name.Equals(bufferSizeKey, StringComparison.Ordinal)).Value;
            object srcFileNameVal = benchmarkCase.Parameters.Items.First(x => x.Name.Equals(srcFileNameKey, StringComparison.Ordinal)).Value;
            object modeVal = benchmarkCase.Parameters.Items.First(x => x.Name.Equals(modeKey, StringComparison.Ordinal)).Value;
            if (bufferSizeVal is not string bufferSizeStr)
                return false;
            if (srcFileNameVal is not string srcFileNameStr)
                return false;
            if (modeVal is not string modeStr)
                return false;

            // Set parameters to benchmark instances
            PropertyInfo bufferSizeProp = descriptor.Type.GetProperty(bufferSizeKey);
            bufferSizeProp.SetValue(instance, bufferSizeStr);
            PropertyInfo srcFileNameProp = descriptor.Type.GetProperty(srcFileNameKey);
            srcFileNameProp.SetValue(instance, srcFileNameStr);
            PropertyInfo modeProp = descriptor.Type.GetProperty(modeKey);
            modeProp.SetValue(instance, modeStr);
            return true;
        }

        public override string ParseReturnObject(object ret) => ParseDouble(ret);
    }

    public class BufferRatioConfig : BenchConfig
    {
        public BufferRatioConfig() : base()
        {
            // Columns
            AddColumn(new CompRatioColumn());
        }
    }
    #endregion

    #region BufferSizeBench
    [Config(typeof(BufferRatioConfig))]
    public class BufferSizeBench
    {
        #region Fields and Properties
        private string _sampleDir;
        private const string ModeCompress = "Compress";
        private const string ModeDecompress = "Decompress";
        private const string Level = "Default";

        // BufferSizes
        [ParamsSource(nameof(BufferSizes))]
        public int BufferSize { get; set; }
        public IReadOnlyList<int> BufferSizes { get; set; } = new int[]
        {
            64 * 1024,
            128 * 1024,
            256 * 1024,
            512 * 1024,
            1024 * 1024,
            2 * 1024 * 1024,
            4 * 1024 * 1024,
        };

        // SrcFiles
        [ParamsSource(nameof(SrcFileNames))]
        public string SrcFileName { get; set; }
        public IReadOnlyList<string> SrcFileNames { get; set; } = new List<string>(BenchSamples.LessFileNames);

        /// <summary>
        /// Cache raw source files to memory to minimize I/O bottleneck.
        /// </summary>
        public Dictionary<string, byte[]> SrcFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        // Mode
        [ParamsSource(nameof(Modes))]
        public string Mode { get; set; }
        public IReadOnlyList<string> Modes { get; set; } = new List<string>()
        {
            ModeCompress,
            ModeDecompress,
        };
        #endregion

        #region Startup and Cleanup
        public void GlobalSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.All);

            _sampleDir = Program.SampleDir;

            foreach (string srcFileName in SrcFileNames)
            {
                // Load raw files to compress
                {
                    string srcRawFile = Path.Combine(_sampleDir, "Raw", srcFileName);
                    using MemoryStream ms = new MemoryStream();
                    using (FileStream fs = new FileStream(srcRawFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fs.CopyTo(ms);
                    }

                    SrcFiles[srcFileName] = ms.ToArray();
                }

                // Load compressed files to decompress
                foreach (string ext in new string[] { ".zz", ".xz", ".lz4", ".zst" })
                {
                    string srcCompFile = Path.Combine(_sampleDir, Level, srcFileName + ext);
                    using MemoryStream ms = new MemoryStream();
                    using (FileStream fs = new FileStream(srcCompFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fs.CopyTo(ms);
                    }

                    SrcFiles[CompFileKey(Level, srcFileName, ext)] = ms.ToArray();
                }
            }
        }

        [GlobalSetup(Targets = new string[] { nameof(ZLibNgNativeJoveler) })]
        public void ZLibNgSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.ZLibNg);

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
            Program.NativeGlobalCleanup();
        }
        #endregion

        #region Util
        public static string CompFileKey(string level, string srcFileName, string ext)
        {
            ext = ext.Trim('.');
            return $"{level}_{srcFileName}.{ext}";
        }
        #endregion

        #region zlib
        [Benchmark(Description = "zlib-ng")]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public double ZLibNgNativeJoveler()
        {
            if (Mode.Equals(ModeCompress, StringComparison.Ordinal))
            {
                Joveler.Compression.ZLib.ZLibCompressOptions compOpts = new Joveler.Compression.ZLib.ZLibCompressOptions()
                {
                    Level = Joveler.Compression.ZLib.ZLibCompLevel.Default,
                    BufferSize = BufferSize,
                    LeaveOpen = true,
                };

                long compLen;
                byte[] rawData = SrcFiles[SrcFileName];
                using (MemoryStream ms = new MemoryStream())
                {
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
            else
            {
                Joveler.Compression.ZLib.ZLibDecompressOptions decompOpts = new Joveler.Compression.ZLib.ZLibDecompressOptions()
                {
                    BufferSize = BufferSize,
                    LeaveOpen = true,
                };

                long rawLen;
                byte[] compData = SrcFiles[CompFileKey(Level, SrcFileName, "zz")];
                using (MemoryStream ms = new MemoryStream())
                {
                    using (MemoryStream rms = new MemoryStream(compData))
                    using (Joveler.Compression.ZLib.ZLibStream zs = new Joveler.Compression.ZLib.ZLibStream(rms, decompOpts))
                    {
                        zs.CopyTo(ms);
                    }

                    ms.Flush();
                    rawLen = ms.Position;
                }
                return (double)compData.Length / rawLen;
            }
        }
        #endregion

        #region xz-utils
        [Benchmark(Description = "xz")]
        [BenchmarkCategory(BenchConfig.XZ)]
        public double XZSingleNativeJoveler()
        {
            if (Mode.Equals(ModeCompress, StringComparison.Ordinal))
            {
                Joveler.Compression.XZ.XZCompressOptions compOpts = new Joveler.Compression.XZ.XZCompressOptions
                {
                    Level = Joveler.Compression.XZ.LzmaCompLevel.Default,
                    BufferSize = BufferSize,
                    LeaveOpen = true,
                };

                long compLen;
                byte[] rawData = SrcFiles[SrcFileName];
                using (MemoryStream ms = new MemoryStream())
                {
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
            else
            {
                Joveler.Compression.XZ.XZDecompressOptions decompOpts = new Joveler.Compression.XZ.XZDecompressOptions()
                {
                    BufferSize = BufferSize,
                    LeaveOpen = true,
                };

                long rawLen;
                byte[] compData = SrcFiles[CompFileKey(Level, SrcFileName, "xz")];
                using (MemoryStream ms = new MemoryStream())
                {
                    using (MemoryStream rms = new MemoryStream(compData))
                    using (Joveler.Compression.XZ.XZStream zs = new Joveler.Compression.XZ.XZStream(rms, decompOpts))
                    {
                        zs.CopyTo(ms);
                    }

                    ms.Flush();
                    rawLen = ms.Position;
                }
                return (double)compData.Length / rawLen;
            }
        }

        [Benchmark(Description = "xz-T1")]
        [BenchmarkCategory(BenchConfig.XZ)]
        public double XZMultiNativeJoveler()
        {
            if (Mode.Equals(ModeCompress, StringComparison.Ordinal))
            {
                Joveler.Compression.XZ.XZCompressOptions compOpts = new Joveler.Compression.XZ.XZCompressOptions
                {
                    Level = Joveler.Compression.XZ.LzmaCompLevel.Default,
                    BufferSize = BufferSize,
                    LeaveOpen = true,
                };

                // LZMA2 threaded compression with -9 option takes a lot of memory.
                // To prevent memory starvation and make test results consistent, test only 1 threads.
                Joveler.Compression.XZ.XZThreadedCompressOptions threadOpts = new Joveler.Compression.XZ.XZThreadedCompressOptions
                {
                    Threads = 1,
                };

                long compLen;
                byte[] rawData = SrcFiles[SrcFileName];
                using (MemoryStream ms = new MemoryStream())
                {
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
            else
            {
                Joveler.Compression.XZ.XZDecompressOptions decompOpts = new Joveler.Compression.XZ.XZDecompressOptions()
                {
                    BufferSize = BufferSize,
                    LeaveOpen = true,
                };

                // LZMA2 threaded compression with -9 option takes a lot of memory.
                // To prevent memory starvation and make test results consistent, test only 1 threads.
                Joveler.Compression.XZ.XZThreadedDecompressOptions threadOpts = new Joveler.Compression.XZ.XZThreadedDecompressOptions
                {
                    Threads = 1,
                };

                long rawLen;
                byte[] compData = SrcFiles[CompFileKey(Level, SrcFileName, "xz")];
                using (MemoryStream ms = new MemoryStream())
                {
                    using (MemoryStream rms = new MemoryStream(compData))
                    using (Joveler.Compression.XZ.XZStream zs = new Joveler.Compression.XZ.XZStream(rms, decompOpts, threadOpts))
                    {
                        zs.CopyTo(ms);
                    }

                    ms.Flush();
                    rawLen = ms.Position;
                }
                return (double)compData.Length / rawLen;
            }
        }
        #endregion

        #region lz4
        [Benchmark(Description = "lz4")]
        [BenchmarkCategory(BenchConfig.LZ4)]
        public double LZ4NativeJoveler()
        {
            if (Mode.Equals(ModeCompress, StringComparison.Ordinal))
            {
                Joveler.Compression.LZ4.LZ4FrameCompressOptions compOpts = new Joveler.Compression.LZ4.LZ4FrameCompressOptions()
                {
                    Level = Joveler.Compression.LZ4.LZ4CompLevel.Default,
                    BufferSize = BufferSize,
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
            else
            {
                Joveler.Compression.LZ4.LZ4FrameDecompressOptions decompOpts = new Joveler.Compression.LZ4.LZ4FrameDecompressOptions()
                {
                    BufferSize = BufferSize,
                    LeaveOpen = true,
                };

                long rawLen;
                byte[] compData = SrcFiles[CompFileKey(Level, SrcFileName, "lz4")];
                using (MemoryStream ms = new MemoryStream())
                {
                    using (MemoryStream rms = new MemoryStream(compData))
                    using (Joveler.Compression.LZ4.LZ4FrameStream zs = new Joveler.Compression.LZ4.LZ4FrameStream(rms, decompOpts))
                    {
                        zs.CopyTo(ms);
                    }

                    ms.Flush();
                    rawLen = ms.Position;
                }
                return (double)compData.Length / rawLen;
            }
        }
        #endregion

        #region Zstd
        [Benchmark(Description = "zstd")]
        [BenchmarkCategory(BenchConfig.Zstd)]
        public double ZstdSingleNativeJoveler()
        {
            if (Mode.Equals(ModeCompress, StringComparison.Ordinal))
            {
                Joveler.Compression.Zstd.ZstdCompressOptions compOpts = new Joveler.Compression.Zstd.ZstdCompressOptions()
                {
                    CompressionLevel = 3,
                    MTWorkers = 0,
                    BufferSize = BufferSize,
                    LeaveOpen = true,
                };

                long compLen;
                byte[] rawData = SrcFiles[SrcFileName];
                using (MemoryStream ms = new MemoryStream())
                {
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
            else
            {
                Joveler.Compression.Zstd.ZstdDecompressOptions decompOpts = new Joveler.Compression.Zstd.ZstdDecompressOptions()
                {
                    BufferSize = BufferSize,
                    LeaveOpen = true,
                };

                long rawLen;
                byte[] compData = SrcFiles[CompFileKey(Level, SrcFileName, "zstd")];
                using (MemoryStream ms = new MemoryStream())
                {
                    using (MemoryStream rms = new MemoryStream(compData))
                    using (Joveler.Compression.Zstd.ZstdStream zs = new Joveler.Compression.Zstd.ZstdStream(rms, decompOpts))
                    {
                        zs.CopyTo(ms);
                    }

                    ms.Flush();
                    rawLen = ms.Position;
                }
                return (double)compData.Length / rawLen;
            }
        }

        [Benchmark(Description = "zstd-T1")]
        [BenchmarkCategory(BenchConfig.Zstd)]
        public double ZstdMultiNativeJoveler()
        {
            if (Mode.Equals(ModeCompress, StringComparison.Ordinal))
            {
                Joveler.Compression.Zstd.ZstdCompressOptions compOpts = new Joveler.Compression.Zstd.ZstdCompressOptions()
                {
                    CompressionLevel = 3,
                    MTWorkers = 1,
                    BufferSize = BufferSize,
                    LeaveOpen = true,
                };

                long compLen;
                byte[] rawData = SrcFiles[SrcFileName];
                using (MemoryStream ms = new MemoryStream())
                {
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
            else
            {
                Joveler.Compression.Zstd.ZstdDecompressOptions decompOpts = new Joveler.Compression.Zstd.ZstdDecompressOptions()
                {
                    BufferSize = BufferSize,
                    LeaveOpen = true,
                };

                long rawLen;
                byte[] compData = SrcFiles[CompFileKey(Level, SrcFileName, "zstd")];
                using (MemoryStream ms = new MemoryStream())
                {
                    using (MemoryStream rms = new MemoryStream(compData))
                    using (Joveler.Compression.Zstd.ZstdStream zs = new Joveler.Compression.Zstd.ZstdStream(rms, decompOpts))
                    {
                        zs.CopyTo(ms);
                    }

                    ms.Flush();
                    rawLen = ms.Position;
                }
                return (double)compData.Length / rawLen;
            }
        }
        #endregion
    }
    #endregion
}
