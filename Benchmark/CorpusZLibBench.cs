using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    public class CorpusZLibUpBench : CorpusBenchBase
    {
        public override void NativeLibInit()
        {
            Program.NativeGlobalInit(AlgorithmFlags.ZLibUp);
        }
    }

    public class CorpusZLibNgBench : CorpusBenchBase
    {
        public override void NativeLibInit()
        {
            Program.NativeGlobalInit(AlgorithmFlags.ZLibNg);
        }
    }
}
