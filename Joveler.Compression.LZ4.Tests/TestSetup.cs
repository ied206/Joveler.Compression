/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018 Hajin Jang

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
using System.IO;
using System.Runtime.InteropServices;

namespace Joveler.Compression.LZ4.Tests
{
    [TestClass]
    public class TestSetup
    {
        public static string BaseDir;
        public static string SampleDir;

        [AssemblyInitialize]
        public static void Init(TestContext context)
        {
            BaseDir = Path.GetFullPath(Path.Combine(TestHelper.GetProgramAbsolutePath(), "..", "..", ".."));
            SampleDir = Path.Combine(BaseDir, "Samples");

            const string x64 = "x64";
            const string x86 = "x86";
            const string armhf = "armhf";
            const string arm64 = "arm64";

            const string dllName = "liblz4.dll";
            const string soName = "liblz4.so";

            string libPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                        libPath = Path.Combine(x86, dllName);
                        break;
                    case Architecture.X64:
                        libPath = Path.Combine(x64, dllName);
                        break;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        libPath = Path.Combine(x64, soName);
                        break;
                    case Architecture.Arm:
                        libPath = Path.Combine(armhf, soName);
                        break;
                    case Architecture.Arm64:
                        libPath = Path.Combine(arm64, soName);
                        break;
                }
            }

            if (libPath == null)
                throw new PlatformNotSupportedException();

            LZ4Init.GlobalInit(libPath);
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            LZ4Init.GlobalCleanup();
        }
    }

    public class TestHelper
    {
        public static string GetProgramAbsolutePath()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (Path.GetDirectoryName(path) != null)
                path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return path;
        }

        public static int RunLZ4(string tempArchiveFile, string destFile)
        {
            const string binDir = "RefBin";

            string binary = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                binary = Path.Combine(TestSetup.SampleDir, binDir, "lz4.exe");
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
