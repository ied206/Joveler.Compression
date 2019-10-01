using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Joveler.Compression.LZ4.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [TestClass]
    [TestCategory("Joveler.Compression.LZ4")]
    public class LZ4InitTests
    {
        [TestMethod]
        public void VersionTests()
        {
            Version verInst = LZ4Init.Version();
            Console.WriteLine(verInst);
            string verStr = LZ4Init.VersionString();
            Console.WriteLine(verStr);
        }
    }
}
