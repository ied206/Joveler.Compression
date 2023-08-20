using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Benchmark
{
    #region Parameters
    [Flags]
    public enum AlgorithmFlags
    {
        None = 0x0,
        ZLib = 0x2,
        ZLibUp = 0x1,
        ZLibNg = 0x2,
        XZ = 0x4,
        LZ4 = 0x8,
        Zstd = 0x10,
        All = ZLibUp | ZLibNg | XZ | LZ4 | Zstd,
    }

    public abstract class ParamOptions
    {
        [Option("algo", Default = AlgorithmFlags.All, HelpText = "Choose algorithms to benchmark | zlib,xz,lz4,all")]
        public AlgorithmFlags Algorithms { get; set; }
    }

    [Verb("all", HelpText = "Benchmark all")]
    public class AllBenchOptions : ParamOptions { }

    [Verb("comp", HelpText = "Benchmark compression")]
    public class CompBenchOptions : ParamOptions { }

    [Verb("decomp", HelpText = "Benchmark decompression")]
    public class DecompBenchOptions : ParamOptions { }

    [Verb("xzmulti", HelpText = "Benchmark multithread options (XZ only)")]
    public class XZMultiOptionBenchOptions : ParamOptions { }

    [Verb("hash", HelpText = "Benchmark hash and checksums")]
    public class HashBenchOptions : ParamOptions { }

    [Verb("buffer", HelpText = "Benchmark buffer size")]
    public class BufferSizeBenchOptions : ParamOptions { }

    [Verb("zlib-fork", HelpText = "Compare zlib forks")]
    public class ZLibForkBenchOptions : ParamOptions { }
    #endregion

    #region Program
    public static class Program
    {
        #region Directories
        public static string BaseDir => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        public static string SampleDir => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "Samples"));
        #endregion

        #region PrintErrorAndExit
        internal static void PrintErrorAndExit(IEnumerable<Error> errs)
        {
            foreach (Error err in errs)
                Console.WriteLine(err.ToString());
            Environment.Exit(1);
        }
        #endregion

        #region Static properties
        private static AlgorithmFlags _initFlags = AlgorithmFlags.None;
        #endregion

        #region Init and Cleanup
        public static void NativeGlobalInit(AlgorithmFlags flags)
        {
            _initFlags = flags;

            const string runtimes = "runtimes";
            const string native = "native";

            string zlibNgCompatPath = null;
            string zlibUpstreamPath = null;
            string xzPath = null;
            string lz4Path = null;
            string zstdPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string libDir = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X86 => Path.Combine(BaseDir, runtimes, "win-x86", native),
                    Architecture.X64 => Path.Combine(BaseDir, runtimes, "win-x64", native),
                    Architecture.Arm64 => Path.Combine(BaseDir, runtimes, "win-arm64", native),
                    _ => throw new PlatformNotSupportedException(),
                };
                zlibNgCompatPath = Path.Combine(libDir, "zlib1.dll");
                zlibUpstreamPath = Path.Combine(libDir, "zlibwapi-upstream.dll");
                xzPath = Path.Combine(libDir, "liblzma.dll");
                lz4Path = Path.Combine(libDir, "liblz4.dll");
                zstdPath = Path.Combine(libDir, "libzstd.dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string libDir = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => Path.Combine(BaseDir, runtimes, "linux-x64", native),
                    Architecture.Arm => Path.Combine(BaseDir, runtimes, "linux-arm", native),
                    Architecture.Arm64 => Path.Combine(BaseDir, runtimes, "linux-arm64", native),
                    _ => throw new PlatformNotSupportedException(),
                };
                zlibNgCompatPath = Path.Combine(libDir, "libz.so");
                zlibUpstreamPath = Path.Combine(libDir, "libz-upstream.so");
                xzPath = Path.Combine(libDir, "liblzma.so");
                lz4Path = Path.Combine(libDir, "liblz4.so");
                zstdPath = Path.Combine(libDir, "libzstd.so");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string libDir = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => Path.Combine(BaseDir, runtimes, "osx-x64", native),
                    Architecture.Arm64 => Path.Combine(BaseDir, runtimes, "osx-arm64", native),
                    _ => throw new PlatformNotSupportedException(),
                };
                zlibNgCompatPath = Path.Combine(libDir, "libz.dylib");
                zlibUpstreamPath = Path.Combine(libDir, "libz-upstream.dylib");
                xzPath = Path.Combine(libDir, "liblzma.dylib");
                lz4Path = Path.Combine(libDir, "liblz4.dylib");
                zstdPath = Path.Combine(libDir, "libzstd.dylib");
            }

            if (zlibNgCompatPath == null || zlibUpstreamPath == null || 
                xzPath == null || lz4Path == null)
                throw new PlatformNotSupportedException();

            // zlib-ng and zlib are mutually exclusive.
            // Joveler.Compression.ZLib cannot load two or more zlib at once.
            if (flags.HasFlag(AlgorithmFlags.ZLibNg))
                Joveler.Compression.ZLib.ZLibInit.GlobalInit(zlibNgCompatPath);
            else if (flags.HasFlag(AlgorithmFlags.ZLibUp)) 
                Joveler.Compression.ZLib.ZLibInit.GlobalInit(zlibUpstreamPath);

            if (flags.HasFlag(AlgorithmFlags.XZ))
                Joveler.Compression.XZ.XZInit.GlobalInit(xzPath);
            if (flags.HasFlag(AlgorithmFlags.LZ4))
                Joveler.Compression.LZ4.LZ4Init.GlobalInit(lz4Path);
            if (flags.HasFlag(AlgorithmFlags.Zstd))
                Joveler.Compression.Zstd.ZstdInit.GlobalInit(zstdPath);
        }

        public static void NativeGlobalCleanup()
        {
            if (_initFlags.HasFlag(AlgorithmFlags.ZLibNg) || _initFlags.HasFlag(AlgorithmFlags.ZLibUp))
                Joveler.Compression.ZLib.ZLibInit.GlobalCleanup();
            if (_initFlags.HasFlag(AlgorithmFlags.XZ))
                Joveler.Compression.XZ.XZInit.GlobalCleanup();
            if (_initFlags.HasFlag(AlgorithmFlags.LZ4))
                Joveler.Compression.LZ4.LZ4Init.GlobalCleanup();
            if (_initFlags.HasFlag(AlgorithmFlags.Zstd))
                Joveler.Compression.Zstd.ZstdInit.GlobalCleanup();
        }
        #endregion

        #region Commnad Line Options
        public static ParamOptions Opts { get; private set; }
        #endregion

        #region Main
        public static void Main(string[] args)
        {
            Parser argParser = new Parser(conf =>
            {
                conf.HelpWriter = Console.Out;
                conf.CaseInsensitiveEnumValues = true;
                conf.CaseSensitive = false;
            });

            argParser.ParseArguments<AllBenchOptions,
                CompBenchOptions, DecompBenchOptions, XZMultiOptionBenchOptions, 
                HashBenchOptions, BufferSizeBenchOptions, ZLibForkBenchOptions>(args)
                .WithParsed<AllBenchOptions>(x => Opts = x)
                .WithParsed<CompBenchOptions>(x => Opts = x)
                .WithParsed<DecompBenchOptions>(x => Opts = x)
                .WithParsed<XZMultiOptionBenchOptions>(x => Opts = x)
                .WithParsed<HashBenchOptions>(x => Opts = x)
                .WithParsed<BufferSizeBenchOptions>(x => Opts = x)
                .WithParsed<ZLibForkBenchOptions>(x => Opts = x)
                .WithNotParsed(PrintErrorAndExit);
            Debug.Assert(Opts != null, $"{nameof(Opts)} != null");

            // InvertedTomato.Crc is the slowest, and ships unoptimized binaries.
            // Disable for awhile to avoid BenchmarkDotNet's unoptimized run error.
#if INVERTEDTOMATO_CRC_EANBLE
            ManualConfig config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);
#else
            ManualConfig config = DefaultConfig.Instance.WithOptions(ConfigOptions.Default);
#endif

            switch (Opts)
            {
                case AllBenchOptions _:
                    BenchmarkRunner.Run<CompBench>(config);
                    BenchmarkRunner.Run<DecompBench>(config);
                    BenchmarkRunner.Run<XZMultiOptionBench>(config);
                    BenchmarkRunner.Run<HashBench>(config);
                    BenchmarkRunner.Run<BufferSizeBench>(config);
                    break;
                case CompBenchOptions _:
                    BenchmarkRunner.Run<CompBench>(config);
                    break;
                case DecompBenchOptions _:
                    BenchmarkRunner.Run<DecompBench>(config);
                    break;
                case XZMultiOptionBenchOptions _:
                    BenchmarkRunner.Run<XZMultiOptionBench>(config);
                    break;
                case HashBenchOptions _:
                    BenchmarkRunner.Run<HashBench>(config);
                    break;
                case BufferSizeBenchOptions _:
                    BenchmarkRunner.Run<BufferSizeBench>(config);
                    break;
                case ZLibForkBenchOptions _:
                    BenchmarkRunner.Run<CorpusZLibUpBench>(config);
                    BenchmarkRunner.Run<CorpusZLibNgBench>(config);
                    break;
            }
        }
        #endregion
    }
    #endregion
}
