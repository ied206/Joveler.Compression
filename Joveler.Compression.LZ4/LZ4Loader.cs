/*
    Derived from LZ4 header files (BSD 2-Clause)
    Copyright (c) 2011-2016, Yann Collet

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-present Hajin Jang

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

using Joveler.DynLoader;
using System;
using System.Runtime.InteropServices;

namespace Joveler.Compression.LZ4
{
    internal unsafe sealed class LZ4Loader : DynLoaderBase
    {
        #region Constructor
        public LZ4Loader() : base() { }
        #endregion

        #region (override) DefaultLibFileName
        protected override string DefaultLibFileName
        {
            get
            {
#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "liblz4.so.1";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "liblz4.dylib";
#endif
                throw new PlatformNotSupportedException();
            }
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        protected override void LoadFunctions()
        {
            #region Version - LzmaVersionNumber, LzmaVersionString
            VersionNumber = GetFuncPtr<LZ4_versionNumber>(nameof(LZ4_versionNumber));
            VersionString = GetFuncPtr<LZ4_versionString>(nameof(LZ4_versionString));
            GetFrameVersion = GetFuncPtr<LZ4F_getVersion>(nameof(LZ4F_getVersion));
            #endregion

            #region Error - IsError, GetErrorName
            FrameIsError = GetFuncPtr<LZ4F_isError>(nameof(LZ4F_isError));
            GetErrorName = GetFuncPtr<LZ4F_getErrorName>(nameof(LZ4F_getErrorName));
            #endregion

            #region FrameCompression
            CreateFrameCompressContext = GetFuncPtr<LZ4F_createCompressionContext>(nameof(LZ4F_createCompressionContext));
            FreeFrameCompressContext = GetFuncPtr<LZ4F_freeCompressionContext>(nameof(LZ4F_freeCompressionContext));
            FrameCompressBegin = GetFuncPtr<LZ4F_compressBegin>(nameof(LZ4F_compressBegin));
            FrameCompressBeginUsingDict = GetFuncPtr<LZ4F_compressBegin_usingDict>(nameof(LZ4F_compressBegin_usingDict));
            FrameCompressBound = GetFuncPtr<LZ4F_compressBound>(nameof(LZ4F_compressBound));
            FrameCompressUpdate = GetFuncPtr<LZ4F_compressUpdate>(nameof(LZ4F_compressUpdate));
            FrameFlush = GetFuncPtr<LZ4F_flush>(nameof(LZ4F_flush));
            FrameCompressEnd = GetFuncPtr<LZ4F_compressEnd>(nameof(LZ4F_compressEnd));
            #endregion

            #region FrameDecompression
            CreateFrameDecompressContext = GetFuncPtr<LZ4F_createDecompressionContext>(nameof(LZ4F_createDecompressionContext));
            FreeFrameDecompressContext = GetFuncPtr<LZ4F_freeDecompressionContext>(nameof(LZ4F_freeDecompressionContext));
            GetFrameInfo = GetFuncPtr<LZ4F_getFrameInfo>(nameof(LZ4F_getFrameInfo));
            FrameDecompress = GetFuncPtr<LZ4F_decompress>(nameof(LZ4F_decompress));
            ResetDecompressContext = GetFuncPtr<LZ4F_resetDecompressionContext>(nameof(LZ4F_resetDecompressionContext));
            #endregion

            #region CDict - Bulk processing dictionary compression
            FrameCreateCDict = GetFuncPtr<LZ4F_createCDict>(nameof(LZ4F_createCDict));
            FrameFreeCDict = GetFuncPtr<LZ4F_freeCDict>(nameof(LZ4F_freeCDict));
            FrameCompressBeginUsingCDict = GetFuncPtr<LZ4F_compressBegin_usingCDict>(nameof(LZ4F_compressBegin_usingCDict));
            #endregion

            #region xxHash
            XXHVersionNumber = GetFuncPtr<LZ4_XXH_versionNumber>(nameof(LZ4_XXH_versionNumber));

            XXH32 = GetFuncPtr<LZ4_XXH32>(nameof(LZ4_XXH32));
            XXH32CreateState = GetFuncPtr<LZ4_XXH32_createState>(nameof(LZ4_XXH32_createState));
            XXH32FreeState = GetFuncPtr<LZ4_XXH32_freeState>(nameof(LZ4_XXH32_freeState));
            XXH32CopyState = GetFuncPtr<LZ4_XXH32_copyState>(nameof(LZ4_XXH32_copyState));
            XXH32Reset = GetFuncPtr<LZ4_XXH32_reset>(nameof(LZ4_XXH32_reset));
            XXH32Update = GetFuncPtr<LZ4_XXH32_update>(nameof(LZ4_XXH32_update));
            XXH32Digest = GetFuncPtr<LZ4_XXH32_digest>(nameof(LZ4_XXH32_digest));

            XXH64 = GetFuncPtr<LZ4_XXH64>(nameof(LZ4_XXH64));
            XXH64CreateState = GetFuncPtr<LZ4_XXH64_createState>(nameof(LZ4_XXH64_createState));
            XXH64FreeState = GetFuncPtr<LZ4_XXH64_freeState>(nameof(LZ4_XXH64_freeState));
            XXH64CopyState = GetFuncPtr<LZ4_XXH64_copyState>(nameof(LZ4_XXH64_copyState));
            XXH64Reset = GetFuncPtr<LZ4_XXH64_reset>(nameof(LZ4_XXH64_reset));
            XXH64Update = GetFuncPtr<LZ4_XXH64_update>(nameof(LZ4_XXH64_update));
            XXH64Digest = GetFuncPtr<LZ4_XXH64_digest>(nameof(LZ4_XXH64_digest));
            #endregion
        }

        protected override void ResetFunctions()
        {
            #region Version - LZ4VersionNumber, LZ4VersionString
            VersionNumber = null;
            VersionString = null;
            GetFrameVersion = null;
            #endregion

            #region Error - IsError, GetErrorName
            FrameIsError = null;
            GetErrorName = null;
            #endregion

            #region FrameCompression
            CreateFrameCompressContext = null;
            FreeFrameCompressContext = null;
            FrameCompressBegin = null;
            FrameCompressBeginUsingDict = null;
            FrameCompressBound = null;
            FrameCompressUpdate = null;
            FrameFlush = null;
            FrameCompressEnd = null;
            #endregion

            #region FrameDecompression
            CreateFrameDecompressContext = null;
            FreeFrameDecompressContext = null;
            GetFrameInfo = null;
            FrameDecompress = null;
            ResetDecompressContext = null;
            #endregion

            #region xxHash
            XXHVersionNumber = null;

            XXH32 = null;
            XXH32CreateState = null;
            XXH32FreeState = null;
            XXH32CopyState = null;
            XXH32Reset = null;
            XXH32Update = null;
            XXH32Digest = null;

            XXH64 = null;
            XXH64CreateState = null;
            XXH64FreeState = null; ;
            XXH64CopyState = null;
            XXH64Reset = null;
            XXH64Update = null;
            XXH64Digest = null;
            #endregion
        }
        #endregion

        #region liblz4 Function Pointer
        #region Version - VersionNumber, VersionString
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint LZ4_versionNumber();
        internal LZ4_versionNumber? VersionNumber;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr LZ4_versionString();
        internal LZ4_versionString? VersionString;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint LZ4F_getVersion();
        internal LZ4F_getVersion? GetFrameVersion;
        #endregion

        #region Error - IsError, GetErrorName
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint LZ4F_isError(nuint code); // size_t
        internal LZ4F_isError? FrameIsError;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr LZ4F_getErrorName(nuint code); // size_t
        internal LZ4F_getErrorName? GetErrorName;
        #endregion

        #region FrameCompress
        /// <summary>
        /// The first thing to do is to create a compressionContext object,
        /// which will keep track of operation state during streaming compression.
        /// This is achieved using LZ4F_createCompressionContext(), which takes as argument a version,
        /// and a pointer to LZ4F_cctx*, to write the resulting pointer into.
        /// </summary>
        /// <param name="cctxPtr">
        /// MUST be != NULL.
        /// </param>
        /// <param name="version">
        /// provided MUST be LZ4F_VERSION. It is intended to track potential version mismatch, notably when using DLL.
        /// The function provides a pointer to a fully allocated LZ4F_cctx object.
        /// </param>
        /// <returns>
        /// If @return != zero, context creation failed.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_createCompressionContext(
            ref IntPtr cctxPtr,
            uint version);
        internal LZ4F_createCompressionContext? CreateFrameCompressContext;

        /// <summary>
        /// A created compression context can be employed multiple times for consecutive streaming operations.
        /// Once all streaming compression jobs are completed,
        /// the state object can be released using LZ4F_freeCompressionContext().
        /// </summary>
        /// <remarks>
        /// Note1 : LZ4F_freeCompressionContext() is always successful. Its return value can be ignored.
        /// Note2 : LZ4F_freeCompressionContext() works fine with NULL input pointers (do nothing).
        /// </remarks>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_freeCompressionContext(IntPtr cctx);
        internal LZ4F_freeCompressionContext? FreeFrameCompressContext;

        /// <summary>
        ///  will write the frame header into dstBuffer.
        ///  dstCapacity must be >= LZ4F_HEADER_SIZE_MAX bytes.
        /// `prefsPtr` is optional : you can provide NULL as argument, all preferences will then be set to default.
        /// </summary>
        /// <returns>
        /// number of bytes written into dstBuffer for the header
        /// or an error code (which can be tested using LZ4F_isError())
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_compressBegin(
            IntPtr cctx,
            byte* dstBuffer,
            nuint dstCapacity,
            FramePreferences prefsPtr);
        internal LZ4F_compressBegin? FrameCompressBegin;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_compressBegin_usingDict(
            IntPtr cctx,
            byte* dstBuffer,
            nuint dstCapacity,
            byte* dictBuffer,
            nuint dictSize,
            FramePreferences prefsPtr);
        internal LZ4F_compressBegin_usingDict? FrameCompressBeginUsingDict;

        /// <summary>
        /// Provides minimum dstCapacity required to guarantee success of
        /// LZ4F_compressUpdate(), given a srcSize and preferences, for a worst case scenario.
        /// When srcSize==0, LZ4F_compressBound() provides an upper bound for LZ4F_flush() and LZ4F_compressEnd() instead.
        /// Note that the result is only valid for a single invocation of LZ4F_compressUpdate().
        /// When invoking LZ4F_compressUpdate() multiple times,
        /// if the output buffer is gradually filled up instead of emptied and re-used from its start,
        /// one must check if there is enough remaining capacity before each invocation, using LZ4F_compressBound().
        /// @return is always the same for a srcSize and prefsPtr.
        /// </summary>
        /// <param name="prefsPtr">when NULL is provided, preferences will be set to cover worst case scenario</param>
        /// <remarks>
        /// @return if automatic flushing is not enabled, includes the possibility that internal buffer might already be filled by up to(blockSize-1) bytes.
        /// It also includes frame footer(ending + checksum), since it might be generated by LZ4F_compressEnd().
        /// @return doesn't include frame header, as it was already generated by LZ4F_compressBegin().
        /// </remarks>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_compressBound(
            nuint srcSize, // size_t
            FramePreferences prefsPtr);
        internal LZ4F_compressBound? FrameCompressBound;

        /// <summary>
        /// LZ4F_compressUpdate() can be called repetitively to compress as much data as necessary.
        /// </summary>
        /// <remarks>
        /// Important rule: dstCapacity MUST be large enough to ensure operation success even in worst case situations.
        /// This value is provided by LZ4F_compressBound().
        /// If this condition is not respected, LZ4F_compress() will fail (result is an errorCode).
        /// After an error, the state is left in a UB state, and must be re-initialized or freed.
        /// If previously an uncompressed block was written, buffered data is flushed
        /// before appending compressed data is continued.
        /// </remarks>
        /// <param name="cOptPtr">
        /// is optional : NULL can be provided, in which case all options are set to default.
        /// </param>
        /// <return>
        /// number of bytes written into `dstBuffer` (it can be zero, meaning input data was just buffered).
        /// or an error code if it fails (which can be tested using LZ4F_isError())
        /// </return>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_compressUpdate(
            IntPtr cctx,
            byte* dstBuffer,
            nuint dstCapacity, // size_t
            byte* srcBuffer,
            nuint srcSize, // size_t
            FrameCompressOptions cOptPtr);
        internal LZ4F_compressUpdate? FrameCompressUpdate;

        /// <summary>
        ///  When data must be generated and sent immediately, without waiting for a block to be completely filled,
        ///  it's possible to call LZ4_flush(). It will immediately compress any data buffered within cctx.
        /// `dstCapacity` must be large enough to ensure the operation will be successful.
        /// `cOptPtr` is optional : it's possible to provide NULL, all options will be set to default.
        /// </summary>
        /// <remarks>
        /// LZ4F_flush() is guaranteed to be successful when dstCapacity >= LZ4F_compressBound(0, prefsPtr).
        /// </remarks>
        /// <return>
        /// number of bytes written into dstBuffer (it can be zero, which means there was no data stored within cctx)
        /// or an error code if it fails (which can be tested using LZ4F_isError())
        /// </return>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_flush(
            IntPtr cctx,
            byte* dstBuffer,
            nuint dstCapacity, // size_t
            FrameCompressOptions cOptPtr);
        internal LZ4F_flush? FrameFlush;

        /// <summary>
        ///  To properly finish an LZ4 frame, invoke LZ4F_compressEnd().
        ///  It will flush whatever data remained within `cctx` (like LZ4_flush())
        ///  and properly finalize the frame, with an endMark and a checksum.
        /// `cOptPtr` is optional : NULL can be provided, in which case all options will be set to default.
        /// </summary>
        /// <remarks>
        /// LZ4F_compressEnd() is guaranteed to be successful when dstCapacity >= LZ4F_compressBound(0, prefsPtr).
        /// </remarks>
        /// <return>
        /// number of bytes written into dstBuffer (necessarily >= 4 (endMark), or 8 if optional frame checksum is enabled)
        /// or an error code if it fails (which can be tested using LZ4F_isError())
        /// A successful call to LZ4F_compressEnd() makes `cctx` available again for another compression task.
        /// </return>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_compressEnd(
            IntPtr cctx,
            byte* dstBuffer,
            nuint dstCapacity, // size_t
            FrameCompressOptions cOptPtr);
        internal LZ4F_compressEnd? FrameCompressEnd;
        #endregion

        #region FrameDecompress
        /// <summary>
        /// Create an LZ4F_dctx object, to track all decompression operations.
        /// </summary>
        /// <param name="cctxPtr">
        /// MUST be valid.
        /// </param>
        /// <param name="version">
        /// MUST be LZ4F_VERSION.
        /// </param>
        /// <returns>
        /// The @return is an errorCode, which can be tested using LZ4F_isError().
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_createDecompressionContext(
            ref IntPtr cctxPtr,
            uint version);
        internal LZ4F_createDecompressionContext? CreateFrameDecompressContext;

        /// <summary>
        /// dctx memory can be released using LZ4F_freeDecompressionContext();
        /// </summary>
        /// <param name="dctx"></param>
        /// <returns>
        /// Result of LZ4F_freeDecompressionContext() indicates current state of decompressionContext when being released.
        /// That is, it should be == 0 if decompression has been completed fully and correctly.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_freeDecompressionContext(IntPtr dctx);
        internal LZ4F_freeDecompressionContext? FreeFrameDecompressContext;

        /*
        /// <summary>
        /// Provide the header size of a frame starting at `src`.
        /// `srcSize` must be >= LZ4F_MIN_SIZE_TO_KNOW_HEADER_LENGTH, which is enough to decode the header length.
        /// Frame header size is variable, but is guaranteed to be >= LZ4F_HEADER_SIZE_MIN bytes, and <= LZ4F_HEADER_SIZE_MAX bytes.
        /// </summary>
        /// <returns>
        /// size of frame header
        /// or an error code, which can be tested using LZ4F_isError()
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_headerSize(IntPtr src, nuint srcSize);
        internal LZ4F_headerSize FrameHeaderSize;
        */

        /// <summary>
        /// This function extracts frame parameters (max blockSize, dictID, etc.).
        /// </summary>
        /// <remarks>
        /// Its usage is optional: user can also invoke LZ4F_decompress() directly.
        ///
        /// Extracted information will fill an existing LZ4F_frameInfo_t structure.
        /// This can be useful for allocation and dictionary identification purposes.
        ///
        /// LZ4F_getFrameInfo() can work in the following situations :
        ///
        ///  1) At the beginning of a new frame, before any invocation of LZ4F_decompress().
        ///     It will decode header from `srcBuffer`,
        ///     consuming the header and starting the decoding process.
        ///
        ///     Input size must be large enough to contain the full frame header.
        ///     Frame header size can be known beforehand by LZ4F_headerSize().
        ///     Frame header size is variable, but is guaranteed to be >= LZ4F_HEADER_SIZE_MIN bytes,
        ///     and not more than &lt;= LZ4F_HEADER_SIZE_MAX bytes.
        ///     Hence, blindly providing LZ4F_HEADER_SIZE_MAX bytes or more will always work.
        ///     It's allowed to provide more input data than the header size,
        ///     LZ4F_getFrameInfo() will only consume the header.
        ///
        ///     If input size is not large enough,
        ///     aka if it's smaller than header size,
        ///     function will fail and return an error code.
        ///
        ///  2) After decoding has been started,
        ///     it's possible to invoke LZ4F_getFrameInfo() anytime
        ///     to extract already decoded frame parameters stored within dctx.
        ///
        ///     Note that, if decoding has barely started,
        ///     and not yet read enough information to decode the header,
        ///     LZ4F_getFrameInfo() will fail.
        ///
        ///  The number of bytes consumed from srcBuffer will be updated in ///srcSizePtr (necessarily &lt;= original value).
        ///  LZ4F_getFrameInfo() only consumes bytes when decoding has not yet started,
        ///  and when decoding the header has been successful.
        ///  Decompression must then resume from (srcBuffer + srcSizePtr).
        /// </remarks>
        /// <returns>
        /// a hint about how many srcSize bytes LZ4F_decompress() expects for next call, or an error code which can be tested using LZ4F_isError().
        ///     note 1 : in case of error, dctx is not modified. Decoding operation can resume from beginning safely.
        ///     note 2 : frame parameters are copied into an already allocated LZ4F_frameInfo_t structure.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_getFrameInfo(
            IntPtr dctx,
            FrameInfo frameInfoPtr,
            IntPtr srcCapacity,
            nuint srcSizePtr); // size_t
        internal LZ4F_getFrameInfo? GetFrameInfo;

        /// <summary>
        /// Call this function repetitively to regenerate data compressed in `srcBuffer`.
        /// 
        /// The function requires a valid dctx state.
        /// It will read up to* srcSizePtr bytes from srcBuffer,
        /// and decompress data into dstBuffer, of capacity *dstSizePtr.
        ///
        /// The number of bytes consumed from srcBuffer will be written into *srcSizePtr (necessarily &lt;= original value).
        /// The number of bytes decompressed into dstBuffer will be written into *dstSizePtr (necessarily &lt;= original value).
        ///
        /// The function does not necessarily read all input bytes, so always check value in *srcSizePtr.
        /// Unconsumed source data must be presented again in subsequent invocations.
        ///
        /// `dstBuffer` can freely change between each consecutive function invocation.
        /// `dstBuffer` content will be overwritten.
        /// </summary>
        /// <returns>
        /// an hint of how many `srcSize` bytes LZ4F_decompress() expects for next call.
        ///  Schematically, it's the size of the current (or remaining) compressed block + header of next block.
        ///  Respecting the hint provides some small speed benefit, because it skips intermediate buffers.
        ///  This is just a hint though, it's always possible to provide any srcSize.
        ///
        ///  When a frame is fully decoded, @return will be 0 (no more data expected).
        ///  When provided with more bytes than necessary to decode a frame,
        ///  LZ4F_decompress() will stop reading exactly at end of current frame, and @return 0.
        ///
        ///  If decompression failed, @return is an error code, which can be tested using LZ4F_isError().
        ///  After a decompression error, the `dctx` context is not resumable.
        ///  Use LZ4F_resetDecompressionContext() to return to clean state.
        ///
        ///  After a frame is fully decoded, dctx can be used again to decompress another frame.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_decompress(
            IntPtr dctx,
            byte* dstBuffer,
            ref nuint dstSizePtr, // size_t
            byte* srcBuffer,
            ref nuint srcSizePtr, // size_t
            FrameDecompressOptions dOptPtr);
        internal LZ4F_decompress? FrameDecompress;

        /// <summary>
        /// In case of an error, the context is left in "undefined" state.
        /// In which case, it's necessary to reset it, before re-using it.
        /// This method can also be used to abruptly stop any unfinished decompression,
        /// and start a new one using same context resources.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void LZ4F_resetDecompressionContext(IntPtr dctx);
        internal LZ4F_resetDecompressionContext? ResetDecompressContext;
        #endregion

        #region CDict - Bulk processing dictionary compression
        /// <summary>
        /// When compressing multiple messages / blocks using the same dictionary, it's recommended to initialize it just once.
        /// LZ4_createCDict() will create a digested dictionary, ready to start future compression operations without startup delay.
        /// LZ4_CDict can be created once and shared by multiple threads concurrently, since its usage is read-only.
        /// </summary>
        /// <param name="dictBuffer">
        /// @dictBuffer can be released after LZ4_CDict creation, since its content is copied within CDict.
        /// </param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr LZ4F_createCDict(
           byte* dictBuffer,
           nuint dictSize);
        internal LZ4F_createCDict? FrameCreateCDict;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr LZ4F_freeCDict(
           IntPtr CDict);
        internal LZ4F_freeCDict? FrameFreeCDict;

        /// <summary>
        /// Inits streaming dictionary compression, and writes the frame header into dstBuffer.
        /// </summary>
        /// <param name="cctx"></param>
        /// <param name="dstBuffer">@dstCapacity must be >= LZ4F_HEADER_SIZE_MAX bytes.</param>
        /// <param name="dstCapacity"></param>
        /// <param name="cdict">
        /// must outlive the compression session.
        /// </param>
        /// <param name="prefsPtr">
        /// @prefsPtr is optional : one may provide NULL as argument,
        /// note however that it's the only way to insert a @dictID in the frame header.
        /// </param>
        /// <returns>
        /// number of bytes written into dstBuffer for the header,
        /// or an error code, which can be tested using LZ4F_isError().
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate nuint LZ4F_compressBegin_usingCDict(
            IntPtr cctx,
            byte* dstBuffer,
            nuint dstCapacity,
            IntPtr cdict,
            FramePreferences prefsPtr);
        internal LZ4F_compressBegin_usingCDict? FrameCompressBeginUsingCDict;
        #endregion

        #region xxHash
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint LZ4_XXH_versionNumber();
        internal LZ4_XXH_versionNumber? XXHVersionNumber;

        /// <summary>
        /// Calculate the 32-bit hash of sequence "length" bytes stored at memory address "input".
        /// The memory between input & input+length must be valid (allocated and read-accessible).
        /// "seed" can be used to alter the result predictably.
        /// </summary>
        /// <remarks>xxhash is not chainable.</remarks>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint LZ4_XXH32(
            byte* input,
            nuint length,
            uint seed);
        internal LZ4_XXH32? XXH32;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr LZ4_XXH32_createState();
        internal LZ4_XXH32_createState? XXH32CreateState;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate XXHashErrorCode LZ4_XXH32_freeState(
            IntPtr statePtr);
        internal LZ4_XXH32_freeState? XXH32FreeState;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void LZ4_XXH32_copyState(
            IntPtr dstState, IntPtr srcState);
        internal LZ4_XXH32_copyState? XXH32CopyState;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate XXHashErrorCode LZ4_XXH32_reset(
            IntPtr statePtr, uint seed);
        internal LZ4_XXH32_reset? XXH32Reset;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate XXHashErrorCode LZ4_XXH32_update(
            IntPtr statePtr, byte* ipnut, nuint length);
        internal LZ4_XXH32_update? XXH32Update;

        /// <summary>
        /// In xxhash, you cannot continue feeding the data after calling the digest().
        /// </summary>
        /// <param name="statePtr"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint LZ4_XXH32_digest(
            IntPtr statePtr);
        internal LZ4_XXH32_digest? XXH32Digest;

        /// <summary>
        /// Calculate the 64-bit hash of sequence of length "len" stored at memory address "input".
        /// "seed" can be used to alter the result predictably.
        /// This function runs faster on 64-bit systems, but slower on 32-bit systems (see benchmark).
        /// </summary>
        /// <remarks>xxhash is not chainable.</remarks>
        /// <param name="input"></param>
        /// <param name="length"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong LZ4_XXH64(
            byte* input,
            nuint length,
            ulong seed);
        internal LZ4_XXH64? XXH64;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr LZ4_XXH64_createState();
        internal LZ4_XXH64_createState? XXH64CreateState;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate XXHashErrorCode LZ4_XXH64_freeState(
            IntPtr statePtr);
        internal LZ4_XXH64_freeState? XXH64FreeState;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void LZ4_XXH64_copyState(
            IntPtr dstState, IntPtr srcState);
        internal LZ4_XXH64_copyState? XXH64CopyState;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate XXHashErrorCode LZ4_XXH64_reset(
            IntPtr statePtr, ulong seed);
        internal LZ4_XXH64_reset? XXH64Reset;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate XXHashErrorCode LZ4_XXH64_update(
            IntPtr statePtr, byte* ipnut, nuint length);
        internal LZ4_XXH64_update? XXH64Update;

        /// <summary>
        /// In xxhash, you cannot continue feeding the data after calling the digest().
        /// </summary>
        /// <param name="statePtr"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong LZ4_XXH64_digest(
            IntPtr statePtr);
        internal LZ4_XXH64_digest? XXH64Digest;
        #endregion
        #endregion
    }
}
