using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Benchmark
{
    public class BufferSizeBench
    {
        #region Fields and Properties
        // BufferSizes
        [ParamsSource(nameof(BufferSizes))]
        public int BufferSize { get; set; }
        public IReadOnlyList<int> BufferSizes { get; set; } = new int[]
        {
            4 * 1024,
            16 * 1024,
            64 * 1024,
            256 * 1024,
            1024 * 1024,
            4 * 1024 * 1024,
        };

        // SrcFiles
        public IReadOnlyList<string> SrcFileNames { get; set; } = new string[]
        {
            "Banner.bmp",
            "Banner.svg",
            "Type4.txt",
        };
        private byte[] _srcData;
        #endregion

        #region Startup and Cleanup
        [GlobalSetup]
        public void GlobalSetup()
        {
            Program.NativeGlobalInit();

            // Populate _srcData
            int medianSize = BufferSizes[BufferSizes.Count / 2];

            string sampleDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "Samples"));

            List<byte[]> rawDataList = new List<byte[]>(SrcFileNames.Count);
            foreach (string srcFileName in SrcFileNames)
            {
                string srcFile = Path.Combine(sampleDir, "Raw", srcFileName);

                byte[] rawData;
                using (FileStream fs = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    rawData = new byte[fs.Length];
                    fs.Read(rawData, 0, rawData.Length);
                }
                rawDataList.Add(rawData);
            }

            using (MemoryStream ms = new MemoryStream(medianSize))
            {
                int i = 0;
                while (ms.Position <= medianSize)
                {
                    byte[] rawData = rawDataList[i];
                    ms.Write(rawData, 0, rawData.Length);

                    i += 1;
                    if (i == rawDataList.Count)
                        i = 0;
                }

                _srcData = ms.ToArray();
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _srcData = null;
            Program.NativeGlobalCleanup();
        }
        #endregion

        #region LZ4
        [Benchmark]
        public void LZ4()
        {
            Joveler.Compression.LZ4.LZ4FrameCompressOptions compOpts = new Joveler.Compression.LZ4.LZ4FrameCompressOptions()
            {
                BufferSize = BufferSize,
            };

            Joveler.Compression.LZ4.LZ4FrameDecompressOptions decompOpts = new Joveler.Compression.LZ4.LZ4FrameDecompressOptions()
            {
                BufferSize = BufferSize,
            };

            byte[] xzData;
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(_srcData))
                using (Joveler.Compression.LZ4.LZ4FrameStream xzs = new Joveler.Compression.LZ4.LZ4FrameStream(ms, compOpts))
                {
                    rms.CopyTo(xzs);
                }
                xzData = ms.ToArray();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(xzData))
                using (Joveler.Compression.LZ4.LZ4FrameStream xzs = new Joveler.Compression.LZ4.LZ4FrameStream(rms, decompOpts))
                {
                    xzs.CopyTo(ms);
                }
            }
        }
        #endregion

        #region XZ
        [Benchmark]
        public void XZ()
        {
            Joveler.Compression.XZ.XZCompressOptions compOpts = new Joveler.Compression.XZ.XZCompressOptions()
            {
                BufferSize = BufferSize,
            };

            Joveler.Compression.XZ.XZDecompressOptions decompOpts = new Joveler.Compression.XZ.XZDecompressOptions()
            {
                BufferSize = BufferSize,
            };

            byte[] xzData;
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(_srcData))
                using (Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(ms, compOpts))
                {
                    rms.CopyTo(xzs);
                }
                xzData = ms.ToArray();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(xzData))
                using (Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(rms, decompOpts))
                {
                    xzs.CopyTo(ms);
                }
            }
        }
        #endregion

        #region ZLib
        [Benchmark]
        public void ZLib()
        {
            Joveler.Compression.ZLib.ZLibCompressOptions compOpts = new Joveler.Compression.ZLib.ZLibCompressOptions()
            {
                BufferSize = BufferSize,
            };

            Joveler.Compression.ZLib.ZLibDecompressOptions decompOpts = new Joveler.Compression.ZLib.ZLibDecompressOptions()
            {
                BufferSize = BufferSize,
            };

            byte[] zlibData;
            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(_srcData))
                using (Joveler.Compression.ZLib.ZLibStream xzs = new Joveler.Compression.ZLib.ZLibStream(ms, compOpts))
                {
                    rms.CopyTo(xzs);
                }
                zlibData = ms.ToArray();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (MemoryStream rms = new MemoryStream(zlibData))
                using (Joveler.Compression.ZLib.ZLibStream xzs = new Joveler.Compression.ZLib.ZLibStream(rms, decompOpts))
                {
                    xzs.CopyTo(ms);
                }
            }
        }
        #endregion
    }
}
