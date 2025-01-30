/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-present Hajin Jang

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
    public static class XZMemory
    {
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
            XZInit.Manager.EnsureLoaded();

            if (XZInit.Lib == null)
                throw new ObjectDisposedException(nameof(XZInit));

            uint preset = XZCompressOptions.ToPreset(level, extremeFlag);
            return XZInit.Lib.LzmaEasyEncoderMemUsage?.Invoke(preset) ?? throw new EntryPointNotFoundException(nameof(XZInit.Lib.LzmaEasyEncoderMemUsage));
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
            XZInit.Manager.EnsureLoaded();

            if (XZInit.Lib == null)
                throw new ObjectDisposedException(nameof(XZInit));

            return XZInit.Lib.LzmaEasyEncoderMemUsage?.Invoke(compOpts.Preset) ?? throw new EntryPointNotFoundException(nameof(XZInit.Lib.LzmaEasyEncoderMemUsage));
        }

        /// <summary>
        /// Calculate approximate memory usage of multithreaded .xz encoder
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required for encoding with the given options. 
        /// If an error occurs, for example due to unsupported preset or filter chain, UINT64_MAX is returned.
        /// </returns>
        public static ulong ThreadedEncoderMemUsage(LzmaCompLevel level, bool extremeFlag, int threads)
        {
            XZInit.Manager.EnsureLoaded();

            if (XZInit.Lib == null)
                throw new ObjectDisposedException(nameof(XZInit));

            uint preset = XZCompressOptions.ToPreset(level, extremeFlag);
            LzmaMt mtOpts = LzmaMt.Create(preset, threads);
            return XZInit.Lib.LzmaStreamEncoderMtMemUsage?.Invoke(mtOpts) ?? throw new EntryPointNotFoundException(nameof(XZInit.Lib.LzmaStreamEncoderMtMemUsage));
        }

        /// <summary>
        /// Calculate approximate memory usage of multithreaded .xz encoder
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required for encoding with the given options. 
        /// If an error occurs, for example due to unsupported preset or filter chain, UINT64_MAX is returned.
        /// </returns>
        public static ulong ParallelEncoderMemUsage(XZCompressOptions compOpts, XZParallelCompressOptions threadOpts)
        {
            XZInit.Manager.EnsureLoaded();

            if (XZInit.Lib == null)
                throw new ObjectDisposedException(nameof(XZInit));

            LzmaMt mtOpts = compOpts.ToLzmaMt(threadOpts);
            return XZInit.Lib.LzmaStreamEncoderMtMemUsage?.Invoke(mtOpts) ?? throw new EntryPointNotFoundException(nameof(XZInit.Lib.LzmaStreamEncoderMtMemUsage));
        }

        /// <summary>
        /// Calculate approximate memory usage of multithreaded .xz encoder
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required for encoding with the given options. 
        /// If an error occurs, for example due to unsupported preset or filter chain, UINT64_MAX is returned.
        /// </returns>
        [Obsolete($"Renamed to [{nameof(ParallelEncoderMemUsage)}].")]
        public static ulong ThreadedEncoderMemUsage(XZCompressOptions compOpts, XZThreadedCompressOptions threadOpts)
        {
            return ParallelEncoderMemUsage(compOpts, threadOpts.ToParallel());
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
            XZInit.Manager.EnsureLoaded();

            if (XZInit.Lib == null)
                throw new ObjectDisposedException(nameof(XZInit));

            uint preset = XZCompressOptions.ToPreset(level, extremeFlag);
            return XZInit.Lib.LzmaEasyDecoderMemUsage?.Invoke(preset) ?? throw new EntryPointNotFoundException(nameof(XZInit.Lib.LzmaEasyDecoderMemUsage));
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
            XZInit.Manager.EnsureLoaded();

            if (XZInit.Lib == null)
                throw new ObjectDisposedException(nameof(XZInit));

            return XZInit.Lib.LzmaEasyDecoderMemUsage?.Invoke(compOpts.Preset) ?? throw new EntryPointNotFoundException(nameof(XZInit.Lib.LzmaEasyDecoderMemUsage));
        }
        #endregion
    }
}
