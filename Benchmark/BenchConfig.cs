using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
#if ENABLE_MEMORY_PROFILER
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
#endif
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Benchmark
{
    #region (Base) BenchConfig
    public class BenchConfig : ManualConfig
    {
        public const string ZLib = nameof(ZLib);
        public const string XZ = nameof(XZ);
        public const string LZ4 = nameof(LZ4);
        public const string Zstd = nameof(Zstd);

        private readonly AlgorithmFlags _algoFlags;

        public BenchConfig() :
            base()
        {
            WithOptions(ConfigOptions.Default);

            _algoFlags = Program.Opts.Algorithms;

            // Memory Profiler - Takes up too much time
#if ENABLE_MEMORY_PROFILER
            AddDiagnoser(MemoryDiagnoser.Default);
            if (IsAdminPrivilege())
                AddDiagnoser(new NativeMemoryProfiler());
#endif

            // Catergory - Run only designated compression algorithm tests
            switch (_algoFlags)
            {
                case AlgorithmFlags.None:
                    return;
            }

            List<string> categories = new List<string>();
            if (_algoFlags.HasFlag(AlgorithmFlags.ZLibNg) || _algoFlags.HasFlag(AlgorithmFlags.ZLibUp))
                categories.Add(ZLib);

            if (_algoFlags.HasFlag(AlgorithmFlags.XZ))
                categories.Add(XZ);
            if (_algoFlags.HasFlag(AlgorithmFlags.LZ4))
                categories.Add(LZ4);
            if (_algoFlags.HasFlag(AlgorithmFlags.Zstd))
                categories.Add(Zstd);

            AddFilter(new AnyCategoriesFilter(categories.ToArray()));
        }

#if ENABLE_MEMORY_PROFILER
        private static bool IsAdminPrivilege()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                case Architecture.X64:
                    return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                default:
                    return false;
            }
        }
#endif
    }
    #endregion

    #region ParamOrderConfig
    public class ParamOrderer : IOrderer
    {
        public bool SeparateLogicalGroups => true;

        public IEnumerable<BenchmarkCase> GetExecutionOrder(ImmutableArray<BenchmarkCase> benchmarksCase, IEnumerable<BenchmarkLogicalGroupRule> order = null)
        {
            //return benchmarksCase.OrderByDescending(x => x.Parameters.DisplayInfo)
            //    .OrderByDescending(x => x.Descriptor.WorkloadMethodDisplayInfo);
            return benchmarksCase;
        }

        public string GetHighlightGroupKey(BenchmarkCase benchmarkCase)
        {
            return null;
        }

        public string GetLogicalGroupKey(ImmutableArray<BenchmarkCase> allBenchmarksCases, BenchmarkCase benchmarkCase)
        {
            //if (benchmarkCase.Parameters.GetArgument("SrcFileName").Value is not string srcFileName)
            //    return benchmarkCase.Job.DisplayInfo;
            return benchmarkCase.Parameters.DisplayInfo;
        }

        public IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups, IEnumerable<BenchmarkLogicalGroupRule> order = null)
        {
            return logicalGroups.OrderBy(x => x.Key);
        }

        public IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCases, Summary summary)
        {
            return benchmarksCases.OrderBy(x => x.Parameters.ValueInfo);
        }
    }

    public class ParamOrderConfig : BenchConfig
    {
        public ParamOrderConfig() : base()
        {
            // Orderder
            Orderer = new ParamOrderer();
        }
    }
    #endregion

    #region ReturnValueColumn
    public abstract class ReturnValueColumn : IColumn
    {
        public virtual string Id { get; protected set; } = $"{nameof(ReturnValueColumn)}.Return";
        public virtual string ColumnName { get; protected set; } = "Return";
        public virtual string Legend => $"Return value of the benchmark case.";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 0;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Size;

        public ReturnValueColumn()
        {
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            // Create an instance of benchmark instances
            Descriptor descriptor = benchmarkCase.Descriptor;
            object instance = Activator.CreateInstance(descriptor.Type);
            descriptor.GlobalSetupMethod?.Invoke(instance, Array.Empty<object>());

            // Get parameters from benchmarkCase & Set parameters to benchmark instances
            if (!LoadParams(instance, benchmarkCase))
                return "Param load failed";

            // Run a benchmark ourselves and record return value
            object ret = descriptor.WorkloadMethod.Invoke(instance, null);
            return ParseReturnObject(ret);
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

        public bool IsAvailable(Summary summary) => true;
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        public abstract bool LoadParams(object instance, BenchmarkCase benchmarkCase);
        public abstract string ParseReturnObject(object ret);

        protected static string ParseDouble(object ret)
        {
            if (ret is not double retVal)
                return $"[{ret.GetType().Name}] is not a dobule";
            return retVal.ToString("0.000");
        }
    }
    #endregion
}