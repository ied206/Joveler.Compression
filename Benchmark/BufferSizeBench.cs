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

        // RawData
        public List<byte[]> RawDataList { get; set; } = new List<byte[]>(2);

        // XZData
        public List<byte[]> XZDataList { get; set; } = new List<byte[]>(2);
        #endregion

        #region Startup and Cleanup
        [GlobalSetup]
        public void GlobalSetup()
        {
            Program.NativeGlobalInit();

            // Populate RawDataList and XZDataList
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
                Joveler.Compression.XZ.XZCompressOptions compOpts = new Joveler.Compression.XZ.XZCompressOptions();
                Joveler.Compression.XZ.XZStreamOptions advOpts = new Joveler.Compression.XZ.XZStreamOptions();
                using MemoryStream ms = new MemoryStream();
                using MemoryStream rms = new MemoryStream(rawData);
                using (Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(ms, compOpts, advOpts))
                {
                    rms.CopyTo(xzs);
                }
                byte[] xzData = ms.ToArray();
                XZDataList.Add(xzData);
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
            Joveler.Compression.XZ.XZCompressOptions compOpts = new Joveler.Compression.XZ.XZCompressOptions();
            Joveler.Compression.XZ.XZStreamOptions advOpts = new Joveler.Compression.XZ.XZStreamOptions()
            {
                BufferSize = BufferSize,
            };

            foreach (byte[] rawData in RawDataList)
            {
                using MemoryStream ms = new MemoryStream();
                using MemoryStream rms = new MemoryStream(rawData);
                using Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(ms, compOpts, advOpts);
                rms.CopyTo(xzs);
            }
        }
        #endregion

        #region XZ_Deompress
        [Benchmark]
        public void XZ_Decompress()
        {
            Joveler.Compression.XZ.XZDecompressOptions decompOpts = new Joveler.Compression.XZ.XZDecompressOptions();

            foreach (byte[] xzData in XZDataList)
            {
                using MemoryStream ms = new MemoryStream();
                using MemoryStream rms = new MemoryStream(xzData);
                using Joveler.Compression.XZ.XZStream xzs = new Joveler.Compression.XZ.XZStream(ms, decompOpts);
                xzs.CopyTo(ms);
            }
        }
        #endregion
    }
}
