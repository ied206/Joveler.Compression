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

namespace Joveler.Compression.ZLib.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ZLibInitUpCdeclTests : ZLibInitTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.UpstreamCdecl;
    }

    [TestClass]
    [DoNotParallelize]
    public class ZLibInitUpStdcallTests : ZLibInitTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.UpstreamStdcall;
    }

    [TestClass]
    [DoNotParallelize]
    public class ZLibInitNgCdeclTests : ZLibInitTestsBase
    {
        protected override TestNativeAbi Abi => TestNativeAbi.ZLibNgCdecl;
    }

    #region ZLibInitTestsBase
    public abstract class ZLibInitTestsBase : ZLibTestBase
    {
        [TestMethod]
        public void VersionTests()
        {
            Console.WriteLine(ZLibInit.VersionString());
        }

        [TestMethod]
        public void CompileFlags()
        {
            ZLibCompileFlags flags = ZLibInit.CompileFlags();

            Console.WriteLine("[*] Type sizes");
            Console.WriteLine($"{nameof(flags.CUIntSize)} = {flags.CUIntSize}");
            Console.WriteLine($"{nameof(flags.CULongSize)} = {flags.CULongSize}");
            Console.WriteLine($"{nameof(flags.PtrSize)} = {flags.PtrSize}");
            Console.WriteLine($"{nameof(flags.ZOffsetSize)} = {flags.ZOffsetSize}");
            Console.WriteLine();

            Console.WriteLine("[*] Compiler, assembler, and debug options");
            Console.WriteLine($"{nameof(flags.IsDebug)} = {flags.IsDebug}");
            Console.WriteLine($"{nameof(flags.IsWinApi)} = {flags.IsWinApi}");
            Console.WriteLine();

            Console.WriteLine("[*] One-time table building (smaller code, but not thread-safe if true)");
            Console.WriteLine($"{nameof(flags.IsBuildFixed)} = {flags.IsBuildFixed}");
            Console.WriteLine($"{nameof(flags.IsDynamicCrcTable)} = {flags.IsDynamicCrcTable}");
            Console.WriteLine();

            Console.WriteLine("[*] Library content (indicates missing functionality)");
            Console.WriteLine($"{nameof(flags.NoGZCompress)} = {flags.NoGZCompress}");
            Console.WriteLine($"{nameof(flags.NoGZip)} = {flags.NoGZip}");
            Console.WriteLine();

            Console.WriteLine("[*] Operation variations (changes in library functionality)");
            Console.WriteLine($"{nameof(flags.PKZipBugWorkaround)} = {flags.PKZipBugWorkaround}");
            Console.WriteLine($"{nameof(flags.FastestDeflateOnly)} = {flags.FastestDeflateOnly}");
            Console.WriteLine();
        }
    }
    #endregion
}
