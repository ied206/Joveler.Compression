using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

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
        public string[] SrcFileNames { get; set; } = new string[3]
        {
            "Banner.bmp",
            "Banner.svg",
            "Type4.txt",
        };
        public Dictionary<string, byte[]> SrcFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        // Levels
        [ParamsSource(nameof(Levels))]
        public string Level { get; set; }
        public string[] Levels { get; set; } = new string[3]
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
                using (MemoryStream ms = new MemoryStream())
                {
                    using (FileStream fs = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fs.CopyTo(ms);
                    }

                    SrcFiles[srcFileName] = ms.ToArray();
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
                using (MemoryStream rms = new MemoryStream(rawData))
                using (SharpCompress.Compressors.Deflate.ZlibStream zs = new SharpCompress.Compressors.Deflate.ZlibStream(ms, SharpCompress.Compressors.CompressionMode.Compress, ManagedZLibLevelDict[Level]))
                {
                    rms.CopyTo(zs);
                    ms.Flush();
                    compLen = ms.Position;
                }
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
                using (MemoryStream rms = new MemoryStream(rawData))
                using (Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(ms, Joveler.Compression.XZ.LzmaMode.Compress, XZPresetDict[Level], true))
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

    #region DecompBench
    public class DecompBench
    {
        private string _sampleDir;
        private string _destDir;

        // SrcFiles
        [ParamsSource(nameof(SrcFileNames))]
        public string SrcFileName { get; set; }
        public string[] SrcFileNames { get; set; } = new string[3]
        {
            "Banner.bmp",
            "Banner.svg",
            "Type4.txt",
        };
        public Dictionary<string, byte[]> SrcFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        // Levels
        [ParamsSource(nameof(Levels))]
        public string Level { get; set; }
        public string[] Levels { get; set; } = new string[3]
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
                    foreach (string ext in new string[] { ".zz", ".xz", ".lz4" })
                    {
                        string srcFile = Path.Combine(_sampleDir, level, srcFileName + ext);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (FileStream fs = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                fs.CopyTo(ms);
                            }

                            SrcFiles[$"{level}_{srcFileName}{ext}"] = ms.ToArray();
                        }
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
        public long Native_LZ4()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.lz4"];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(compData))
                using (Joveler.Compression.LZ4.LZ4FrameStream zs = new Joveler.Compression.LZ4.LZ4FrameStream(rms, Joveler.Compression.LZ4.LZ4Mode.Decompress))
                {
                    zs.CopyTo(ms);
                }

                ms.Flush();
                return ms.Length;
            }
        }

        [Benchmark]
        public long Managed_LZ4()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.lz4"];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(compData))
                using (K4os.Compression.LZ4.Streams.LZ4DecoderStream zs = K4os.Compression.LZ4.Streams.LZ4Stream.Decode(rms, 0))
                {
                    zs.CopyTo(ms);
                }

                ms.Flush();
                return ms.Length;
            }
        }

        [Benchmark]
        public long Native_ZLib()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.zz"];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(compData))
                using (Joveler.Compression.ZLib.ZLibStream zs = new Joveler.Compression.ZLib.ZLibStream(rms, Joveler.Compression.ZLib.ZLibMode.Decompress))
                {
                    zs.CopyTo(ms);
                }

                ms.Flush();
                return ms.Length;
            }
        }

        [Benchmark]
        public long Managed_ZLib()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.zz"];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(compData))
                using (SharpCompress.Compressors.Deflate.ZlibStream zs = new SharpCompress.Compressors.Deflate.ZlibStream(rms, SharpCompress.Compressors.CompressionMode.Decompress))
                {
                    zs.CopyTo(ms);
                }

                ms.Flush();
                return ms.Length;
            }
        }

        [Benchmark]
        public long Native_XZ()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.xz"];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(compData))
                using (Joveler.Compression.XZ.XZStream zs = new Joveler.Compression.XZ.XZStream(rms, Joveler.Compression.XZ.LzmaMode.Decompress))
                {
                    zs.CopyTo(ms);
                }

                ms.Flush();
                return ms.Length;
            }
        }

        [Benchmark]
        public long Managed_XZ()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.xz"];
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(compData))
                using (SharpCompress.Compressors.Xz.XZStream zs = new SharpCompress.Compressors.Xz.XZStream(rms))
                {
                    zs.CopyTo(ms);
                }

                ms.Flush();
                return ms.Length;
            }
        }
    }
    #endregion

    #region Program
    public class Program
    {
        public static void NativeGlobalInit()
        {
            const string x64 = "x64";
            const string x86 = "x86";
            const string armhf = "armhf";

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string zlibPath = null;
            string xzPath = null;
            string lz4Path = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        zlibPath = Path.Combine(baseDir, x64, "zlibwapi.dll");
                        xzPath = Path.Combine(baseDir, x64, "liblzma.dll");
                        lz4Path = Path.Combine(baseDir, x64, "liblz4.dll");
                        break;
                    case Architecture.X86:
                        zlibPath = Path.Combine(baseDir, x86, "zlibwapi.dll");
                        xzPath = Path.Combine(baseDir, x86, "liblzma.dll");
                        lz4Path = Path.Combine(baseDir, x86, "liblz4.dll");
                        break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        zlibPath = Path.Combine(baseDir, x64, "libz.so");
                        xzPath = Path.Combine(baseDir, x64, "liblzma.so");
                        lz4Path = Path.Combine(baseDir, x64, "liblz4.so");
                        break;
                    case Architecture.Arm:
                        zlibPath = Path.Combine(baseDir, armhf, "libz.so");
                        xzPath = Path.Combine(baseDir, armhf, "liblzma.so");
                        lz4Path = Path.Combine(baseDir, armhf, "liblz4.so");
                        break;
                }
            }

            if (zlibPath == null || xzPath == null || lz4Path == null)
                throw new PlatformNotSupportedException();

            Joveler.Compression.ZLib.ZLibInit.GlobalInit(zlibPath, 64 * 1024);
            Joveler.Compression.XZ.XZInit.GlobalInit(xzPath, 64 * 1024);
            Joveler.Compression.LZ4.LZ4Init.GlobalInit(lz4Path, 64 * 1024);
        }

        public static void NativeGlobalCleanup()
        {
            Joveler.Compression.ZLib.ZLibInit.GlobalCleanup();
            Joveler.Compression.XZ.XZInit.GlobalCleanup();
            Joveler.Compression.LZ4.LZ4Init.GlobalCleanup();
        }

        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<CompBench>();
            BenchmarkRunner.Run<DecompBench>();
        }
    }
    #endregion
}
