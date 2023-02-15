/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2023 Hajin Jang

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

using System;

namespace Joveler.Compression.XZ
{
    public class XZHardware
    {
        #region Hardware - PhysMem & CPU Threads
        public static ulong PhysMem()
        {
            XZInit.Manager.EnsureLoaded();

            return XZInit.Lib.LzmaPhysMem();
        }

        public static uint CpuThreads()
        {
            XZInit.Manager.EnsureLoaded();

            return XZInit.Lib.LzmaCpuThreads();
        }
        #endregion

        #region (internal) Thread Limiter
        internal static uint CheckThreadCount(int threads)
        {
            if (threads < 0)
                throw new ArgumentOutOfRangeException(nameof(threads));
            if (threads == 0) // Use system's thread number by default
                threads = Environment.ProcessorCount;
            else if (Environment.ProcessorCount < threads) // If the number of CPU cores/threads exceeds system thread number,
                threads = Environment.ProcessorCount; // Limit the number of threads to keep memory usage lower.
            return (uint)threads;
        }
        #endregion
    }
}
