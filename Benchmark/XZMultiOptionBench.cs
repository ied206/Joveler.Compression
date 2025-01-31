using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    #region XZMultiCompBench
    [Config(typeof(BenchConfig))]
    public class XZMultiOptionBench
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
        public Dictionary<string, byte[]> SrcCompFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        public Dictionary<string, byte[]> SrcDecompFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        // TimeOut (in milliseconds)
        [ParamsSource(nameof(TimeOutValues))]
        public uint TimeOutValue { get; set; }
        public IReadOnlyList<uint> TimeOutValues { get; set; } = new uint[]
        {
            0,
            100,
            300,
            1000,
        };

        [GlobalSetup]
        public void GlobalSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.XZ);

            _sampleDir = Program.SampleDir;

            _destDir = Path.GetTempFileName();
            File.Delete(_destDir);
            Directory.CreateDirectory(_destDir);

            foreach (string srcFileName in SrcFileNames)
            {
                // Compress
                {
                    string srcFile = Path.Combine(_sampleDir, "Raw", srcFileName);
                    using MemoryStream ms = new MemoryStream();
                    using (FileStream fs = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fs.CopyTo(ms);
                    }

                    SrcCompFiles[srcFileName] = ms.ToArray();
                }

                // Decompress
                {
                    string srcFile = Path.Combine(_sampleDir, "Default", $"{srcFileName}.xz");
                    using MemoryStream ms = new MemoryStream();
                    using (FileStream fs = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fs.CopyTo(ms);
                    }

                    SrcDecompFiles[$"{srcFileName}.xz"] = ms.ToArray();
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
        [BenchmarkCategory(BenchConfig.XZ)]
        public double XZ_Native_Comp()
        {
            long compLen;
            byte[] rawData = SrcCompFiles[SrcFileName];
            using (MemoryStream ms = new MemoryStream())
            {
                Joveler.Compression.XZ.XZCompressOptions compOpts = new Joveler.Compression.XZ.XZCompressOptions
                {
                    Level = Joveler.Compression.XZ.LzmaCompLevel.Default,
                    LeaveOpen = true,
                };

                Joveler.Compression.XZ.XZParallelCompressOptions threadOpts = new Joveler.Compression.XZ.XZParallelCompressOptions
                {
                    Threads = 2,
                    TimeOut = TimeOutValue,
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
        [BenchmarkCategory(BenchConfig.XZ)]
        public long XZ_Native_Decomp()
        {
            byte[] compData = SrcDecompFiles[$"{SrcFileName}.xz"];
            using MemoryStream ms = new MemoryStream();

            Joveler.Compression.XZ.XZDecompressOptions decompOpts = new Joveler.Compression.XZ.XZDecompressOptions();

            Joveler.Compression.XZ.XZParallelDecompressOptions threadOpts = new Joveler.Compression.XZ.XZParallelDecompressOptions
            {
                Threads = 2,
                TimeOut = TimeOutValue,
            };

            using (MemoryStream rms = new MemoryStream(compData))
            using (Joveler.Compression.XZ.XZStream zs = new Joveler.Compression.XZ.XZStream(rms, decompOpts))
            {
                zs.CopyTo(ms);
            }

            ms.Flush();
            return ms.Length;
        }
    }
    #endregion
}
