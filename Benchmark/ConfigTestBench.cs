using BenchmarkDotNet.Attributes;
using System.Collections.Generic;

namespace Benchmark
{
    [Config(typeof(CompRatioConfig))]
    [ShortRunJob()]
    public class ConfigTestBench
    {
        // SrcFileNames
        [ParamsSource(nameof(SrcFileNames))]
        public string SrcFileName { get; set; }
        public IReadOnlyList<string> SrcFileNames { get; set; } = new List<string>()
        {
            "1",
            "2"
        };

        // Levels
        [ParamsSource(nameof(Levels))]
        public string Level { get; set; }
        public IReadOnlyList<string> Levels { get; set; } = new string[]
        {
            "Fast",
            "Best",
        };

        [GlobalSetup(Targets = new string[] { nameof(FirstDummy) })]
        public void FirstSetup()
        {
        }

        [GlobalSetup]
        public void OtherSetup()
        {
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public double FirstDummy()
        {
            // Console.WriteLine($"{Level}_{SrcFileName}");
            return 0.1;
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public double SecondDummy()
        {
            // Console.WriteLine($"{Level}_{SrcFileName}");
            return 0.2;
        }

        [Benchmark]
        [BenchmarkCategory(BenchConfig.ZLib)]
        public double ThirdDummy()
        {
            // Console.WriteLine($"{Level}_{SrcFileName}");
            return 0.3;
        }
    }
}
