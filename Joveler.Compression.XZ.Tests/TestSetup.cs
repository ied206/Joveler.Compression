/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2019 Hajin Jang

    MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.Compression.XZ.Tests
{
    [TestClass]
    public static class TestSetup
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
            XZInit.GlobalInit(libPath);
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            XZInit.GlobalCleanup();
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
                libPath = Path.Combine(libDir, "liblzma.dll");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libPath = Path.Combine(libDir, "liblzma.so");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libPath = Path.Combine(libDir, "liblzma.dylib");

            if (libPath == null)
                throw new PlatformNotSupportedException($"Unable to find native library.");
            if (!File.Exists(libPath))
                throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

            return libPath;
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

        public static int RunXZ(string tempArchiveFile)
        {
            const string binDir = "RefBin";

            string binary = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                binary = Path.Combine(TestSetup.SampleDir, binDir, "xz.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "xz.x64.elf");
                        break;
                    case Architecture.Arm:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "xz.armhf.elf");
                        break;
                    case Architecture.Arm64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "xz.arm64.elf");
                        break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        binary = Path.Combine(TestSetup.SampleDir, binDir, "xz.x64.mach");
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
}
