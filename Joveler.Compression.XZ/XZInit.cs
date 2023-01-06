/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2020 Hajin Jang

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
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace Joveler.Compression.XZ
{
    public static class XZInit
    {
        #region LoadManager
        internal static XZLoadManager Manager = new XZLoadManager();
        internal static XZLoader Lib => Manager.Lib;
        #endregion

        #region GlobalInit, GlobalCleanup
        public static void GlobalInit() => Manager.GlobalInit();
        public static void GlobalInit(string libPath) => Manager.GlobalInit(libPath);
        public static void GlobalCleanup() => Manager.GlobalCleanup();
        #endregion

        #region Version - (Static)
        public static Version Version()
        {
            Manager.EnsureLoaded();

            /*
             * Note from "lzma\version.h"
             *
             * The version number is of format xyyyzzzs where
             *  - x = major
             *  - yyy = minor
             *  - zzz = revision
             *  - s indicates stability: 0 = alpha, 1 = beta, 2 = stable
             *
             * The same xyyyzzz triplet is never reused with different stability levels.
             * For example, if 5.1.0alpha has been released, there will never be 5.1.0beta
             * or 5.1.0 stable.
             */

            uint verInt = Lib.LzmaVersionNumber();
            int major = (int)(verInt / 10000000u);
            int minor = (int)(verInt % 10000000u / 10000u);
            int revision = (int)(verInt % 10000u / 10u);
            int stability = (int)(verInt % 10u);

            return new Version(major, minor, revision, stability);
        }

        public static string VersionString()
        {
            Manager.EnsureLoaded();

            IntPtr ptr = Lib.LzmaVersionString();
            return Marshal.PtrToStringAnsi(ptr);
        }
        #endregion

        #region Hardware - PhysMem & CPU Threads (Obsolete)
        [Obsolete($"Replaced to {nameof(XZHardware)}.{nameof(PhysMem)}().")]
        public static ulong PhysMem()
        {
            return XZHardware.PhysMem();
        }

        [Obsolete($"Replaced to {nameof(XZHardware)}.{nameof(CpuThreads)}().")]
        public static uint CpuThreads()
        {
            return XZHardware.CpuThreads();
        }
        #endregion

        #region MemUsage
        /// <summary>
        /// Calculate approximate memory usage of single-threaded encoder
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required for the given preset when encoding.
        /// If an error occurs, for example due to unsupported preset, UINT64_MAX is returned.
        /// </returns>
        [Obsolete($"Replaced to {nameof(XZMemory)}.{nameof(XZMemory.EncoderMemUsage)}().")]
        public static ulong EncoderMemUsage(LzmaCompLevel level, bool extremeFlag)
        {
            Manager.EnsureLoaded();

            uint preset = XZCompressOptions.ToPreset(level, extremeFlag);
            return Lib.LzmaEasyEncoderMemUsage(preset);
        }

        /// <summary>
        /// Calculate approximate memory usage of single-threaded encoder
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required for the given preset when encoding.
        /// If an error occurs, for example due to unsupported preset, UINT64_MAX is returned.
        /// </returns>
        [Obsolete($"Replaced to {nameof(XZMemory)}.{nameof(XZMemory.EncoderMemUsage)}().")]
        public static ulong EncoderMemUsage(XZCompressOptions compOpts)
        {
            Manager.EnsureLoaded();

            return Lib.LzmaEasyEncoderMemUsage(compOpts.Preset);
        }

        /// <summary>
        /// Calculate approximate memory usage of multithreaded .xz encoder
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required for encoding with the given options. 
        /// If an error occurs, for example due to unsupported preset or filter chain, UINT64_MAX is returned.
        /// </returns>
        [Obsolete($"Replaced to {nameof(XZMemory)}.{nameof(XZMemory.ThreadedEncoderMemUsage)}().")]
        public static ulong EncoderMultiMemUsage(LzmaCompLevel level, bool extremeFlag, int threads)
        {
            return XZMemory.ThreadedEncoderMemUsage(level, extremeFlag, threads);
        }

        /// <summary>
        /// Calculate approximate memory usage of multithreaded .xz encoder
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required for encoding with the given options. 
        /// If an error occurs, for example due to unsupported preset or filter chain, UINT64_MAX is returned.
        /// </returns>
        [Obsolete($"Replaced to {nameof(XZMemory)}.{nameof(XZMemory.ThreadedEncoderMemUsage)}().")]
        public static ulong EncoderMultiMemUsage(XZCompressOptions compOpts, XZThreadedCompressOptions threadOpts)
        {
            return XZMemory.ThreadedEncoderMemUsage(compOpts, threadOpts);
        }

        /// <summary>
        /// Calculate approximate decoder memory usage of a preset
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required to decompress a file that was compressed using the given preset.
        /// If an error occurs, for example due to unsupported preset, UINT64_MAX is returned.
        /// </returns>
        [Obsolete($"Replaced to {nameof(XZMemory)}.{nameof(XZMemory.DecoderMemUsage)}(). Planned to be removed in next major update.")]
        public static ulong DecoderMemUsage(LzmaCompLevel level, bool extremeFlag)
        {
            return XZMemory.DecoderMemUsage(level, extremeFlag);
        }

        /// <summary>
        /// Calculate approximate decoder memory usage of a preset
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required to decompress a file that was compressed using the given preset.
        /// If an error occurs, for example due to unsupported preset, UINT64_MAX is returned.
        /// </returns>
        [Obsolete($"Replaced to {nameof(XZMemory)}.{nameof(XZMemory.DecoderMemUsage)}. Planned to be removed in next major update.")]
        public static ulong DecoderMemUsage(XZCompressOptions compOpts)
        {
            return XZMemory.DecoderMemUsage(compOpts);
        }
        #endregion
    }
}
