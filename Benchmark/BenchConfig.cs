using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using System.Collections.Generic;

namespace Benchmark
{
    public class BenchConfig : ManualConfig
    {
        public const string ZLib = nameof(ZLib);
        public const string ZLibUp = nameof(ZLibUp);
        public const string ZLibNg = nameof(ZLibNg);
        public const string XZ = nameof(XZ);
        public const string LZ4 = nameof(LZ4);
        public const string ZSTD = nameof(ZSTD);

        public BenchConfig()
        {
            switch (Program.Opts.Algorithms)
            {
                case AlgorithmFlags.None:
                case AlgorithmFlags.All: // All HasFlag will be matched right below
                    return;
            }

            List<string> categories = new List<string>();
            if (Program.Opts.Algorithms.HasFlag(AlgorithmFlags.ZLibNg))
            {
                categories.Add(ZLib);
                categories.Add(ZLibNg);
            }
            else if (Program.Opts.Algorithms.HasFlag(AlgorithmFlags.ZLibUp))
            {
                categories.Add(ZLib);
                categories.Add(ZLibUp);
            }
                
            if (Program.Opts.Algorithms.HasFlag(AlgorithmFlags.XZ))
                categories.Add(XZ);
            if (Program.Opts.Algorithms.HasFlag(AlgorithmFlags.LZ4))
                categories.Add(LZ4);
            if (Program.Opts.Algorithms.HasFlag(AlgorithmFlags.Zstd))
                categories.Add(ZSTD);

            AddFilter(new AnyCategoriesFilter(categories.ToArray()));
        }
    }
}
