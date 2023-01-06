using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joveler.Compression.XZ
{
    public class XZMemory
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

            uint preset = XZCompressOptions.ToPreset(level, extremeFlag);
            return XZInit.Lib.LzmaEasyEncoderMemUsage(preset);
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

            return XZInit.Lib.LzmaEasyEncoderMemUsage(compOpts.Preset);
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

            uint preset = XZCompressOptions.ToPreset(level, extremeFlag);
            LzmaMt mtOpts = LzmaMt.Create(preset, threads);
            return XZInit.Lib.LzmaStreamEncoderMtMemUsage(mtOpts);
        }

        /// <summary>
        /// Calculate approximate memory usage of multithreaded .xz encoder
        /// </summary>
        /// <returns>
        /// Number of bytes of memory required for encoding with the given options. 
        /// If an error occurs, for example due to unsupported preset or filter chain, UINT64_MAX is returned.
        /// </returns>
        public static ulong ThreadedEncoderMemUsage(XZCompressOptions compOpts, XZThreadedCompressOptions threadOpts)
        {
            XZInit.Manager.EnsureLoaded();

            LzmaMt mtOpts = compOpts.ToLzmaMt(threadOpts);
            return XZInit.Lib.LzmaStreamEncoderMtMemUsage(mtOpts);
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

            uint preset = XZCompressOptions.ToPreset(level, extremeFlag);
            return XZInit.Lib.LzmaEasyDecoderMemUsage(preset);
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

            return XZInit.Lib.LzmaEasyDecoderMemUsage(compOpts.Preset);
        }
        #endregion
    }
}
