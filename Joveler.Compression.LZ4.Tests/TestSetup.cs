/*
    Derived from LZ4 header files (BSD 2-Clause)
    Copyright (c) 2011-2016, Yann Collet

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2020 Hajin Jang

    Redistribution and use in source and binary forms, with or without modification,
    are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this
      list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice, this
      list of conditions and the following disclaimer in the documentation and/or
      other materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
    WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
    ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
    (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
    LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
    ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
    (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
    SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
// ReSharper disable InconsistentNaming

namespace Joveler.Compression.LZ4.Tests
{
    [TestClass]
    public class TestSetup
    {
        public static string BaseDir { get; private set; }
        public static string SampleDir { get; private set; }

        [AssemblyInitialize]
        public static void Init(TestContext context)
        {
            _ = context;

            string absPath = TestHelper.GetProgramAbsolutePath();
            BaseDir = Path.GetFullPath(Path.Combine(absPath, "..", "..", ".."));
            SampleDir = Path.Combine(BaseDir, "Samples");

            string libPath = GetNativeLibPath();
            LZ4Init.GlobalInit(libPath);
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            LZ4Init.GlobalCleanup();
        }

        private static string GetNativeLibPath()
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

            string libPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libPath = Path.Combine(libDir, "liblz4.dll");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libPath = Path.Combine(libDir, "liblz4.so");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libPath = Path.Combine(libDir, "liblz4.dylib");

            if (libPath == null)
                throw new PlatformNotSupportedException($"Unable to find native library.");
            if (!File.Exists(libPath))
                throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

            return libPath;
        }

        [TestMethod]
        public void LogRuntimeInfo()
        {
            string platform = "unknown";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                platform = "win";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                platform = "linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                platform = "osx";

            string arch = "unknown";
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    arch = "x86";
                    break;
                case Architecture.X64:
                    arch = "x64";
                    break;
                case Architecture.Arm:
                    arch = "arm";
                    break;
                case Architecture.Arm64:
                    arch = "arm64";
                    break;
            }

            Console.WriteLine($"Platform = {platform}");
            Console.WriteLine($"Arch     = {arch}");
        }
    }

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

        public static int RunLZ4(string tempArchiveFile, string destFile)
        {
            const string binDir = "RefBin";

            string binary = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                binary = Path.Combine(TestSetup.SampleDir, binDir, "lz4.x64.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "lz4.x64.elf");
                        break;
                    case Architecture.Arm:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "lz4.armhf.elf");
                        break;
                    case Architecture.Arm64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "lz4.arm64.elf");
                        break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "lz4.x64.mach");
                        break;
                    case Architecture.Arm64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "lz4.arm64.mach");
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
                    Arguments = $"-k -d {tempArchiveFile} {destFile}",
                }
            };
            proc.Start();
            proc.WaitForExit();
            return proc.ExitCode;
        }
    }
}
