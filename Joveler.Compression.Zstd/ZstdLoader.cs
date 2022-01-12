/*
    Derived from Zstandard header files (BSD 2-Clause)
    Copyright (c) 2016-present, Yann Collet, Facebook, Inc. All rights reserved.

    C# Wrapper written by Hajin Jang
    Copyright (C) 2020-2021 Hajin Jang

    Redistribution and use in source and binary forms, with or without modification,
    are permitted provided that the following conditions are met:

     * Redistributions of source code must retain the above copyright notice, this
       list of conditions and the following disclaimer.

     * Redistributions in binary form must reproduce the above copyright notice,
       this list of conditions and the following disclaimer in the documentation
       and/or other materials provided with the distribution.

     * Neither the name Facebook nor the names of its contributors may be used to
       endorse or promote products derived from this software without specific
       prior written permission.

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

using Joveler.DynLoader;
using System;
using System.Runtime.InteropServices;

namespace Joveler.Compression.Zstd
{
    public unsafe class ZstdLoader : DynLoaderBase
    {
        #region Constructor
        public ZstdLoader() : base() { }
        #endregion

        #region (override) DefaultLibFileName
        protected override string DefaultLibFileName
        {
            get
            {
#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "libzstd.so.1";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "libzstd.1.dylib";
#endif
                throw new PlatformNotSupportedException();
            }
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        protected override void LoadFunctions()
        {
            #region Version - VersionNumber, VersionString
            VersionNumber = GetFuncPtr<ZSTD_versionNumber>();
            VersionString = GetFuncPtr<ZSTD_versionString>();
            #endregion

            /*
            #region Simple API - SimpleCompress, SimpleDecompress
            SimpleCompress = GetFuncPtr<ZSTD_compress>();
            SimpleDecompress = GetFuncPtr<ZSTD_decompress>();
            #endregion
            */

            #region Helper Functions
            CompressBound = GetFuncPtr<ZSTD_compressBound>();
            IsError = GetFuncPtr<ZSTD_isError>();
            GetErrorName = GetFuncPtr<ZSTD_getErrorName>();
            MinCLevel = GetFuncPtr<ZSTD_minCLevel>();
            MaxCLevel = GetFuncPtr<ZSTD_maxCLevel>();
            DefaultCLevel = GetFuncPtr<ZSTD_defaultCLevel>();
            #endregion

            #region Compression Streaming
            CreateCStream = GetFuncPtr<ZSTD_createCStream>();
            FreeCStream = GetFuncPtr<ZSTD_freeCStream>();
            CompressionStream2 = GetFuncPtr<ZSTD_compressStream2>();
            CStreamInSize = GetFuncPtr<ZSTD_CStreamInSize>();
            CStreamOutSize = GetFuncPtr<ZSTD_CStreamOutSize>();
            GetCParams = GetFuncPtr<ZSTD_getCParams>();
            CheckCParams = GetFuncPtr<ZSTD_checkCParams>();
            AdjustCParams = GetFuncPtr<ZSTD_adjustCParams>();
            CParamGetBounds = GetFuncPtr<ZSTD_cParam_getBounds>();
            CCtxSetParameter = GetFuncPtr<ZSTD_CCtx_setParameter>();
            #endregion

            #region Decompression Streaming
            CreateDStream = GetFuncPtr<ZSTD_createDStream>();
            FreeDStream = GetFuncPtr<ZSTD_freeDStream>();
            DecompressionStream = GetFuncPtr<ZSTD_decompressStream>();
            DStreamInSize = GetFuncPtr<ZSTD_DStreamInSize>();
            DStreamOutSize = GetFuncPtr<ZSTD_DStreamOutSize>();
            #endregion

            #region Streaming Common
            ToFlushNow = GetFuncPtr<ZSTD_toFlushNow>();
            #endregion

            #region Memory Management
            SizeOfCStream = GetFuncPtr<ZSTD_sizeof_CStream>();
            SizeOfDStream = GetFuncPtr<ZSTD_sizeof_DStream>();
            EstimateCStreamSize = GetFuncPtr<ZSTD_estimateCStreamSize>();
            EstimateCStreamSizeUsingCParams = GetFuncPtr<ZSTD_estimateCStreamSize_usingCParams>();
            EstimateDStreamSize = GetFuncPtr<ZSTD_estimateDStreamSize>();
            EstimateDStreamSizeFromFrame = GetFuncPtr<ZSTD_estimateDStreamSize_fromFrame>();
            #endregion
        }

        protected override void ResetFunctions()
        {
            #region Version - VersionNumber, VersionString
            VersionNumber = null;
            VersionString = null;
            #endregion

            #region Helper Functions
            CompressBound = null;
            IsError = null;
            GetErrorName = null;
            MinCLevel = null;
            MaxCLevel = null;
            DefaultCLevel = null;
            #endregion

            #region Compression Streaming
            CreateCStream = null;
            FreeCStream = null;
            CompressionStream2 = null;
            CStreamInSize = null;
            CStreamOutSize = null;
            GetCParams = null;
            CheckCParams = null;
            AdjustCParams = null;
            CParamGetBounds = null;
            CCtxSetParameter = null;
            #endregion

            #region Decompression Streaming
            CreateDStream = null;
            FreeDStream = null;
            DecompressionStream = null;
            DStreamInSize = null;
            DStreamOutSize = null;
            #endregion

            #region Streaming Common
            ToFlushNow = null;
            #endregion

            #region Memory Management
            SizeOfCStream = null;
            SizeOfDStream = null;
            EstimateCStreamSize = null;
            EstimateCStreamSizeUsingCParams = null;
            EstimateDStreamSize = null;
            EstimateDStreamSizeFromFrame = null;
            #endregion
        }
        #endregion

        #region libzstd Function Pointer
        #region Version - VersionNumber, VersionString
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ZSTD_versionNumber();
        internal ZSTD_versionNumber VersionNumber;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ZSTD_versionString();
        internal ZSTD_versionString VersionString;
        #endregion

        #region Simple API - SimpleCompress, SimpleDecompress
        /// <summary>
        /// Compresses `src` content as a single zstd compressed frame into already allocated `dst`.
        /// Hint : compression runs faster if `dstCapacity` >=  `ZSTD_compressBound(srcSize)`.
        /// </summary>
        /// <returns>
        /// compressed size written into `dst` (<= `dstCapacity),
        /// or an error code if it fails (which can be tested using ZSTD_isError()).
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_compress(
            byte* dst, UIntPtr dstCapabity,
            byte* src, UIntPtr srcSize,
            int compressionLevel);
        internal ZSTD_compress SimpleCompress;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dstCapabity">
        /// an upper bound of originalSize to regenerate.
        /// If user cannot imply a maximum upper bound, it's better to use streaming mode to decompress data.
        /// </param>
        /// <param name="compressedSize">
        /// must be the _exact_ size of some number of compressed and/or skippable frames.
        /// </param>
        /// <returns>
        /// the number of bytes decompressed into `dst` (<= `dstCapacity`), or an errorCode if it fails (which can be tested using ZSTD_isError()).
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_decompress(
            byte* dst, UIntPtr dstCapabity,
            byte* src, UIntPtr compressedSize);
        internal ZSTD_decompress SimpleDecompress;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src">
        /// `src` should point to the start of a ZSTD encoded frame.
        /// </param>
        /// <param name="srcSize">
        /// `srcSize` must be at least as large as the frame header.
        /// hint : any size >= `ZSTD_frameHeaderSize_max` is large enough.
        /// </param>
        /// <remarks>
        /// note 1 : a 0 return value means the frame is valid but "empty".
        /// note 2 : decompressed size is an optional field, it may not be present, typically in streaming mode.
        ///          When `return==ZSTD_CONTENTSIZE_UNKNOWN`, data to decompress could be any size.
        ///          In which case, it's necessary to use streaming mode to decompress data.
        ///          Optionally, application can rely on some implicit limit,
        ///          as ZSTD_decompress() only needs an upper bound of decompressed size.
        ///          (For example, data could be necessarily cut into blocks <= 16 KB).
        /// note 3 : decompressed size is always present when compression is completed using single-pass functions,
        ///          such as ZSTD_compress(), ZSTD_compressCCtx() ZSTD_compress_usingDict() or ZSTD_compress_usingCDict().
        /// note 4 : decompressed size can be very large(64-bits value),
        ///          potentially larger than what local system can handle as a single memory segment.
        ///          In which case, it's necessary to use streaming mode to decompress data.
        /// note 5 : If source is untrusted, decompressed size could be wrong or intentionally modified.
        ///          Always ensure return value fits within application's authorized limits.
        ///          Each application can set its own limits.
        /// note 6 : This function replaces ZSTD_getDecompressedSize()
        /// </remarks>
        /// <returns>
        ///  - decompressed size of `src` frame content, if known
        ///  - ZSTD_CONTENTSIZE_UNKNOWN if the size cannot be determined
        ///  - ZSTD_CONTENTSIZE_ERROR if an error occurred(e.g.invalid magic number, srcSize too small)
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong ZSTD_getFrameContentSize(byte* src, UIntPtr srcSize);
        internal ZSTD_getFrameContentSize GetFrameContentSize;

        internal const long ContentSizeUnknown = -1;
        internal const long ContentSizeError = -2;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src">
        /// `src` should point to the start of a ZSTD frame or skippable frame.
        /// </param>
        /// <param name="srcSize">
        /// `srcSize` must be >= first frame size
        /// </param>
        /// <returns>
        /// the compressed size of the first frame starting at `src`,
        /// suitable to pass as `srcSize` to `ZSTD_decompress` or similar,
        /// or an error code if input is invalid
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_findFrameCompressedSize(byte* src, UIntPtr srcSize);
        internal ZSTD_findFrameCompressedSize FindFrameCompressedSize;
        #endregion

        #region Helper - CompressBound, IsError, GetErrorName, MinCLevel, MaxCLevel, DefaultCLevel
        /// <summary>
        /// maximum compressed size in worst case single-pass scenario
        /// </summary>
        /// <param name="srcSize"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_compressBound(UIntPtr srcSize); // size_t
        internal ZSTD_compressBound CompressBound;

        /// <summary>
        /// tells if a `size_t` function result is an error code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ZSTD_isError(UIntPtr code); // size_t
        internal ZSTD_isError IsError;

        /// <summary>
        /// provides readable string from an error code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ZSTD_getErrorName(UIntPtr code); // size_t
        internal ZSTD_getErrorName GetErrorName;

        /// <summary>
        /// minimum negative compression level allowed, requires v1.4.0+
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int ZSTD_minCLevel();
        internal ZSTD_minCLevel MinCLevel;

        /// <summary>
        /// maximum compression level available
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int ZSTD_maxCLevel();
        internal ZSTD_maxCLevel MaxCLevel;

        /// <summary>
        /// default compression level, specified by ZSTD_CLEVEL_DEFAULT, requires v1.5.0+
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int ZSTD_defaultCLevel();
        internal ZSTD_defaultCLevel DefaultCLevel;
        #endregion

        #region Compression Streaming
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ZSTD_createCStream();
        internal ZSTD_createCStream CreateCStream;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_freeCStream(IntPtr zcs);
        internal ZSTD_freeCStream FreeCStream;

        /// <summary>
        /// Behaves about the same as ZSTD_compressStream, with additional control on end directive.
        /// </summary>
        /// <remarks>
        /// - Compression parameters are pushed into CCtx before starting compression, using ZSTD_CCtx_set*()
        /// - Compression parameters cannot be changed once compression is started (save a list of exceptions in multi-threading mode)
        /// - output->pos must be <= dstCapacity, input->pos must be <= srcSize
        /// - output->pos and input->pos will be updated. They are guaranteed to remain below their respective limit.
        /// - endOp must be a valid directive
        /// - When nbWorkers==0 (default), function is blocking : it completes its job before returning to caller.
        /// - When nbWorkers>=1, function is non-blocking : it copies a portion of input, distributes jobs to internal worker threads, flush to output whatever is available,
        ///                                                 and then immediately returns, just indicating that there is some data remaining to be flushed.
        ///                                                 The function nonetheless guarantees forward progress : it will return only after it reads or write at least 1+ byte.
        /// - Exception : if the first call requests a ZSTD_e_end directive and provides enough dstCapacity, the function delegates to ZSTD_compress2() which is always blocking.
        /// - @return 
        ///           or an error code, which can be tested using ZSTD_isError().
        ///           if @return != 0, flush is not fully completed, there is still some data left within internal buffers.
        ///            This is useful for ZSTD_e_flush, since in this case more flushes are necessary to empty all buffers.
        ///           For ZSTD_e_end, @return == 0 when internal buffers are fully flushed and frame is completed.
        /// - after a ZSTD_e_end directive, if internal buffer is not fully flushed (@return != 0),
        ///           only ZSTD_e_end or ZSTD_e_flush operations are allowed.
        ///           Before starting a new compression job, or changing compression parameters,
        ///           it is required to fully flush internal buffers.
        /// </remarks>
        /// <param name="cctx"></param>
        /// <param name="output"></param>
        /// <param name="input"></param>
        /// <param name="endOp"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_compressStream2(
            IntPtr cctx,
            OutBuffer output,
            InBuffer input,
            EndDirective endOp);
        internal ZSTD_compressStream2 CompressionStream2;

        /// <summary>
        /// recommended size for input buffer
        /// </summary>
        /// <returns>
        /// However, note that these recommendations are from the perspective of a C caller program.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_CStreamInSize();
        internal ZSTD_CStreamInSize CStreamInSize;

        /// <summary>
        /// recommended size for output buffer. Guarantee to successfully flush at least one complete compressed block.
        /// </summary>
        /// <returns>
        /// However, note that these recommendations are from the perspective of a C caller program.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_CStreamOutSize();
        internal ZSTD_CStreamOutSize CStreamOutSize;

        /// <summary>
        /// Behave the same as ZSTD_compressCCtx(), but compression parameters are set using the advanced API.
        /// ZSTD_compress2() always starts a new frame.
        /// Should cctx hold data from a previously unfinished frame, everything about it is forgotten.
        /// - Compression parameters are pushed into CCtx before starting compression, using ZSTD_CCtx_set*()
        /// - The function is always blocking, returns when compression is completed.
        /// Hint : compression runs faster if `dstCapacity` >=  `ZSTD_compressBound(srcSize)`.
        /// </summary>
        /// <returns>
        /// compressed size written into `dst` (<= `dstCapacity),
        /// or an error code if it fails (which can be tested using ZSTD_isError()).
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_compress2(
            IntPtr cctx,
            byte* dst,
            UIntPtr dstCapacity, // size_t
            byte* src,
            UIntPtr srcSize // size_t
        );
        internal ZSTD_compress2 Compress2;

        /// <summary>
        /// Returns ZSTD_compressionParameters structure for a selected compression level and estimated srcSize.
        /// </summary>
        /// <param name="estimatedSrcSize">
        /// `estimatedSrcSize` value is optional, select 0 if not known
        /// </param>
        /// <param name="dictSize">
        /// `dictSize` must be `0` when there is no dictionary.
        /// </param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate CParameter ZSTD_getCParams(
            int compressionLevel,
            ulong estimatedSrcSize,
            UIntPtr dictSize // size_t
        );
        internal ZSTD_getCParams GetCParams;

        /// <summary>
        /// Ensure param values remain within authorized range.
        /// </summary>
        /// <returns>
        /// 0 on success, or an error code (can be checked with ZSTD_isError())
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_checkCParams(CParameter cParams);
        internal ZSTD_checkCParams CheckCParams;

        /// <summary>
        /// optimize params for a given `srcSize` and `dictSize`. This function never fails (wide contract)
        /// </summary>
        /// <param name="cParams">
        /// cPar can be invalid : all parameters will be clamped within valid range in the @return struct.
        /// </param>
        /// <param name="srcSize">
        /// `srcSize` can be unknown, in which case use ZSTD_CONTENTSIZE_UNKNOWN.
        /// </param>
        /// <param name="dictSize">
        /// `dictSize` must be `0` when there is no dictionary.
        /// </param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_adjustCParams(
            CParameter cParams,
            ulong srcSize,
            UIntPtr dictSize // size_t
        );
        internal ZSTD_adjustCParams AdjustCParams;

        /// <summary>
        /// All parameters must belong to an interval with lower and upper bounds,
        /// otherwise they will either trigger an error or be automatically clamped.
        /// </summary>
        /// <returns>
        /// a structure, ZSTD_bounds, which contains
        /// - an error status field, which must be tested using ZSTD_isError()
        /// - lower and upper bounds, both inclusive
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate Bounds ZSTD_cParam_getBounds(CParameter cParam);
        internal ZSTD_cParam_getBounds CParamGetBounds;

        /// <summary>
        /// Set one compression parameter, selected by enum ZSTD_cParameter.
        ///  All parameters have valid bounds.Bounds can be queried using ZSTD_cParam_getBounds().
        /// Providing a value beyond bound will either clamp it, or trigger an error(depending on parameter).
        /// Setting a parameter is generally only possible during frame initialization(before starting compression).
        /// Exception : when using multi-threading mode(nbWorkers >= 1),
        /// the following parameters can be updated _during_ compression(within same frame) :
        ///              => compressionLevel, hashLog, chainLog, searchLog, minMatch, targetLength and strategy.
        /// new parameters will be active for next job only(after a flush()).
        /// </summary>
        /// <returns>
        /// an error code (which can be tested using ZSTD_isError()).
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_CCtx_setParameter(IntPtr cctx, CParameter param, int value);
        internal ZSTD_CCtx_setParameter CCtxSetParameter;
        #endregion

        #region Decompression Streaming
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ZSTD_createDStream();
        internal ZSTD_createDStream CreateDStream;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_freeDStream(IntPtr zds);
        internal ZSTD_freeDStream FreeDStream;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_decompressStream(
            IntPtr zds,
            byte* output,
            byte* input);
        internal ZSTD_decompressStream DecompressionStream;

        /// <summary>
        /// recommended size for input buffer
        /// </summary>
        /// <returns>
        /// However, note that these recommendations are from the perspective of a C caller program.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_DStreamInSize();
        internal ZSTD_DStreamInSize DStreamInSize;

        /// <summary>
        /// recommended size for output buffer. Guarantee to successfully flush at least one complete block in all circumstances.
        /// </summary>
        /// <returns>
        /// However, note that these recommendations are from the perspective of a C caller program.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_DStreamOutSize();
        internal ZSTD_DStreamOutSize DStreamOutSize;
        #endregion

        #region Streaming Common
        /// <summary>
        /// Tell how many bytes are ready to be flushed immediately.
        /// Useful for multithreading scenarios (nbWorkers >= 1).
        /// Probe the oldest active job, defined as oldest job not yet entirely flushed,
        /// and check its output buffer.
        /// </summary>
        /// <return>
        /// amount of data stored in oldest job and ready to be flushed immediately.
        /// if @return == 0, it means either :
        /// + there is no active job (could be checked with ZSTD_frameProgression()), or
        /// + oldest job is still actively compressing data,
        ///   but everything it has produced has also been flushed so far,
        ///   therefore flush speed is limited by production speed of oldest job
        ///   irrespective of the speed of concurrent (and newer) jobs.
        /// </return>
        /// <param name="cctx"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_toFlushNow(IntPtr cctx);
        internal ZSTD_toFlushNow ToFlushNow;
        #endregion

        // TODO: Dictionary functions

        #region Memory Management
        /// <summary>
        /// These functions give the _current_ memory usage of selected object.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_sizeof_CStream(IntPtr zcs);
        internal ZSTD_sizeof_CStream SizeOfCStream;

        /// <summary>
        /// These functions give the _current_ memory usage of selected object.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_sizeof_DStream(IntPtr zds);
        internal ZSTD_sizeof_DStream SizeOfDStream;

        /// <summary>
        /// ZSTD_estimateCStreamSize() will provide a budget large enough for any compression level up to selected one.
        /// It will also consider src size to be arbitrarily "large", which is worst case.
        /// If srcSize is known to always be small, ZSTD_estimateCStreamSize_usingCParams() can provide a tighter estimation.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_estimateCStreamSize(int compressionLevel);
        internal ZSTD_estimateCStreamSize EstimateCStreamSize;

        /// <summary>
        /// ZSTD_estimateCStreamSize_usingCParams() can be used in tandem with ZSTD_getCParams() to create cParams from compressionLevel.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_estimateCStreamSize_usingCParams(CompressionParameters compressionLevel);
        internal ZSTD_estimateCStreamSize_usingCParams EstimateCStreamSizeUsingCParams;

        /// <summary>
        /// ZSTD_DStream memory budget depends on window Size.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_estimateDStreamSize(UIntPtr windowSize);
        internal ZSTD_estimateDStreamSize EstimateDStreamSize;

        /// <summary>
        /// ZSTD_DStream memory budget depends on window Size, deducted from a valid frame Header.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_estimateDStreamSize_fromFrame(IntPtr src, UIntPtr srcSize);
        internal ZSTD_estimateDStreamSize_fromFrame EstimateDStreamSizeFromFrame;
        #endregion

        // TODO: Frame size functions

        #endregion
    }
}

#region Compression, Decopression Context
/*
#region Compression Context
        /// <summary>
        /// When compressing many times, it is recommended to allocate a context just once, and re-use it for each successive compression operation.
        /// This will make workload friendlier for system's memory.
        /// Note : re-using context is just a speed / resource optimization.
        ///        It doesn't change the compression ratio, which remains identical.
        /// Note 2 : In multi-threaded environments,
        ///          use one different context per thread for parallel execution.
        /// </summary>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ZSTD_createCCtx();
        internal ZSTD_createCCtx CreateCCtx;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_freeCCtx(IntPtr cctx);
        internal ZSTD_freeCCtx FreeCCtx;

        /// <summary>
        /// Same as ZSTD_compress(), using an explicit ZSTD_CCtx.<br/>
        /// Important : in order to behave similarly to `ZSTD_compress()`,
        /// this function compresses at requested compression level,
        /// __ignoring any other parameter__.
        /// If any advanced parameter was set using the advanced API,
        /// they will all be reset.Only `compressionLevel` remains.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_compressCCtx( // size_t
            IntPtr cctx,
            byte* dst,
            UIntPtr dstCapacity, // size_t
            byte* src,
            UIntPtr srcSize, // size_t
            int compressionLevel);
        internal ZSTD_compressCCtx CompressCCtx;

        

        /// <summary>
        /// Total input data size to be compressed as a single frame.
        /// Value will be written in frame header, unless if explicitly forbidden using ZSTD_c_contentSizeFlag.
        /// This value will also be controlled at end of frame, and trigger an error if not respected.
        /// </summary>
        /// <returns>
        /// 0, or an error code (which can be tested with ZSTD_isError()).
        /// Note 1 : pledgedSrcSize==0 actually means zero, aka an empty frame.
        ///          In order to mean "unknown content size", pass constant ZSTD_CONTENTSIZE_UNKNOWN.
        ///          ZSTD_CONTENTSIZE_UNKNOWN is default value for any new frame.
        /// Note 2 : pledgedSrcSize is only valid once, for the next frame.
        ///          It's discarded at the end of the frame, and replaced by ZSTD_CONTENTSIZE_UNKNOWN.
        /// Note 3 : Whenever all input data is provided and consumed in a single round,
        ///          for example with ZSTD_compress2(),
        ///          or invoking immediately ZSTD_compressStream2(,,,ZSTD_e_end),
        ///          this value is automatically overridden by srcSize instead.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_CCtx_setPledgedSrcSize(IntPtr cctx, ulong pledgedSrcSize);
        internal ZSTD_CCtx_setPledgedSrcSize CCtxSetPledgedSrcSize;

        /// <summary>
        /// There are 2 different things that can be reset, independently or jointly :
        /// - The session : will stop compressing current frame, and make CCtx ready to start a new one.
        ///                 Useful after an error, or to interrupt any ongoing compression.
        ///                 Any internal data not yet flushed is cancelled.
        ///                 Compression parameters and dictionary remain unchanged.
        ///                 They will be used to compress next frame.
        ///                 Resetting session never fails.
        /// - The parameters : changes all parameters back to "default".
        ///                    This removes any reference to any dictionary too.
        ///                    Parameters can only be changed between 2 sessions (i.e.no compression is currently ongoing)
        ///                    otherwise the reset fails, and function returns an error value(which can be tested using ZSTD_isError())
        /// - Both : similar to resetting the session, followed by resetting parameters.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_CCtx_reset(IntPtr cctx, ResetDirective reset);
        internal ZSTD_CCtx_reset CCtxReset;

        /// <summary>
        /// Behave the same as ZSTD_compressCCtx(), but compression parameters are set using the advanced API.
        /// ZSTD_compress2() always starts a new frame.
        /// Should cctx hold data from a previously unfinished frame, everything about it is forgotten.
        /// - Compression parameters are pushed into CCtx before starting compression, using ZSTD_CCtx_set*()
        /// - The function is always blocking, returns when compression is completed.
        /// Hint : compression runs faster if `dstCapacity` >=  `ZSTD_compressBound(srcSize)`.
        /// </summary>
        /// <returns>
        /// compressed size written into `dst` (<= `dstCapacity),
        /// or an error code if it fails (which can be tested using ZSTD_isError()).
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_compress2(
            IntPtr cctx,
            byte* dst,
            UIntPtr dstCapacity, // size_t
            byte* src,
            UIntPtr srcSize // size_t
        );
        internal ZSTD_compress2 Compress2;
        #endregion

        #region Decompression Context
        /// <summary>
        /// When decompressing many times, it is recommended to allocate a context only once, and re-use it for each successive compression operation.
        /// This will make workload friendlier for system's memory.
        /// Use one context per thread for parallel execution.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ZSTD_createDCtx();
        internal ZSTD_createDCtx CreateDCtx;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_freeDCtx(IntPtr dctx);
        internal ZSTD_freeDCtx FreeDCtx;

        /// <summary>
        /// All parameters must belong to an interval with lower and upper bounds,
        /// otherwise they will either trigger an error or be automatically clamped.
        /// </summary>
        /// <returns>
        /// a structure, ZSTD_bounds, which contains
        /// - an error status field, which must be tested using ZSTD_isError()
        /// - both lower and upper bounds, inclusive
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate Bounds ZSTD_dParam_getBounds(DParameter dParam);
        internal ZSTD_dParam_getBounds DParamGetBounds;

        /// <summary>
        /// Set one compression parameter, selected by enum ZSTD_dParameter.
        /// All parameters have valid bounds. Bounds can be queried using ZSTD_dParam_getBounds().
        /// Providing a value beyond bound will either clamp it, or trigger an error (depending on parameter).
        /// Setting a parameter is only possible during frame initialization (before starting decompression).
        /// </summary>
        /// <returns>
        /// 0, or an error code (which can be tested using ZSTD_isError()).
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_DCtx_setParameter(IntPtr dctx, DParameter param, int value);
        internal ZSTD_DCtx_setParameter DCtxSetParameter;

        /// <summary>
        /// Return a DCtx to clean state.
        /// Session and parameters can be reset jointly or separately.
        /// Parameters can only be reset when no active frame is being decompressed.
        /// </summary>
        /// <returns>
        /// 0, or an error code, which can be tested with ZSTD_isError()
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_DCtx_reset(IntPtr dctx, ResetDirective reset);
        internal ZSTD_DCtx_reset DCtxReset;
        #endregion
*/
#endregion
