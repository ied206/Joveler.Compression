/*
    Derived from LZ4 header files (BSD 2-Clause)
    Copyright (c) 2011-2016, Yann Collet

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018 Hajin Jang

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
using System.ComponentModel;
using System.Runtime.InteropServices;

// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable InconsistentNaming

namespace Joveler.LZ4
{
    #region PinnedArray
    internal class PinnedArray<T> : IDisposable
    {
        private GCHandle hArray;
        public T[] Array;
        public IntPtr Ptr => hArray.AddrOfPinnedObject();

        public IntPtr this[int idx] => Marshal.UnsafeAddrOfPinnedArrayElement(Array, idx);
        public static implicit operator IntPtr(PinnedArray<T> fixedArray) => fixedArray[0];

        public PinnedArray(T[] array)
        {
            Array = array;
            hArray = GCHandle.Alloc(array, GCHandleType.Pinned);
        }

        ~PinnedArray()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (hArray.IsAllocated)
                    hArray.Free();
            }
        }
    }
    #endregion

    #region NativeMethods
    internal static unsafe class NativeMethods
    {
        #region Const
        public const string MsgInitFirstError = "Please call LZ4Stream.GlobalInit() first!";
        public const string MsgAlreadyInited = "Joveler.LZ4 is already initialized.";
        #endregion

        #region Fields
        internal static IntPtr hModule;
        internal static bool Loaded => hModule != IntPtr.Zero;
        #endregion

        #region Windows kernel32 API
        internal static class Win32
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPTStr)] string lpFileName);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            internal static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern uint FreeLibrary(IntPtr hModule);
        }
        #endregion

        #region Linux libdl API
#pragma warning disable IDE1006 // 명명 스타일
        internal static class Linux
        {
            internal const int RTLD_NOW = 0x0002;
            internal const int RTLD_GLOBAL = 0x0100;

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr dlopen(string fileName, int flags);

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int dlclose(IntPtr handle);

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern string dlerror();

            [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr dlsym(IntPtr handle, string symbol);
        }
#pragma warning restore IDE1006 // 명명 스타일
        #endregion

        #region GetFuncPtr
        private static T GetFuncPtr<T>(string funcSymbol) where T : Delegate
        {
            IntPtr funcPtr;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                funcPtr = Win32.GetProcAddress(hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new ArgumentException($"Cannot import [{funcSymbol}]", new Win32Exception());
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                funcPtr = Linux.dlsym(hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new ArgumentException($"Cannot import [{funcSymbol}]", Linux.dlerror());
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        internal static void LoadFuntions()
        {
            #region Version - LzmaVersionNumber, LzmaVersionString
            VersionNumber = GetFuncPtr<LZ4_versionNumber>("LZ4_versionNumber");
            VersionString = GetFuncPtr<LZ4_versionString>("LZ4_versionString");
            GetFrameVersion = GetFuncPtr<LZ4F_getVersion>("LZ4F_getVersion");
            #endregion

            #region Error - IsError, GetErrorName
            FrameIsError = GetFuncPtr<LZ4F_isError>("LZ4F_isError");
            GetErrorName = GetFuncPtr<LZ4F_getErrorName>("LZ4F_getErrorName");
            #endregion

            #region FrameCompression
            CreateFrameCompressionContext = GetFuncPtr<LZ4F_createCompressionContext>("LZ4F_createCompressionContext");
            FreeFrameCompressionContext = GetFuncPtr<LZ4F_freeCompressionContext>("LZ4F_freeCompressionContext");
            FrameCompressionBegin = GetFuncPtr<LZ4F_compressBegin>("LZ4F_compressBegin");
            FrameCompressionBound = GetFuncPtr<LZ4F_compressBound>("LZ4F_compressBound");
            FrameCompressionUpdate = GetFuncPtr<LZ4F_compressUpdate>("LZ4F_compressUpdate");
            FrameFlush = GetFuncPtr<LZ4F_flush>("LZ4F_flush");
            FrameCompressionEnd = GetFuncPtr<LZ4F_compressEnd>("LZ4F_compressEnd");
            #endregion

            #region FrameDecompression
            CreateFrameDecompressionContext = GetFuncPtr<LZ4F_createDecompressionContext>("LZ4F_createDecompressionContext");
            FreeFrameDecompressionContext = GetFuncPtr<LZ4F_freeDecompressionContext>("LZ4F_freeDecompressionContext");
            GetFrameInfo = GetFuncPtr<LZ4F_getFrameInfo>("LZ4F_getFrameInfo");
            FrameDecompress = GetFuncPtr<LZ4F_decompress>("LZ4F_decompress");
            ResetDecompressionContext = GetFuncPtr<LZ4F_resetDecompressionContext>("LZ4F_resetDecompressionContext");
            #endregion
        }

        internal static void ResetFuntcions()
        {
            #region Version - LZ4VersionNumber, LZ4VersionString
            VersionNumber = null;
            VersionString = null;
            #endregion

            #region Error - IsError, GetErrorName
            FrameIsError = null;
            GetErrorName = null;
            #endregion

            #region FrameCompression
            CreateFrameCompressionContext = null;
            FreeFrameCompressionContext = null;
            FrameCompressionBegin = null;
            FrameCompressionBound = null;
            FrameCompressionUpdate = null;
            FrameFlush = null;
            FrameCompressionEnd = null;
            #endregion

            #region FrameDecompression
            CreateFrameDecompressionContext = null;
            FreeFrameDecompressionContext = null;
            GetFrameInfo = null;
            FrameDecompress = null;
            ResetDecompressionContext = null;
            #endregion
        }
        #endregion

        #region liblz4 Function Pointer
        #region Version - VersionNumber, VersionString
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint LZ4_versionNumber();
        internal static LZ4_versionNumber VersionNumber;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        internal delegate string LZ4_versionString();
        internal static LZ4_versionString VersionString;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint LZ4F_getVersion();
        internal static LZ4F_getVersion GetFrameVersion;
        #endregion

        #region Error - IsError, GetErrorName
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint LZ4F_isError(UIntPtr code); // size_t
        internal static LZ4F_isError FrameIsError;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr LZ4F_getErrorName(UIntPtr code); // size_t
        internal static LZ4F_getErrorName GetErrorName;
        #endregion

        #region FrameCompression
        /// <summary>
        /// The first thing to do is to create a compressionContext object, which will be used in all compression operations.
        /// This is achieved using LZ4F_createCompressionContext(), which takes as argument a version.
        /// The version provided MUST be LZ4F_VERSION. It is intended to track potential version mismatch, notably when using DLL.
        /// The function will provide a pointer to a fully allocated LZ4F_cctx object.
        /// </summary>
        /// <returns>
        /// If @return != zero, there was an error during context creation.
        /// Object can release its memory using LZ4F_freeCompressionContext();
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr LZ4F_createCompressionContext(
            ref IntPtr cctxPtr,
            uint version);
        internal static LZ4F_createCompressionContext CreateFrameCompressionContext;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr LZ4F_freeCompressionContext(IntPtr cctx);
        internal static LZ4F_freeCompressionContext FreeFrameCompressionContext;

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
        internal delegate UIntPtr LZ4F_compressBegin(
            IntPtr cctx,
            byte* dstBuffer,
            UIntPtr dstCapacity, // size_t
            FramePreferences prefsPtr);
        internal static LZ4F_compressBegin FrameCompressionBegin;

        /// <summary>
        ///  Provides minimum dstCapacity for a given srcSize to guarantee operation success in worst case situations.
        ///  prefsPtr is optional : when NULL is provided, preferences will be set to cover worst case scenario.
        ///  Result is always the same for a srcSize and prefsPtr, so it can be trusted to size reusable buffers.
        ///  When srcSize==0, LZ4F_compressBound() provides an upper bound for LZ4F_flush() and LZ4F_compressEnd() operations.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr LZ4F_compressBound(
            UIntPtr srcSize, // size_t
            FramePreferences prefsPtr);
        internal static LZ4F_compressBound FrameCompressionBound;

        /// <summary>
        ///  When data must be generated and sent immediately, without waiting for a block to be completely filled,
        ///  it's possible to call LZ4_flush(). It will immediately compress any data buffered within cctx.
        /// `dstCapacity` must be large enough to ensure the operation will be successful.
        /// `cOptPtr` is optional : it's possible to provide NULL, all options will be set to default.
        /// </summary>
        /// <return>
        /// number of bytes written into dstBuffer (it can be zero, which means there was no data stored within cctx)
        /// or an error code if it fails (which can be tested using LZ4F_isError())
        /// </return>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr LZ4F_compressUpdate(
            IntPtr cctx,
            byte* dstBuffer,
            UIntPtr dstCapacity, // size_t
            byte* srcBuffer,
            UIntPtr srcSize, // size_t
            FrameCompressOptions cOptPtr);
        internal static LZ4F_compressUpdate FrameCompressionUpdate;

        /// <summary>
        ///  When data must be generated and sent immediately, without waiting for a block to be completely filled,
        ///  it's possible to call LZ4_flush(). It will immediately compress any data buffered within cctx.
        /// `dstCapacity` must be large enough to ensure the operation will be successful.
        /// `cOptPtr` is optional : it's possible to provide NULL, all options will be set to default.
        /// </summary>
        /// <return>
        /// number of bytes written into dstBuffer (it can be zero, which means there was no data stored within cctx)
        /// or an error code if it fails (which can be tested using LZ4F_isError())
        /// </return>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr LZ4F_flush(
            IntPtr cctx,
            byte* dstBuffer,
            UIntPtr dstCapacity, // size_t
            FrameCompressOptions cOptPtr);
        internal static LZ4F_flush FrameFlush;

        /// <summary>
        ///  To properly finish an LZ4 frame, invoke LZ4F_compressEnd().
        ///  It will flush whatever data remained within `cctx` (like LZ4_flush())
        ///  and properly finalize the frame, with an endMark and a checksum.
        /// `cOptPtr` is optional : NULL can be provided, in which case all options will be set to default.
        /// </summary>
        /// /// <return>
        /// number of bytes written into dstBuffer (necessarily >= 4 (endMark), or 8 if optional frame checksum is enabled)
        /// or an error code if it fails (which can be tested using LZ4F_isError())
        /// A successful call to LZ4F_compressEnd() makes `cctx` available again for another compression task.
        /// </return>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr LZ4F_compressEnd(
            IntPtr cctx,
            byte* dstBuffer,
            UIntPtr dstCapacity, // size_t
            FrameCompressOptions cOptPtr);
        internal static LZ4F_compressEnd FrameCompressionEnd;
        #endregion

        #region FrameDecompression
        /// <summary>
        ///  Create an LZ4F_dctx object, to track all decompression operations.
        ///  The version provided MUST be LZ4F_VERSION.
        ///  The function provides a pointer to an allocated and initialized LZ4F_dctx object.
        ///  The result is an errorCode, which can be tested using LZ4F_isError().
        ///  dctx memory can be released using LZ4F_freeDecompressionContext();
        /// </summary>
        /// <returns>
        /// The result of LZ4F_freeDecompressionContext() is indicative of the current state of decompressionContext when being released.
        /// That is, it should be == 0 if decompression has been completed fully and correctly.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr LZ4F_createDecompressionContext(
            ref IntPtr cctxPtr,
            uint version);
        internal static LZ4F_createDecompressionContext CreateFrameDecompressionContext;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr LZ4F_freeDecompressionContext(IntPtr dctx);
        internal static LZ4F_freeDecompressionContext FreeFrameDecompressionContext;

        /// <summary>
        ///  This function extracts frame parameters (max blockSize, dictID, etc.).
        /// </summary>
        /// <remarks>
        ///  Its usage is optional.
        ///  Extracted information is typically useful for allocation and dictionary.
        ///  This function works in 2 situations :
        ///   - At the beginning of a new frame, in which case
        ///     it will decode information from `srcBuffer`, starting the decoding process.
        ///     Input size must be large enough to successfully decode the entire frame header.
        ///     Frame header size is variable, but is guaranteed to be &lt;= LZ4F_HEADER_SIZE_MAX bytes.
        ///     It's allowed to provide more input data than this minimum.
        ///   - After decoding has been started.
        ///     In which case, no input is read, frame parameters are extracted from dctx.
        ///   - If decoding has barely started, but not yet extracted information from header,
        ///     LZ4F_getFrameInfo() will fail.
        ///  The number of bytes consumed from srcBuffer will be updated within *srcSizePtr (necessarily &lt;= original value).
        ///  Decompression must resume from (srcBuffer + *srcSizePtr).
        /// </remarks>
        /// <returns>
        /// an hint about how many srcSize bytes LZ4F_decompress() expects for next call,
        ///           or an error code which can be tested using LZ4F_isError().
        ///  note 1 : in case of error, dctx is not modified. Decoding operation can resume from beginning safely.
        ///  note 2 : frame parameters are *copied into* an already allocated LZ4F_frameInfo_t structure.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr LZ4F_getFrameInfo(
            IntPtr dctx,
            FrameInfo frameInfoPtr,
            IntPtr srcCapacity,
            UIntPtr srcSizePtr); // size_t
        internal static LZ4F_getFrameInfo GetFrameInfo;

        /// <summary>
        ///  Call this function repetitively to regenerate compressed data from `srcBuffer`.
        ///  The function will read up to *srcSizePtr bytes from srcBuffer,
        ///  and decompress data into dstBuffer, of capacity *dstSizePtr.
        ///
        ///  The number of bytes consumed from srcBuffer will be written into *srcSizePtr (necessarily &lt;= original value).
        ///  The number of bytes decompressed into dstBuffer will be written into *dstSizePtr (necessarily &lt;= original value).
        ///
        ///  The function does not necessarily read all input bytes, so always check value in *srcSizePtr.
        ///  Unconsumed source data must be presented again in subsequent invocations.
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
        internal delegate UIntPtr LZ4F_decompress(
            IntPtr dctx,
            byte* dstBuffer,
            ref UIntPtr dstSizePtr, // size_t
            byte* srcBuffer,
            ref UIntPtr srcSizePtr, // size_t
            FrameDecompressOptions dOptPtr);
        internal static LZ4F_decompress FrameDecompress;

        /// <summary>
        /// In case of an error, the context is left in "undefined" state.
        /// In which case, it's necessary to reset it, before re-using it.
        /// This method can also be used to abruptly stop any unfinished decompression,
        /// and start a new one using same context resources.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void LZ4F_resetDecompressionContext(IntPtr dctx);
        internal static LZ4F_resetDecompressionContext ResetDecompressionContext;
        #endregion

        #endregion
    }
    #endregion
}
