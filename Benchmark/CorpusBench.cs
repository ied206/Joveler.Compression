using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    #region CorpusBenchBase
    // [Config(typeof(CorpusZLibConfig))]
    [Config(typeof(BenchConfig))]
    public class CorpusBench
    {
        private string _sampleDir;
        private string _destDir;

        // SrcFileNames
        [ParamsSource(nameof(SrcFileNames))]
        public string SrcFileName { get; set; }
        public List<string> SrcFileNames { get; set; } = new List<string>();
        /// <summary>
        /// Cache raw source files to memory to minimize I/O bottleneck.
        /// </summary>
        public Dictionary<string, byte[]> SrcBytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);

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

        // CompressedBytes
        public Dictionary<string, byte[]> CompressedBytesDict = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        public void GlobalSetup()
        {
            _sampleDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "Samples"));
            string corpusDir = Path.Combine(_sampleDir, "Corpus");

            _destDir = Path.GetTempFileName();
            File.Delete(_destDir);
            Directory.CreateDirectory(_destDir);

            // Load raw files
            foreach (string srcFileName in SrcFileNames)
            {
                string srcFile = Path.Combine(corpusDir, srcFileName);
                using MemoryStream ms = new MemoryStream();
                using (FileStream fs = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.CopyTo(ms);
                }

                SrcBytes[srcFileName] = ms.ToArray();
            }

            /*
            // Create compressed bytes
            foreach (string srcFileName in SrcFileNames)
            {
                byte[] rawBytes = SrcBytes[srcFileName];

                Joveler.Compression.ZLib.ZLibCompressOptions compOpts = new Joveler.Compression.ZLib.ZLibCompressOptions()
                {
                    Level = NativeZLibLevelDict[Level],
                    LeaveOpen = true,
                };

                using MemoryStream cms = new MemoryStream(rawBytes.Length);
                using (MemoryStream rms = new MemoryStream(rawBytes))
                using (Joveler.Compression.ZLib.ZLibStream zs = new Joveler.Compression.ZLib.ZLibStream(cms, compOpts))
                {
                    rms.CopyTo(zs);
                }

                CompressedBytesDict[srcFileName] = cms.ToArray();
            }
            */
        }

        [GlobalSetup(Targets = new string[] { nameof(ZLibNgComp) })]
        public void ZLibNgSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.ZLibNg);
            GlobalSetup();
        }

        [GlobalSetup(Targets = new string[] { nameof(ZLibUpComp) })]
        public void ZLibUpSetup()
        {
            Program.NativeGlobalInit(AlgorithmFlags.ZLibUp);
            GlobalSetup();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            if (Directory.Exists(_destDir))
                Directory.Delete(_destDir);
            Program.NativeGlobalCleanup();
        }

        public long ZLibComp()
        {
            long compLen;
            byte[] rawData = SrcBytes[SrcFileName];
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

            return compLen;
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public long ZLibUpComp()
        {
            return ZLibComp();
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public long ZLibNgComp()
        {
            return ZLibComp();
        }
    }
    #endregion
}
