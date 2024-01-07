/*
    C# tests by Hajin Jang
    Copyright (C) 2017-present Hajin Jang

    zlib license

    This software is provided 'as-is', without any express or implied
    warranty.  In no event will the authors be held liable for any damages
    arising from the use of this software.

    Permission is granted to anyone to use this software for any purpose,
    including commercial applications, and to alter it and redistribute it
    freely, subject to the following restrictions:

    1. The origin of this software must not be misrepresented; you must not
       claim that you wrote the original software. If you use this software
       in a product, an acknowledgment in the product documentation would be
       appreciated but is not required.
    2. Altered source versions must be plainly marked as such, and must not be
       misrepresented as being the original software.
    3. This notice may not be removed or altered from any source distribution.
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

// This tests cannot be parallelized to test two or more native abis at once.
[assembly: DoNotParallelize]

namespace Joveler.Compression.ZLib.Tests
{
    public enum TestNativeAbi
    {
        None = 0,
        UpstreamCdecl = 1,
        UpstreamStdcall = 2,
        ZLibNgCdecl = 3,
    }

    #region TestSetup
    [TestClass]
    public class TestSetup
    {
        public static string BaseDir { get; private set; }
        public static string SampleDir { get; private set; }

        [AssemblyInitialize]
        public static void Init(TestContext context)
        {
            // Differet from other compression wrappers, Joveler.Compression.ZLib supports multiple native ABIs.
            // To test all ABIs, Joveler.Compression.ZLib.Tests do not load native library at AssemblyInitialize.
            // Instead, the libraries will be loaded in ClassInitialize, with DoNotParallelize attribute added to classes.
            _ = context;

            string absPath = TestHelper.GetProgramAbsolutePath();
            BaseDir = Path.GetFullPath(Path.Combine(absPath, "..", "..", ".."));
            SampleDir = Path.Combine(BaseDir, "Samples");
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            ZLibInit.TryGlobalCleanup();
        }

        public static void InitNativeAbi(TestNativeAbi abi)
        {
            // Joveler.Compression.ZLib ships with zlib-ng compat binaries.
            // However, Joveler.Compression.ZLib.Tests also contains zlib-ng modern ABI binaries and zlib stdcall binaries for testing.
            string libPath = GetNativeLibPath(abi);
            ZLibInit.GlobalInit(libPath, GetNativeLoadOptions(abi));
        }

        public static ZLibInitOptions GetNativeLoadOptions(TestNativeAbi abi)
        {
            ZLibInitOptions opts = new ZLibInitOptions();
            switch (abi)
            {
                case TestNativeAbi.UpstreamCdecl:
                    opts.IsWindowsStdcall = false;
                    opts.IsZLibNgModernAbi = false;
                    break;
                case TestNativeAbi.UpstreamStdcall:
                    opts.IsWindowsStdcall = true;
                    opts.IsZLibNgModernAbi = false;
                    break;
                case TestNativeAbi.ZLibNgCdecl:
                    opts.IsWindowsStdcall = false;
                    opts.IsZLibNgModernAbi = true;
                    break;
                default:
                    throw new NotImplementedException();
            }
            return opts;
        }

        public static string GetNativeLibPath(TestNativeAbi abi)
        {
            string libDir = string.Empty;

#if !NETFRAMEWORK
            libDir = "runtimes";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libDir = Path.Combine(libDir, "win-");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libDir = Path.Combine(libDir, "linux-");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libDir = Path.Combine(libDir, "osx-");
#endif

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    libDir += "x86";
                    break;
                case Architecture.X64:
                    libDir += "x64";
                    break;
                case Architecture.Arm:
                    libDir += "arm";
                    break;
                case Architecture.Arm64:
                    libDir += "arm64";
                    break;
            }

#if !NETFRAMEWORK
            libDir = Path.Combine(libDir, "native");
#endif

            string libFileName = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (abi)
                {
                    case TestNativeAbi.UpstreamCdecl:
                        libFileName = "zlib1.dll";
                        break;
                    case TestNativeAbi.UpstreamStdcall:
                        libFileName = "zlibwapi.dll";
                        break;
                    case TestNativeAbi.ZLibNgCdecl:
                        libFileName = "zlib-ng2.dll";
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (abi)
                {
                    case TestNativeAbi.UpstreamCdecl:
                    case TestNativeAbi.UpstreamStdcall:
                        libFileName = "libz.so";
                        break;
                    case TestNativeAbi.ZLibNgCdecl:
                        libFileName = "libz-ng.so";
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                switch (abi)
                {
                    case TestNativeAbi.UpstreamCdecl:
                    case TestNativeAbi.UpstreamStdcall:
                        libFileName = "libz.dylib";
                        break;
                    case TestNativeAbi.ZLibNgCdecl:
                        libFileName = "libz-ng.dylib";
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            if (libFileName == null)
                throw new PlatformNotSupportedException($"Unable to find native library.");

            string libPath = Path.Combine(libDir, libFileName);
            if (!File.Exists(libPath))
                throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

            return libPath;
        }

        #region LogEnvironment
        [TestMethod]
        public void LogEnvironment()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine($"OS = {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}");
            b.AppendLine($"Dotnet Runtime = {RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture}");
            Console.WriteLine(b.ToString());
        }
        #endregion
    }
    #endregion

    #region TestHelper
    public static class TestHelper
    {
        public static string GetProgramAbsolutePath()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (Path.GetDirectoryName(path) != null)
                path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return path;
        }

        public static byte[] SHA256Digest(Stream stream)
        {
            using (HashAlgorithm hash = SHA256.Create())
            {
                return hash.ComputeHash(stream);
            }
        }

        public static byte[] SHA256Digest(byte[] input)
        {
            using (HashAlgorithm hash = SHA256.Create())
            {
                return hash.ComputeHash(input);
            }
        }

        public static int RunPigz(string tempArchiveFile)
        {
            const string binDir = "RefBin";

            string binary = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "pigz.x86.exe");
                        break;
                    case Architecture.X64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "pigz.x64.exe");
                        break;
                    case Architecture.Arm64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "pigz.arm64.exe");
                        break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "pigz.x64.elf");
                        break;
                    case Architecture.Arm:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "pigz.armhf.elf");
                        break;
                    case Architecture.Arm64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "pigz.arm64.elf");
                        break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "pigz.x64.mach");
                        break;
                    case Architecture.Arm64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "pigz.arm64.mach");
                        break;
                }
            }

            if (binary == null)
                throw new PlatformNotSupportedException();

            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = binary,
                    Arguments = $"-k -d {tempArchiveFile}",
                }
            };
            proc.Start();
            proc.WaitForExit();
            return proc.ExitCode;
        }
    }
    #endregion
}
