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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string libDir;
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                        libDir = Path.Combine(baseDir, runtimes, "win-x86", native);
                        break;
                    case Architecture.X64:
                        libDir = Path.Combine(baseDir, runtimes, "win-x64", native);
                        break;
                    case Architecture.Arm64:
                        libDir = Path.Combine(baseDir, runtimes, "win-arm64", native);
                        break;
                    default:
                        throw new PlatformNotSupportedException();
                }

                zlibPath = Path.Combine(libDir, "zlibwapi.dll");
                xzPath = Path.Combine(libDir, "liblzma.dll");
                lz4Path = Path.Combine(libDir, "liblz4.dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string libDir;
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        libDir = Path.Combine(baseDir, runtimes, "linux-x64", native);
                        break;
                    case Architecture.Arm:
                        libDir = Path.Combine(baseDir, runtimes, "linux-arm", native);
                        break;
                    case Architecture.Arm64:
                        libDir = Path.Combine(baseDir, runtimes, "linux-arm64", native);
                        break;
                    default:
                        throw new PlatformNotSupportedException();
                }

                zlibPath = Path.Combine(libDir, "libz.so");
                xzPath = Path.Combine(libDir, "liblzma.so");
                lz4Path = Path.Combine(libDir, "liblz4.so");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string libDir;
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        libDir = Path.Combine(baseDir, runtimes, "osx-x64", native);
                        break;
                    case Architecture.Arm64:
                        throw new PlatformNotSupportedException("TODO");
                    default:
                        throw new PlatformNotSupportedException();
                }

                zlibPath = Path.Combine(libDir, "libz.dylib");
                xzPath = Path.Combine(libDir, "liblzma.dylib");
                lz4Path = Path.Combine(libDir, "liblz4.dylib");
            }

            if (zlibPath == null || xzPath == null || lz4Path == null)
                throw new PlatformNotSupportedException();

            Joveler.Compression.ZLib.ZLibInit.GlobalInit(zlibPath);
            Joveler.Compression.XZ.XZInit.GlobalInit(xzPath);
            Joveler.Compression.LZ4.LZ4Init.GlobalInit(lz4Path);
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
                CompBenchOptions, DecompBenchOptions, HashBenchOptions, BufferSizeBenchOptions>(args)
                .WithParsed<AllBenchOptions>(x => opts = x)
                .WithParsed<CompBenchOptions>(x => opts = x)
                .WithParsed<DecompBenchOptions>(x => opts = x)
                .WithParsed<HashBenchOptions>(x => opts = x)
                .WithParsed<BufferSizeBenchOptions>(x => opts = x)
                .WithNotParsed(PrintErrorAndExit);
            Debug.Assert(opts != null, $"{nameof(opts)} != null");

            switch (opts)
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
