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

using System;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace Joveler.Compression.XZ
{
    public static class XZInit
    {
        #region Const
        internal const string MsgErrorInitFirst = "Please call XZInit.GlobalInit() first!";
        internal const string MsgErrorAlreadyInit = "Joveler.Compression.XZ is already initialized.";
        #endregion

        #region Lib, LoadLock
        internal static XZNative Lib;
        internal static readonly object LoadLock = new object();
        public static bool Loaded
        {
            get
            {
                lock (LoadLock)
                {
                    return Lib != null;
                }
            }
        }

        public static void EnsureLoaded()
        {
            lock (LoadLock)
            {
                if (Lib == null)
                    throw new InvalidOperationException(MsgErrorInitFirst);
            }
        }

        public static void EnsureNotLoaded()
        {
            lock (LoadLock)
            {
                if (Lib != null)
                    throw new InvalidOperationException(MsgErrorAlreadyInit);
            }
        }
        #endregion

        #region GlobalInit, GlobalCleanup
        public static void GlobalInit(string libPath)
        {
            lock (LoadLock)
            {
                if (Lib == null)
                {
                    Lib = new XZNative(libPath);
                }
                else
                {
                    throw new InvalidOperationException(MsgErrorInitFirst);
                }

            }
        }

        public static void GlobalCleanup()
        {
            lock (LoadLock)
            {
                if (Lib != null)
                {
                    Lib.Dispose();
                    Lib = null;
                }
                else
                {
                    throw new InvalidOperationException(MsgErrorAlreadyInit);
                }
            }
        }
        #endregion

        #region Version - (Static)
        public static Version Version()
        {
            EnsureLoaded();

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
            EnsureLoaded();

            IntPtr ptr = Lib.LzmaVersionString();
            return Marshal.PtrToStringAnsi(ptr);
        }
        #endregion

        #region Hardware - PhysMem & CPU Threads
        public static ulong PhysMem()
        {
            EnsureLoaded();

            return Lib.LzmaPhysMem();
        }

        public static uint CpuThreads()
        {
            EnsureLoaded();

            return Lib.LzmaCpuThreads();
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
        public static ulong EncoderMemUsage(LzmaCompLevel level, bool extremeFlag)
        {
            EnsureLoaded();

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
        public static ulong EncoderMemUsage(XZCompressOptions compOpts)
        {
            EnsureLoaded();

            return Lib.LzmaEasyEncoderMemUsage(compOpts.Preset);
        }

        /// <summary>
        /// Calculate approximate memory usage of multithreaded .xz encoder
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required for encoding with the given options. 
        /// If an error occurs, for example due to unsupported preset or filter chain, UINT64_MAX is returned.
        /// </returns>
        public static ulong EncoderMultiMemUsage(LzmaCompLevel level, bool extremeFlag, int threads)
        {
            EnsureLoaded();

            uint preset = XZCompressOptions.ToPreset(level, extremeFlag);
            LzmaMt mtOpts = LzmaMt.Create(preset, threads);
            return Lib.LzmaStreamEncoderMtMemUsage(mtOpts);
        }

        /// <summary>
        /// Calculate approximate memory usage of multithreaded .xz encoder
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required for encoding with the given options. 
        /// If an error occurs, for example due to unsupported preset or filter chain, UINT64_MAX is returned.
        /// </returns>
        public static ulong EncoderMultiMemUsage(XZCompressOptions compOpts, XZThreadedCompressOptions threadOpts)
        {
            EnsureLoaded();

            LzmaMt mtOpts = compOpts.ToLzmaMt(threadOpts);
            return Lib.LzmaStreamEncoderMtMemUsage(mtOpts);
        }

        /// <summary>
        /// Calculate approximate decoder memory usage of a preset
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required to decompress a file that was compressed using the given preset.
        /// If an error occurs, for example due to unsupported preset, UINT64_MAX is returned.
        /// </returns>
        public static ulong DecoderMemUsage(LzmaCompLevel level, bool extremeFlag)
        {
            EnsureLoaded();

            uint preset = XZCompressOptions.ToPreset(level, extremeFlag);
            return Lib.LzmaEasyDecoderMemUsage(preset);
        }

        /// <summary>
        /// Calculate approximate decoder memory usage of a preset
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required to decompress a file that was compressed using the given preset.
        /// If an error occurs, for example due to unsupported preset, UINT64_MAX is returned.
        /// </returns>
        public static ulong DecoderMemUsage(XZCompressOptions compOpts)
        {
            EnsureLoaded();

            return Lib.LzmaEasyDecoderMemUsage(compOpts.Preset);
        }
        #endregion
    }
}
