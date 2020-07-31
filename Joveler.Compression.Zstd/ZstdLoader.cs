using Joveler.DynLoader;
using System;
using System.Runtime.InteropServices;

namespace Joveler.Compression.Zstd
{
    public unsafe class ZstdLoader : DynLoaderBase
    {
        #region Constructor
        public ZstdLoader() : base() { }
        public ZstdLoader(string libPath) : base(libPath) { }
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

            #region Simple API - SimpleCompress, SimpleDecompress
            SimpleCompress = GetFuncPtr<ZSTD_compress>();
            SimpleDecompress = GetFuncPtr<ZSTD_decompress>();
            #endregion
        }

        protected override void ResetFunctions()
        {
            #region Version - VersionNumber, VersionString
            VersionNumber = null;
            VersionString = null;
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate UIntPtr ZSTD_decompress(
            byte* dst, UIntPtr dstCapabity,
            byte* src, UIntPtr compressedSize);
        internal ZSTD_decompress SimpleDecompress;
        #endregion
        #endregion
    }
}
