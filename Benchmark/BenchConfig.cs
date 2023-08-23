using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Filters;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Benchmark
{
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
            _algoFlags = Program.Opts.Algorithms;

            AddDiagnoser(MemoryDiagnoser.Default);
            if (IsAdminPrivilege())
                AddDiagnoser(new NativeMemoryProfiler());

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
    }
}
