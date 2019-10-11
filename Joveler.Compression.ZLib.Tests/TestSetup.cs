/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

    C# tests by Hajin Jang
    Copyright (C) 2017-2019 Hajin Jang

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

namespace Joveler.Compression.ZLib.Tests
{
    #region TestSetup
    [TestClass]
    public class TestSetup
    {
        public static string BaseDir { get; private set; }
        public static string SampleDir { get; private set; }

        [AssemblyInitialize]
        public static void Init(TestContext ctx)
        {
            BaseDir = Path.GetFullPath(Path.Combine(TestHelper.GetProgramAbsolutePath(), "..", "..", ".."));
            SampleDir = Path.Combine(BaseDir, "Samples");

            const string x64 = "x64";
            const string x86 = "x86";
            const string armhf = "armhf";
            const string arm64 = "arm64";

            const string dllName = "zlibwapi.dll";
            const string soName = "libz.so";
            const string dylibName = "libz.dylib";

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
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X64:
                        libPath = Path.Combine(x64, dylibName);
                        break;
                }
            }

            if (libPath == null)
                throw new PlatformNotSupportedException();

            ZLibInit.GlobalInit(libPath);
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            ZLibInit.GlobalCleanup();
        }
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
                binary = Path.Combine(TestSetup.SampleDir, binDir, "pigz.exe");
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
