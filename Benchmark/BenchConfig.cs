using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using System.Collections.Generic;

namespace Benchmark
{
    public class BenchConfig : ManualConfig
    {
        public const string ZLib = "ZLib";
        public const string XZ = "XZ";
        public const string LZ4 = "LZ4";

        public BenchConfig()
        {
            switch (Program.Opts.Algorithms)
            {
                case AlgorithmFlags.None:
                case AlgorithmFlags.All:
                    return;
            }

            List<string> categories = new List<string>();
            if (Program.Opts.Algorithms.HasFlag(AlgorithmFlags.ZLib))
                categories.Add(ZLib);
            else if (Program.Opts.Algorithms.HasFlag(AlgorithmFlags.XZ))
                categories.Add(XZ);
            else if (Program.Opts.Algorithms.HasFlag(AlgorithmFlags.LZ4))
                categories.Add(LZ4);

            AddFilter(new AnyCategoriesFilter(categories.ToArray()));
        }
    }
}
