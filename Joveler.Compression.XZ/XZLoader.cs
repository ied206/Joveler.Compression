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

using Joveler.DynLoader;
using System;
using System.Runtime.InteropServices;

namespace Joveler.Compression.XZ
{
    internal class XZLoader : DynLoaderBase
    {
        #region Constructor
        public XZLoader() : base() { }
        #endregion

        #region (override) DefaultLibFileName
        protected override string DefaultLibFileName
        {
            get
            {
#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "liblzma.so.5";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "liblzma.dylib";
#endif
                throw new PlatformNotSupportedException();
            }
        }
        #endregion

        #region (override) LoadFunctions, ResetFunctions
        protected override void LoadFunctions()
        {
            #region Base - LzmaCode, LzmaEnd, LzmaGetProgress
            LzmaCode = GetFuncPtr<lzma_code>(nameof(lzma_code));
            LzmaEnd = GetFuncPtr<lzma_end>(nameof(lzma_end));
            LzmaGetProgress = GetFuncPtr<lzma_get_progress>(nameof(lzma_get_progress));
            #endregion

            #region Container - Encoders and Decoders
            LzmaEasyEncoderMemUsage = GetFuncPtr<lzma_easy_encoder_memusage>(nameof(lzma_easy_encoder_memusage));
            LzmaEasyDecoderMemUsage = GetFuncPtr<lzma_easy_decoder_memusage>(nameof(lzma_easy_decoder_memusage));
            LzmaEasyEncoder = GetFuncPtr<lzma_easy_encoder>(nameof(lzma_easy_encoder));
            LzmaStreamEncoder = GetFuncPtr<lzma_stream_encoder>(nameof(lzma_stream_encoder));
            LzmaStreamEncoderMtMemUsage = GetFuncPtr<lzma_stream_encoder_mt_memusage>(nameof(lzma_stream_encoder_mt_memusage));
            LzmaStreamEncoderMt = GetFuncPtr<lzma_stream_encoder_mt>(nameof(lzma_stream_encoder_mt));
            LzmaStreamDecoder = GetFuncPtr<lzma_stream_decoder>(nameof(lzma_stream_decoder));
            #endregion

            #region Hardware - PhyMem & CPU Threads
            LzmaPhysMem = GetFuncPtr<lzma_physmem>(nameof(lzma_physmem));
            LzmaCpuThreads = GetFuncPtr<lzma_cputhreads>(nameof(lzma_cputhreads));
            #endregion

            #region Check - Crc32, Crc64
            LzmaCrc32 = GetFuncPtr<lzma_crc32>(nameof(lzma_crc32));
            LzmaCrc64 = GetFuncPtr<lzma_crc64>(nameof(lzma_crc64));
            #endregion

            #region Version - LzmaVersionNumber, LzmaVersionString
            LzmaVersionNumber = GetFuncPtr<lzma_version_number>(nameof(lzma_version_number));
            LzmaVersionString = GetFuncPtr<lzma_version_string>(nameof(lzma_version_string));
            #endregion
        }

        protected override void ResetFunctions()
        {
            #region Base - LzmaCode, LzmaEnd, LzmaGetProgress
            LzmaCode = null;
            LzmaEnd = null;
            LzmaGetProgress = null;
            #endregion

            #region Container - Encoders and Decoders
            LzmaEasyEncoder = null;
            LzmaStreamEncoder = null;
            LzmaStreamEncoderMt = null;
            LzmaStreamDecoder = null;
            #endregion

            #region Hardware - PhyMem & CPU Threads
            LzmaPhysMem = null;
            LzmaCpuThreads = null;
            #endregion

            #region Check - Crc32, Crc64
            LzmaCrc32 = null;
            LzmaCrc64 = null;
            #endregion

            #region Version - LzmaVersionNumber, LzmaVersionString
            LzmaVersionNumber = null;
            LzmaVersionString = null;
            #endregion
        }
        #endregion

        #region liblzma Function Pointer
        #region Base - LzmaCode, LzmaEnd, LzmaGetProgress
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_code(
            LzmaStream strm,
            LzmaAction action);
        internal lzma_code LzmaCode;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void lzma_end(LzmaStream strm);
        internal lzma_end LzmaEnd;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void lzma_get_progress(
            LzmaStream strm,
            ref ulong progress_in,
            ref ulong progress_out);
        internal lzma_get_progress LzmaGetProgress;
        #endregion

        #region Container - Encoders and Decoders
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_easy_encoder_memusage(uint preset);
        internal lzma_easy_encoder_memusage LzmaEasyEncoderMemUsage;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_easy_decoder_memusage(uint preset);
        internal lzma_easy_decoder_memusage LzmaEasyDecoderMemUsage;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_easy_encoder(
            LzmaStream strm,
            uint preset,
            LzmaCheck check);
        internal lzma_easy_encoder LzmaEasyEncoder;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_stream_encoder(
            LzmaStream strm,
            [MarshalAs(UnmanagedType.LPArray)] LzmaFilter[] filters,
            LzmaCheck check);
        internal lzma_stream_encoder LzmaStreamEncoder;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_stream_encoder_mt_memusage(LzmaMt options);
        internal lzma_stream_encoder_mt_memusage LzmaStreamEncoderMtMemUsage;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_stream_encoder_mt(
            LzmaStream strm,
            LzmaMt options);
        internal lzma_stream_encoder_mt LzmaStreamEncoderMt;

        /// <summary>
        /// Initialize .xz Stream decoder
        /// </summary>
        /// <param name="strm">Pointer to properly prepared lzma_stream</param>
        /// <param name="memlimit">
        /// Memory usage limit as bytes.
        /// Use UINT64_MAX to effectively disable the limiter.
        /// </param>
        /// <param name="flags">
        /// Bitwise-or of zero or more of the decoder flags
        /// </param>
        /// <returns>
        /// LZMA_OK: Initialization was successful.
        /// LZMA_MEM_ERROR: Cannot allocate memory.
        /// LZMA_OPTIONS_ERROR: Unsupported flags
        /// LZMA_PROG_ERROR
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_stream_decoder(
            LzmaStream strm,
            ulong memlimit,
            LzmaDecodingFlag flags);
        internal lzma_stream_decoder LzmaStreamDecoder;
        #endregion

        #region Hardware - PhyMem & CPU Threads
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_physmem();
        internal lzma_physmem LzmaPhysMem;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint lzma_cputhreads();
        internal lzma_cputhreads LzmaCpuThreads;
        #endregion

        #region Check - Crc32, Crc64
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate uint lzma_crc32(
            byte* buf,
            UIntPtr size, // size_t
            uint crc);
        internal lzma_crc32 LzmaCrc32;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate ulong lzma_crc64(
            byte* buf,
            UIntPtr size, // size_t
            ulong crc);
        internal lzma_crc64 LzmaCrc64;
        #endregion

        #region Version - LzmaVersionNumber, LzmaVersionString
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint lzma_version_number();
        internal lzma_version_number LzmaVersionNumber;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr lzma_version_string();
        internal lzma_version_string LzmaVersionString;
        #endregion
        #endregion
    }
}
