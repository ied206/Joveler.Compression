using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Joveler.Compression.ZLib.Tests
{
    [TestClass]
    [TestCategory("Joveler.Compression.ZLib")]
    public class ZLibInitTests
    {
        [TestMethod]
        public void VersionTests()
        {
            Console.WriteLine(ZLibInit.VersionString());
        }
    }
}
