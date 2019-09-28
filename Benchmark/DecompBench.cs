using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    #region DecompBench
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
                    foreach (string ext in new string[] { ".zz", ".xz", ".lz4" })
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
        public long Native_LZ4()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.lz4"];
            using MemoryStream ms = new MemoryStream();
            using (MemoryStream rms = new MemoryStream(compData))
            using (Joveler.Compression.LZ4.LZ4FrameStream zs = new Joveler.Compression.LZ4.LZ4FrameStream(rms, Joveler.Compression.LZ4.LZ4Mode.Decompress))
            {
                zs.CopyTo(ms);
            }

            ms.Flush();
            return ms.Length;
        }

        [Benchmark]
        public long Managed_LZ4()
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
        public long Native_ZLib()
        {
            byte[] compData = SrcFiles[$"{Level}_{SrcFileName}.zz"];
            using MemoryStream ms = new MemoryStream();
            using (MemoryStream rms = new MemoryStream(compData))
            using (Joveler.Compression.ZLib.ZLibStream zs = new Joveler.Compression.ZLib.ZLibStream(rms, Joveler.Compression.ZLib.ZLibMode.Decompress))
            {
                zs.CopyTo(ms);
            }

            ms.Flush();
            return ms.Length;
        }

        [Benchmark]
        public long Managed_ZLib()
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
        public long Native_XZ()
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
        public long Managed_XZ()
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
    }
    #endregion
}
