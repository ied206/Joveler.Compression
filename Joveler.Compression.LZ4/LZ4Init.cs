/*
    Derived from LZ4 header files (BSD 2-Clause)
    Copyright (c) 2011-2016, Yann Collet

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2019 Hajin Jang

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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Joveler.Compression.LZ4
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class LZ4Init
    {
        #region GlobalInit, GlobalCleanup
        public static void GlobalInit(string libPath)
        {
            NativeMethods.GlobalInit(libPath);
        }

        public static void GlobalCleanup()
        {
            NativeMethods.GlobalCleanup();
        }
        #endregion

        #region Version - (Static)
        public static Version Version()
        {
            NativeMethods.EnsureLoaded();

            /*
                Definition from "lz4.h"

#define LZ4_VERSION_MAJOR    1 
#define LZ4_VERSION_MINOR    8 
#define LZ4_VERSION_RELEASE  3

#define LZ4_VERSION_NUMBER (LZ4_VERSION_MAJOR *100*100 + LZ4_VERSION_MINOR *100 + LZ4_VERSION_RELEASE)

#define LZ4_LIB_VERSION LZ4_VERSION_MAJOR.LZ4_VERSION_MINOR.LZ4_VERSION_RELEASE
            */

            int verInt = (int)NativeMethods.VersionNumber();
            int major = verInt / 10000;
            int minor = verInt % 10000 / 100;
            int revision = verInt % 100;

            return new Version(major, minor, revision);
        }

        public static string VersionString()
        {
            NativeMethods.EnsureLoaded();

            IntPtr ptr = NativeMethods.VersionString();
            return Marshal.PtrToStringAnsi(ptr);
        }
        #endregion
    }
}
