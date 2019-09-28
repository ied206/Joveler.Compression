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
    #region Program
    public class Program
    {
        #region Parameter
        public abstract class ParamOptions
        {
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
            const string x64 = "x64";
            const string x86 = "x86";
            const string armhf = "armhf";
            const string arm64 = "arm64";

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string zlibPath = null;
            string xzPath = null;
            string lz4Path = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        zlibPath = Path.Combine(baseDir, x64, "zlibwapi.dll");
                        xzPath = Path.Combine(baseDir, x64, "liblzma.dll");
                        lz4Path = Path.Combine(baseDir, x64, "liblz4.dll");
                        break;
                    case Architecture.X86:
                        zlibPath = Path.Combine(baseDir, x86, "zlibwapi.dll");
                        xzPath = Path.Combine(baseDir, x86, "liblzma.dll");
                        lz4Path = Path.Combine(baseDir, x86, "liblz4.dll");
                        break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        zlibPath = Path.Combine(baseDir, x64, "libz.so");
                        xzPath = Path.Combine(baseDir, x64, "liblzma.so");
                        lz4Path = Path.Combine(baseDir, x64, "liblz4.so");
                        break;
                    case Architecture.Arm:
                        zlibPath = Path.Combine(baseDir, armhf, "libz.so");
                        xzPath = Path.Combine(baseDir, armhf, "liblzma.so");
                        lz4Path = Path.Combine(baseDir, armhf, "liblz4.so");
                        break;
                    case Architecture.Arm64:
                        zlibPath = Path.Combine(baseDir, arm64, "libz.so");
                        xzPath = Path.Combine(baseDir, arm64, "liblzma.so");
                        lz4Path = Path.Combine(baseDir, arm64, "liblz4.so");
                        break;
                }
            }

            if (zlibPath == null || xzPath == null || lz4Path == null)
                throw new PlatformNotSupportedException();

            Joveler.Compression.ZLib.ZLibInit.GlobalInit(zlibPath, 64 * 1024);
            Joveler.Compression.XZ.XZInit.GlobalInit(xzPath);
            Joveler.Compression.LZ4.LZ4Init.GlobalInit(lz4Path, 16 * 1024);
        }

        public static void NativeGlobalCleanup()
        {
            Joveler.Compression.ZLib.ZLibInit.GlobalCleanup();
            Joveler.Compression.XZ.XZInit.GlobalCleanup();
            Joveler.Compression.LZ4.LZ4Init.GlobalCleanup();
        }
        #endregion

        #region Main
        public static void Main(string[] args)
        {
            ParamOptions opts = null;
            Parser argParser = new Parser(conf =>
            {
                conf.HelpWriter = Console.Out;
                conf.CaseInsensitiveEnumValues = true;
                conf.CaseSensitive = false;
            });

            argParser.ParseArguments<AllBenchOptions,
                CompBenchOptions, DecompBenchOptions, HashBenchOptions>(args)
                .WithParsed<AllBenchOptions>(x => opts = x)
                .WithParsed<CompBenchOptions>(x => opts = x)
                .WithParsed<DecompBenchOptions>(x => opts = x)
                .WithParsed<HashBenchOptions>(x => opts = x)
                .WithNotParsed(PrintErrorAndExit);
            Debug.Assert(opts != null, $"{nameof(opts)} != null");

            switch (opts)
            {
                case AllBenchOptions _:
                    BenchmarkRunner.Run<CompBench>();
                    BenchmarkRunner.Run<DecompBench>();
                    BenchmarkRunner.Run<HashBench>();
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
            }
        }
        #endregion
    }
    #endregion
}
