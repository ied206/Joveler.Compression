/*
    Derived from Zstandard header files (BSD 2-Clause)
    Copyright (c) 2016-present, Yann Collet, Facebook, Inc. All rights reserved.

    C# Wrapper written by Hajin Jang
    Copyright (C) 2020-2023 Hajin Jang

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
#if !NETFRAMEWORK
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
            VersionNumber = GetFuncPtr<ZSTD_versionNumber>(nameof(ZSTD_versionNumber));
            VersionString = GetFuncPtr<ZSTD_versionString>(nameof(ZSTD_versionString));
            #endregion

            #region Helper Functions
            CompressBound = GetFuncPtr<ZSTD_compressBound>(nameof(ZSTD_compressBound));
            IsError = GetFuncPtr<ZSTD_isError>(nameof(ZSTD_isError));
            GetErrorName = GetFuncPtr<ZSTD_getErrorName>(nameof(ZSTD_getErrorName));
            MinCLevel = GetFuncPtr<ZSTD_minCLevel>(nameof(ZSTD_minCLevel));
            MaxCLevel = GetFuncPtr<ZSTD_maxCLevel>(nameof(ZSTD_maxCLevel));
            DefaultCLevel = GetFuncPtr<ZSTD_defaultCLevel>(nameof(ZSTD_defaultCLevel));
            #endregion

            #region Compression Streaming
            CreateCStream = GetFuncPtr<ZSTD_createCStream>(nameof(ZSTD_createCStream));
            FreeCStream = GetFuncPtr<ZSTD_freeCStream>(nameof(ZSTD_freeCStream));
            CCtxReset = GetFuncPtr<ZSTD_CCtx_reset>(nameof(ZSTD_CCtx_reset));
            CompressStream2 = GetFuncPtr<ZSTD_compressStream2>(nameof(ZSTD_compressStream2));
            CStreamInSize = GetFuncPtr<ZSTD_CStreamInSize>(nameof(ZSTD_CStreamInSize));
            CStreamOutSize = GetFuncPtr<ZSTD_CStreamOutSize>(nameof(ZSTD_CStreamOutSize));
            GetCParams = GetFuncPtr<ZSTD_getCParams>(nameof(ZSTD_getCParams));
            CheckCParams = GetFuncPtr<ZSTD_checkCParams>(nameof(ZSTD_checkCParams));
            AdjustCParams = GetFuncPtr<ZSTD_adjustCParams>(nameof(ZSTD_adjustCParams));
            CParamGetBounds = GetFuncPtr<ZSTD_cParam_getBounds>(nameof(ZSTD_cParam_getBounds));
            CCtxSetParameter = GetFuncPtr<ZSTD_CCtx_setParameter>(nameof(ZSTD_CCtx_setParameter));
            CCtxSetPledgedSrcSize = GetFuncPtr<ZSTD_CCtx_setPledgedSrcSize>(nameof(ZSTD_CCtx_setPledgedSrcSize));
            #endregion

            #region Decompression Streaming
            CreateDStream = GetFuncPtr<ZSTD_createDStream>(nameof(ZSTD_createDStream));
            FreeDStream = GetFuncPtr<ZSTD_freeDStream>(nameof(ZSTD_freeDStream));
            DctxReset = GetFuncPtr<ZSTD_DCtx_reset>(nameof(ZSTD_DCtx_reset));
            DecompressStream = GetFuncPtr<ZSTD_decompressStream>(nameof(ZSTD_decompressStream));
            DStreamInSize = GetFuncPtr<ZSTD_DStreamInSize>(nameof(ZSTD_DStreamInSize));
            DStreamOutSize = GetFuncPtr<ZSTD_DStreamOutSize>(nameof(ZSTD_DStreamOutSize));
            DCtxSetParameter = GetFuncPtr<ZSTD_DCtx_setParameter>(nameof(ZSTD_DCtx_setParameter));
            #endregion

            #region Streaming Common
            ToFlushNow = GetFuncPtr<ZSTD_toFlushNow>(nameof(ZSTD_toFlushNow));
            #endregion

            #region Dictionary and Prefix API
            CctxLoadDictionary = GetFuncPtr<ZSTD_CCtx_loadDictionary>(nameof(ZSTD_CCtx_loadDictionary));
            DctxLoadDictionary = GetFuncPtr<ZSTD_DCtx_loadDictionary>(nameof(ZSTD_DCtx_loadDictionary));
            #endregion

            #region Memory Management
            SizeOfCStream = GetFuncPtr<ZSTD_sizeof_CStream>(nameof(ZSTD_sizeof_CStream));
            SizeOfDStream = GetFuncPtr<ZSTD_sizeof_DStream>(nameof(ZSTD_sizeof_DStream));
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
            CCtxReset = null;
            CompressStream2 = null;
            CStreamInSize = null;
            CStreamOutSize = null;
            GetCParams = null;
            CheckCParams = null;
            AdjustCParams = null;
            CParamGetBounds = null;
            CCtxSetParameter = null;
            CCtxSetPledgedSrcSize = null;
            #endregion

            #region Decompression Streaming
            CreateDStream = null;
            FreeDStream = null;
            DecompressStream = null;
            DStreamInSize = null;
            DStreamOutSize = null;
            DCtxSetParameter = null;
            DctxReset = null;
            #endregion

            #region Streaming Common
            ToFlushNow = null;
            #endregion

            #region Dictionary and Prefix API
            CctxLoadDictionary = null;
            DctxLoadDictionary = null;
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

        #region libzstd Function Pointers
        #region Version - VersionNumber, VersionString
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ZSTD_versionNumber();
        internal ZSTD_versionNumber VersionNumber;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ZSTD_versionString();
        internal ZSTD_versionString VersionString;
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_CCtx_reset(IntPtr cctx, ResetDirective reset);
        internal ZSTD_CCtx_reset CCtxReset;

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
        /// - note: if an operation ends with an error, it may leave @cctx in an undefined state.
        /// Therefore, it's UB to invoke ZSTD_compressStream2() of ZSTD_compressStream() on such a state.
        /// In order to be re-employed after an error, a state must be reset,
        /// which can be done explicitly(ZSTD_CCtx_reset()),
        /// or is sometimes implied by methods starting a new compression job(ZSTD_initCStream(), ZSTD_compressCCtx())
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
        internal ZSTD_compressStream2 CompressStream2;

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
        #endregion

        #region Decompression Streaming
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ZSTD_createDStream();
        internal ZSTD_createDStream CreateDStream;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_freeDStream(IntPtr zds);
        internal ZSTD_freeDStream FreeDStream;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_DCtx_reset(IntPtr dctx, ResetDirective reset);
        internal ZSTD_DCtx_reset DctxReset;

        /*
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_initDStream(IntPtr zds);
        internal ZSTD_initDStream InitDStream;
        */

        /// <summary>
        /// Streaming decompression function.
        /// Call repetitively to consume full input updating it as necessary.
        /// Function will update both input and output `pos` fields exposing current state via these fields:
        /// - `input.pos < input.size`, some input remaining and caller should provide remaining input
        ///    on the next call.
        /// - `output.pos < output.size`, decoder finished and flushed all remaining buffers.
        /// - `output.pos == output.size`, potentially uncflushed data present in the internal buffers,
        ///   call ZSTD_decompressStream() again to flush remaining data to output.
        /// Note : with no additional input, amount of data flushed <= ZSTD_BLOCKSIZE_MAX.
        /// </summary>
        /// <param name="zds"></param>
        /// <param name="output"></param>
        /// <param name="input"></param>
        /// <returns>
        /// 0 when a frame is completely decoded and fully flushed,
        /// or an error code, which can be tested using ZSTD_isError(),
        /// or any other value > 0, which means there is some decoding or flushing to do to complete current frame.0
        /// </returns>
        /// <remarks>
        /// Note: when an operation returns with an error code, the @zds state may be left in undefined state.
        ///       It's UB to invoke `ZSTD_decompressStream()` on such a state.
        ///       In order to re-use such a state, it must be first reset,
        ///       which can be done explicitly (`ZSTD_DCtx_reset()`),
        ///       or is implied for operations starting some new decompression job (`ZSTD_initDStream`, `ZSTD_decompressDCtx()`,
        /// </remarks>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_decompressStream(
            IntPtr zds,
            OutBuffer output,
            InBuffer input);
        internal ZSTD_decompressStream DecompressStream;

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

#if false
        /// <summary>
        /// Refuses allocating internal buffers for frames requiring a window size larger than provided limit.
        /// This protects a decoder context from reserving too much memory for itself (potential attack scenario).
        ///  This parameter is only useful in streaming mode, since no internal buffer is allocated in single-pass mode.
        ///  By default, a decompression context accepts all window sizes <= (1 << ZSTD_WINDOWLOG_LIMIT_DEFAULT)
        /// </summary>
        /// <returns>0, or an error code (which can be tested using ZSTD_isError()).</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_DCtx_setMaxWindowSize(IntPtr dctx, UIntPtr maxWindowSize);
        internal ZSTD_DCtx_setMaxWindowSize DCtxSetMaxWindowSize;
#endif
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

        #region Dictionary and Prefix API
        /// <summary>
        /// ZSTD_CCtx_loadDictionary() : Requires v1.4.0+
        /// Create an internal CDict from `dict` buffer.
        /// Decompression will have to use same dictionary.
        /// </summary>
        /// <remarks>
        /// Special: Loading a NULL (or 0-size) dictionary invalidates previous dictionary,
        ///          meaning "return to no-dictionary mode".
        /// Note 1 : Dictionary is sticky, it will be used for all future compressed frames.
        ///          To return to "no-dictionary" situation, load a NULL dictionary (or reset parameters).
        /// Note 2 : Loading a dictionary involves building tables.
        ///          It's also a CPU consuming operation, with non-negligible impact on latency.
        ///          Tables are dependent on compression parameters, and for this reason,
        ///          compression parameters can no longer be changed after loading a dictionary.
        /// Note 3 : `dict` content will be copied internally.
        ///          Use experimental ZSTD_CCtx_loadDictionary_byReference() to reference content instead.
        ///          In such a case, dictionary buffer must outlive its users.
        /// Note 4 : Use ZSTD_CCtx_loadDictionary_advanced()
        ///          to precisely select how dictionary content must be interpreted. */</remarks>
        /// <returns>0, or an error code (which can be tested with ZSTD_isError()).</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_CCtx_loadDictionary(IntPtr cctx, byte* dict, UIntPtr dictSize);
        internal ZSTD_CCtx_loadDictionary CctxLoadDictionary;

        /// <summary>
        /// Create an internal DDict from dict buffer,
        /// to be used to decompress next frames.
        /// The dictionary remains valid for all future frames, until explicitly invalidated.
        /// </summary>
        /// <remarks>
        /// Special : Adding a NULL (or 0-size) dictionary invalidates any previous dictionary,
        ///           meaning "return to no-dictionary mode".
        /// Note 1 : Loading a dictionary involves building tables,
        ///          which has a non-negligible impact on CPU usage and latency.
        ///          It's recommended to "load once, use many times", to amortize the cost
        /// Note 2 : `dict` content will be copied internally, so `dict` can be released after loading.
        ///          Use ZSTD_DCtx_loadDictionary_byReference() to reference dictionary content instead.
        /// Note 3 : Use ZSTD_DCtx_loadDictionary_advanced() to take control of
        ///          how dictionary content is loaded and interpreted.
        /// </remarks>
        /// <returns>0, or an error code (which can be tested with ZSTD_isError()).</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_DCtx_loadDictionary(IntPtr cctx, byte* dict, UIntPtr dictSize);
        internal ZSTD_DCtx_loadDictionary DctxLoadDictionary;
        #endregion

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
        /// ZSTD_estimateCStreamSize() will provide a memory budget large enough for streaming compression 
        /// using any compression level up to the max specified one.
        /// It will also consider src size to be arbitrarily "large", which is a worst case scenario.
        /// If srcSize is known to always be small, ZSTD_estimateCStreamSize_usingCParams() can provide a tighter estimation.
        /// </summary>
        /// <remarks>
        /// Note : CStream size estimation is only correct for single-threaded compression.
        /// Note 2 : ZSTD_estimateCStreamSize* functions are not compatible with the Block-Level Sequence Producer API at this time.
        /// Size estimates assume that no external sequence producer is registered.
        /// </remarks>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_estimateCStreamSize(int maxCompressionLevel);
        internal ZSTD_estimateCStreamSize EstimateCStreamSize;

        /// <summary>
        /// ZSTD_estimateCStreamSize_usingCParams() can be used in tandem with ZSTD_getCParams() to create cParams from compressionLevel.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_estimateCStreamSize_usingCParams(CompressionParameters compressionLevel);
        internal ZSTD_estimateCStreamSize_usingCParams EstimateCStreamSizeUsingCParams;

        /// <summary>
        /// ZSTD_DStream memory budget depends on frame's window Size.
        /// This information can be passed manually, using ZSTD_estimateDStreamSize,
        /// or deducted from a valid frame Header, using ZSTD_estimateDStreamSize_fromFrame();
        /// Any frame requesting a window size larger than max specified one will be rejected.
        /// </summary>
        /// <remarks>
        /// Note : if streaming is init with function ZSTD_init?Stream_usingDict(),
        /// an internal ?Dict will be created, which additional size is not estimated here.
        /// In this case, get total size by adding ZSTD_estimate?DictSize
        /// </remarks>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_estimateDStreamSize(UIntPtr maxWindowSize);
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
