using BenchmarkDotNet.Running;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

namespace Benchmark
{
    #region Parameter
    [Flags]
    public enum AlgorithmFlags
    {
        None = 0x0,
        ZLib = 0x1,
        XZ = 0x2,
        LZ4 = 0x4,
        Zstd = 0x8,
        All = ZLib | XZ | LZ4 | Zstd,
    }

    public abstract class ParamOptions
    {
        [Option("algo", Default = AlgorithmFlags.All, HelpText = "Choose algorithms to benchmark | zlib,xz,lz4,all")]
        public AlgorithmFlags Algorithms { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Cast<T>() where T : ParamOptions
        {
            T cast = this as T;
            Debug.Assert(cast != null);
            return cast;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Cast<T>(ParamOptions opts) where T : ParamOptions
        {
            return opts.Cast<T>();
        }
    }

    [Verb("all", HelpText = "Benchmark all")]
    public class AllBenchOptions : ParamOptions { }

    [Verb("comp", HelpText = "Benchmark compression")]
    public class CompBenchOptions : ParamOptions { }

    [Verb("decomp", HelpText = "Benchmark decompression")]
    public class DecompBenchOptions : ParamOptions { }

    [Verb("hash", HelpText = "Benchmark hash and checksums")]
    public class HashBenchOptions : ParamOptions { }

    [Verb("buffer", HelpText = "Benchmark buffer size")]
    public class BufferSizeBenchOptions : ParamOptions { }
    #endregion

    #region Program
    public static class Program
    {
        #region PrintErrorAndExit
        internal static void PrintErrorAndExit(IEnumerable<Error> errs)
        {
            foreach (Error err in errs)
                Console.WriteLine(err.ToString());
            Environment.Exit(1);
        }
        #endregion

        #region Init and Cleanup
        public static void NativeGlobalInit()
        {
            const string runtimes = "runtimes";
            const string native = "native";

            string baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));

            string zlibPath = null;
            string xzPath = null;
            string lz4Path = null;
            string zstdPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string libDir = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X86 => Path.Combine(baseDir, runtimes, "win-x86", native),
                    Architecture.X64 => Path.Combine(baseDir, runtimes, "win-x64", native),
                    Architecture.Arm64 => Path.Combine(baseDir, runtimes, "win-arm64", native),
                    _ => throw new PlatformNotSupportedException(),
                };
                zlibPath = Path.Combine(libDir, "zlibwapi.dll");
                xzPath = Path.Combine(libDir, "liblzma.dll");
                lz4Path = Path.Combine(libDir, "liblz4.dll");
                zstdPath = Path.Combine(libDir, "libzstd.dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string libDir = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => Path.Combine(baseDir, runtimes, "linux-x64", native),
                    Architecture.Arm => Path.Combine(baseDir, runtimes, "linux-arm", native),
                    Architecture.Arm64 => Path.Combine(baseDir, runtimes, "linux-arm64", native),
                    _ => throw new PlatformNotSupportedException(),
                };
                zlibPath = Path.Combine(libDir, "libz.so");
                xzPath = Path.Combine(libDir, "liblzma.so");
                lz4Path = Path.Combine(libDir, "liblz4.so");
                zstdPath = Path.Combine(libDir, "libzstd.so");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string libDir = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => Path.Combine(baseDir, runtimes, "osx-x64", native),
                    Architecture.Arm64 => throw new PlatformNotSupportedException("TODO"),
                    _ => throw new PlatformNotSupportedException(),
                };
                zlibPath = Path.Combine(libDir, "libz.dylib");
                xzPath = Path.Combine(libDir, "liblzma.dylib");
                lz4Path = Path.Combine(libDir, "liblz4.dylib");
                zstdPath = Path.Combine(libDir, "libzstd.dylib");
            }

            if (zlibPath == null || xzPath == null || lz4Path == null)
                throw new PlatformNotSupportedException();

            Joveler.Compression.ZLib.ZLibInit.GlobalInit(zlibPath);
            Joveler.Compression.XZ.XZInit.GlobalInit(xzPath);
            Joveler.Compression.LZ4.LZ4Init.GlobalInit(lz4Path);
            Joveler.Compression.Zstd.ZstdInit.GlobalInit(zstdPath);
        }

        public static void NativeGlobalCleanup()
        {
            Joveler.Compression.ZLib.ZLibInit.GlobalCleanup();
            Joveler.Compression.XZ.XZInit.GlobalCleanup();
            Joveler.Compression.LZ4.LZ4Init.GlobalCleanup();
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
                CompBenchOptions, DecompBenchOptions, HashBenchOptions, BufferSizeBenchOptions>(args)
                .WithParsed<AllBenchOptions>(x => Opts = x)
                .WithParsed<CompBenchOptions>(x => Opts = x)
                .WithParsed<DecompBenchOptions>(x => Opts = x)
                .WithParsed<HashBenchOptions>(x => Opts = x)
                .WithParsed<BufferSizeBenchOptions>(x => Opts = x)
                .WithNotParsed(PrintErrorAndExit);
            Debug.Assert(Opts != null, $"{nameof(Opts)} != null");

            switch (Opts)
            {
                case AllBenchOptions _:
                    BenchmarkRunner.Run<CompBench>();
                    BenchmarkRunner.Run<DecompBench>();
                    BenchmarkRunner.Run<HashBench>();
                    BenchmarkRunner.Run<BufferSizeBench>();
                    break;
                case CompBenchOptions _:
                    BenchmarkRunner.Run<CompBench>();
                    break;
                case DecompBenchOptions _:
                    BenchmarkRunner.Run<DecompBench>();
                    break;
                case HashBenchOptions _:
                    BenchmarkRunner.Run<HashBench>();
                    break;
                case BufferSizeBenchOptions _:
                    BenchmarkRunner.Run<BufferSizeBench>();
                    break;
            }
        }
        #endregion
    }
    #endregion
}
