using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        public Dictionary<string, Joveler.ZLib.ZLibCompLevel> NativeZLibLevelDict = new Dictionary<string, Joveler.ZLib.ZLibCompLevel>(StringComparer.Ordinal)
        {
            ["Fastest"] = Joveler.ZLib.ZLibCompLevel.BestSpeed,
            ["Default"] = Joveler.ZLib.ZLibCompLevel.Default,
            ["Best"] = Joveler.ZLib.ZLibCompLevel.BestCompression,
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
            ["Fastest"] = Joveler.XZ.XZStream.MinimumPreset,
            ["Default"] = Joveler.XZ.XZStream.DefaultPreset,
            ["Best"] = Joveler.XZ.XZStream.MaximumPreset,
        };

        // LZ4CompLevel
        public Dictionary<string, Joveler.LZ4.LZ4CompLevel> NativeLZ4LevelDict = new Dictionary<string, Joveler.LZ4.LZ4CompLevel>(StringComparer.Ordinal)
        {
            ["Fastest"] = Joveler.LZ4.LZ4CompLevel.Fast,
            ["Default"] = Joveler.LZ4.LZ4CompLevel.High,
            ["Best"] = Joveler.LZ4.LZ4CompLevel.VeryHigh, // LZ4-HC
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
                using (Joveler.LZ4.LZ4FrameStream lzs = new Joveler.LZ4.LZ4FrameStream(ms, Joveler.LZ4.LZ4Mode.Compress, NativeLZ4LevelDict[Level], true))
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
                using (Joveler.ZLib.ZLibStream zs = new Joveler.ZLib.ZLibStream(ms, Joveler.ZLib.ZLibMode.Compress, NativeZLibLevelDict[Level], true))
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
                using (Joveler.XZ.XZStream xzs = new Joveler.XZ.XZStream(ms, Joveler.XZ.LzmaMode.Compress, XZPresetDict[Level], true))
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
                using (Joveler.LZ4.LZ4FrameStream zs = new Joveler.LZ4.LZ4FrameStream(rms, Joveler.LZ4.LZ4Mode.Decompress))
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
                using (Joveler.ZLib.ZLibStream zs = new Joveler.ZLib.ZLibStream(rms, Joveler.ZLib.ZLibMode.Decompress))
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
                using (Joveler.XZ.XZStream zs = new Joveler.XZ.XZStream(rms, Joveler.XZ.LzmaMode.Decompress))
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
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string zlibDllPath = null;
            string xzDllPath = null;
            string lz4DllPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    zlibDllPath = Path.Combine(baseDir, "x64", "zlibwapi.dll");
                    xzDllPath = Path.Combine(baseDir, "x64", "liblzma.dll");
                    lz4DllPath = Path.Combine(baseDir, "x64", "liblz4.so.1.8.3");
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {
                    zlibDllPath = Path.Combine(baseDir, "x86", "zlibwapi.dll");
                    xzDllPath = Path.Combine(baseDir, "x86", "liblzma.dll");
                    lz4DllPath = Path.Combine(baseDir, "x86", "liblz4.so.1.8.3");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    zlibDllPath = Path.Combine(baseDir, "x64", "libz.so.1.2.11");
                    xzDllPath = Path.Combine(baseDir, "x64", "liblzma.so.5.2.4");
                    lz4DllPath = Path.Combine(baseDir, "x64", "liblz4.so.1.8.3");
                }
            }
            
            if (zlibDllPath == null || xzDllPath == null || lz4DllPath == null)
                throw new PlatformNotSupportedException();


            Joveler.ZLib.ZLibInit.GlobalInit(zlibDllPath, 64 * 1024);
            Joveler.XZ.XZStream.GlobalInit(xzDllPath, 64 * 1024);
            Joveler.LZ4.LZ4FrameStream.GlobalInit(lz4DllPath, 64 * 1024);
        }

        public static void NativeGlobalCleanup()
        {
            Joveler.ZLib.ZLibInit.GlobalCleanup();
            Joveler.XZ.XZStream.GlobalCleanup();
            Joveler.LZ4.LZ4FrameStream.GlobalCleanup();
        }

        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<CompBench>();
            BenchmarkRunner.Run<DecompBench>();
        }
    }
    #endregion
}
