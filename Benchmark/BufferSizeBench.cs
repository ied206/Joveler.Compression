using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

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

        public List<byte[]> RawDataList { get; set; } = new List<byte[]>(2);
        public List<byte[]> ZLibDataList { get; set; } = new List<byte[]>(2);
        public List<byte[]> XZDataList { get; set; } = new List<byte[]>(2);
        #endregion

        #region Startup and Cleanup
        [GlobalSetup]
        public void GlobalSetup()
        {
            Program.NativeGlobalInit();

            // Populate RawDataList and XZDataList, ZLibDataList
            int bigSize = BufferSizes.Max() * 2;
            int medianSize = BufferSizes[BufferSizes.Count / 2];
            for (int i = 0; i < 2; i++)
            {
                byte[] rawData = i == 0 ? new byte[bigSize] : new byte[medianSize];
                Span<byte> firstSpan = rawData.AsSpan(0, rawData.Length / 4);
                Span<byte> secondSpan = rawData.AsSpan(rawData.Length / 2, rawData.Length / 4);
                Random random = new Random(rawData.Length);
                random.NextBytes(firstSpan);
                random.NextBytes(secondSpan);
                RawDataList.Add(rawData);

                // Populate _xzData
                using (MemoryStream ms = new MemoryStream())
                {
                    Joveler.Compression.XZ.XZCompressOptions compOpts = new Joveler.Compression.XZ.XZCompressOptions();
                    using (MemoryStream rms = new MemoryStream(rawData))
                    using (Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(ms, compOpts))
                    {
                        rms.CopyTo(xzs);
                    }
                    byte[] xzData = ms.ToArray();
                    XZDataList.Add(xzData);
                }

                // Populate _zlibData
                using (MemoryStream ms = new MemoryStream())
                {
                    Joveler.Compression.ZLib.ZLibCompressOptions compOpts = new Joveler.Compression.ZLib.ZLibCompressOptions();
                    using (MemoryStream rms = new MemoryStream(rawData))
                    using (Joveler.Compression.ZLib.ZLibStream xzs = new Joveler.Compression.ZLib.ZLibStream(ms, compOpts))
                    {
                        rms.CopyTo(xzs);
                    }
                    byte[] zlibData = ms.ToArray();
                    ZLibDataList.Add(zlibData);
                }
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            RawDataList = null;
            XZDataList = null;
            Program.NativeGlobalCleanup();
        }
        #endregion

        #region XZ_Compress
        [Benchmark]
        public void XZ_Compress()
        {
            Joveler.Compression.XZ.XZCompressOptions compOpts = new Joveler.Compression.XZ.XZCompressOptions()
            {
                BufferSize = BufferSize,
            };
            
            foreach (byte[] rawData in RawDataList)
            {
                using MemoryStream ms = new MemoryStream();
                using MemoryStream rms = new MemoryStream(rawData);
                using Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(ms, compOpts);
                rms.CopyTo(xzs);
            }
        }
        #endregion

        #region XZ_Decompress
        [Benchmark]
        public void XZ_Decompress()
        {
            Joveler.Compression.XZ.XZDecompressOptions decompOpts = new Joveler.Compression.XZ.XZDecompressOptions()
            {
                BufferSize = BufferSize,
            };

            foreach (byte[] xzData in XZDataList)
            {
                using MemoryStream ms = new MemoryStream();
                using MemoryStream rms = new MemoryStream(xzData);
                using Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(rms, decompOpts);
                xzs.CopyTo(ms);
            }
        }
        #endregion

        #region ZLib_Compress
        [Benchmark]
        public void ZLib_Compress()
        {
            Joveler.Compression.ZLib.ZLibCompressOptions compOpts = new Joveler.Compression.ZLib.ZLibCompressOptions()
            {
                BufferSize = BufferSize,
            };

            foreach (byte[] rawData in RawDataList)
            {
                using MemoryStream ms = new MemoryStream();
                using MemoryStream rms = new MemoryStream(rawData);
                using Joveler.Compression.ZLib.ZLibStream zs = new Joveler.Compression.ZLib.ZLibStream(ms, compOpts);
                rms.CopyTo(zs);
            }
        }
        #endregion

        #region ZLib_Decompress
        [Benchmark]
        public void ZLib_Decompress()
        {
            Joveler.Compression.ZLib.ZLibDecompressOptions decompOpts = new Joveler.Compression.ZLib.ZLibDecompressOptions()
            {
                BufferSize = BufferSize,
            };

            foreach (byte[] zlibData in ZLibDataList)
            {
                using MemoryStream ms = new MemoryStream();
                using MemoryStream rms = new MemoryStream(zlibData);
                using Joveler.Compression.ZLib.ZLibStream zs = new Joveler.Compression.ZLib.ZLibStream(rms, decompOpts);
                zs.CopyTo(ms);
            }
        }
        #endregion
    }
}
