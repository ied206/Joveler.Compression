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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joveler.Compression.ZLib.Tests
{
    [DoNotParallelize]
    public abstract class ZLibTestBase
    {
        protected abstract TestNativeAbi Abi { get; }

        [TestInitialize]
        public void TestInitialize()
        {
            TestSetup.InitNativeAbi(Abi);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestSetup.Cleanup();
        }

        /*
        protected static TestNativeAbi Abi { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            TestSetup.InitNativeAbi(Abi);
        }

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void ClassCleanup()
        {
            TestSetup.Cleanup();
        }
        */
    }
}
