using BenchmarkDotNet.Attributes;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CodeDom;

namespace Benchmark
{
    #region CorpusBenchBase
    [Config(typeof(BenchConfig))]
    public abstract class CorpusBenchBase
    {
        private string _sampleDir;
        private string _destDir;

        public double CompRatio { get; set; }

        [ParamsSource(nameof(SrcFileNames))]
        public string SrcFileName { get; set; }
        public IReadOnlyList<string> SrcFileNames { get; set; } = new List<string>();
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

        public abstract void NativeLibInit();

        [GlobalSetup]
        public void GlobalSetup()
        {
            _sampleDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "Samples"));
            string corpusDir = Path.Combine(_sampleDir, "Corpus");
            SrcFileNames = Directory.GetFiles(corpusDir, "*", SearchOption.TopDirectoryOnly).Select(x => Path.GetFileName(x)).ToList();

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
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            if (Directory.Exists(_destDir))
                Directory.Delete(_destDir);
            Program.NativeGlobalCleanup();
        }


        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZLib)]
        [BenchmarkCategory(BenchConfig.ZLibUp)]
        [BenchmarkCategory(BenchConfig.ZLibNg)]
        public double ZLibComp()
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

            CompRatio = (double)compLen / rawData.Length;
            return CompRatio;
        }
    }
    #endregion
}
