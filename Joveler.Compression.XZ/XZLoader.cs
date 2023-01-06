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
            LzmaStreamDecoderMt = GetFuncPtr<lzma_stream_decoder_mt>(nameof(lzma_stream_decoder_mt));
            LzmaAloneDecoder = GetFuncPtr<lzma_alone_decoder>(nameof(lzma_alone_decoder));
            LzmaLZipDecoder = GetFuncPtr<lzma_lzip_decoder>(nameof(lzma_lzip_decoder));
            LzmaAutoDecoder = GetFuncPtr<lzma_auto_decoder>(nameof(lzma_auto_decoder));
            #endregion

            #region Hardware - PhyMem & CPU Threads
            LzmaPhysMem = GetFuncPtr<lzma_physmem>(nameof(lzma_physmem));
            LzmaCpuThreads = GetFuncPtr<lzma_cputhreads>(nameof(lzma_cputhreads));
            #endregion

            #region Memory - Memusage, MemlimitGet, MemlimitSet (DISABLED)
#if LZMA_MEM_ENABLE
            LzmaMemusage = GetFuncPtr<lzma_memusage>(nameof(lzma_memusage));
            LzmaMemlimitGet = GetFuncPtr<lzma_memlimit_get>(nameof(lzma_memlimit_get));
            LzmaMemlimitSet = GetFuncPtr<lzma_memlimit_set>(nameof(lzma_memlimit_set));
#endif
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
            LzmaStreamDecoderMt = null;
            LzmaAloneDecoder = null;
            LzmaLZipDecoder = null;
            LzmaAutoDecoder = null;
            #endregion

            #region Hardware - PhyMem & CPU Threads
            LzmaPhysMem = null;
            LzmaCpuThreads = null;
            #endregion

            #region Memory - Memusage, MemlimitGet, MemlimitSet (DISABLED)
#if LZMA_MEM_ENABLE
            LzmaMemusage = null;
            LzmaMemlimitGet = null;
            LzmaMemlimitSet = null;
#endif
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

#if LZMA_MICROLZMA_ENABLE
        /// <summary>
        /// MicroLZMA encoder
        /// </summary>
        /// <remarks>
        /// The MicroLZMA format is a raw LZMA stream whose first byte (always 0x00)
        /// has been replaced with bitwise-negation of the LZMA properties (lc/lp/pb).
        /// This encoding ensures that the first byte of MicroLZMA stream is never
        /// 0x00. There is no end of payload marker and thus the uncompressed size
        /// must be stored separately. For the best error detection the dictionary
        /// size should be stored separately as well but alternatively one may use
        /// the uncompressed size as the dictionary size when decoding.
        ///
        /// With the MicroLZMA encoder, lzma_code() behaves slightly unusually.
        /// The action argument must be LZMA_FINISH and the return value will never be
        /// LZMA_OK. Thus the encoding is always done with a single lzma_code() after
        /// the initialization. The benefit of the combination of initialization
        /// function and lzma_code() is that memory allocations can be re-used for
        /// better performance.
        ///
        /// lzma_code() will try to encode as much input as is possible to fit into
        /// the given output buffer. If not all input can be encoded, the stream will
        /// be finished without encoding all the input. The caller must check both
        /// input and output buffer usage after lzma_code() (total_in and total_out
        /// in lzma_stream can be convenient). Often lzma_code() can fill the output
        /// buffer completely if there is a lot of input, but sometimes a few bytes
        /// may remain unused because the next LZMA symbol would require more space.
        /// 
        /// lzma_stream.avail_out must be at least 6. Otherwise LZMA_PROG_ERROR
        /// will be returned.
        /// 
        /// The LZMA dictionary should be reasonably low to speed up the encoder
        /// re-initialization. A good value is bigger than the resulting
        /// uncompressed size of most of the output chunks. For example, if output
        /// size is 4 KiB, dictionary size of 32 KiB or 64 KiB is good. If the
        /// data compresses extremely well, even 128 KiB may be useful.
        ///
        /// The MicroLZMA format and this encoder variant were made with the EROFS
        /// file system in mind. This format may be convenient in other embedded
        /// uses too where many small streams are needed. XZ Embedded includes a
        /// decoder for this format.
        /// </remarks>
        /// <returns>
        /// - LZMA_STREAM_END: All good. Check the amounts of input used
        ///                and output produced. Store the amount of input used
        ///                (uncompressed size) as it needs to be known to decompress
        ///                the data.
        ///              - LZMA_OPTIONS_ERROR
        ///              - LZMA_MEM_ERROR
        ///              - LZMA_PROG_ERROR: In addition to the generic reasons for this
        ///                error code, this may also be returned if there isn't enough
        ///                output space (6 bytes) to create a valid MicroLZMA stream.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_microlzma_encoder(
            LzmaStream strm,
            lzma_options_lzma options);
        internal lzma_microlzma_encoder LzmaMicroLzmaEncoder;
#endif

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
        /// LZMA_TELL_NO_CHECK, LZMA_TELL_UNSUPPORTED_CHECK,
        /// LZMA_TELL_ANY_CHECK, LZMA_IGNORE_CHECK,
        /// LZMA_CONCATENATED, LZMA_FAIL_FAST
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

        /// <summary>
        /// Initialize multithreaded .xz Stream decoder
        /// </summary>
        /// <param name="strm">Pointer to properly prepared lzma_stream</param>
        /// <param name="options">Pointer to multithreaded compression options</param>
        /// <remarks>
        /// The decoder can decode multiple Blocks in parallel. This requires that each
        /// Block Header contains the Compressed Size and Uncompressed size fields
        /// which are added by the multi-threaded encoder, see lzma_stream_encoder_mt().
        ///
        /// A Stream with one Block will only utilize one thread. A Stream with multiple
        /// Blocks but without size information in Block Headers will be processed in
        /// single-threaded mode in the same way as done by lzma_stream_decoder().
        /// Concatenated Streams are processed one Stream at a time; no inter-Stream
        /// parallelization is done.
        ///
        /// This function behaves like lzma_stream_decoder() when options->threads == 1
        /// and options->memlimit_threading <= 1.
        /// </remarks>
        /// <returns>
        /// LZMA_OK: Initialization was successful.
        /// LZMA_MEM_ERROR: Cannot allocate memory.
        /// LZMA_MEMLIMIT_ERROR: Memory usage limit was reached.
        /// LZMA_OPTIONS_ERROR: Unsupported flags.
        /// LZMA_PROG_ERROR
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_stream_decoder_mt(
            LzmaStream strm,
            LzmaMt options);
        internal lzma_stream_decoder_mt LzmaStreamDecoderMt;

        /// <summary>
        /// Decode .xz, .lzma, and .lz (lzip) files with autodetection
        /// </summary>
        /// <remarks>
        /// This decoder autodetects between the .xz, .lzma, and .lz file formats,
        /// and calls lzma_stream_decoder(), lzma_alone_decoder(), or
        /// lzma_lzip_decoder() once the type of the input file has been detected.
        ///
        /// Support for .lz was added in 5.4.0.
        ///
        /// If the flag LZMA_CONCATENATED is used and the input is a .lzma file:
        /// For historical reasons concatenated .lzma files aren't supported.
        /// If there is trailing data after one .lzma stream, lzma_code() will
        /// return LZMA_DATA_ERROR. (lzma_alone_decoder() doesn't have such a check
        /// as it doesn't support any decoder flags. It will return LZMA_STREAM_END
        /// after one .lzma stream.)
        /// </remarks>
        /// <param name="strm">
        /// Pointer to properly prepared lzma_stream
        /// </param>
        /// <param name="memlimit">
        /// Memory usage limit as bytes. Use <see cref="UInt64.MaxValue"/> to effectively disable the limiter.
        /// liblzma 5.2.3 and earlier don't allow 0 here and return <see cref="LzmaRet.ProgError"/>;
        /// later versions treat 0 as if 1 had been specified.
        /// </param>
        /// <param name="flags">
        /// Bitwise-or of zero or more of the decoder flags:
        /// <see cref="LzmaDecodingFlag.TellNoCheck"/>, <see cref="LzmaDecodingFlag.TellUnsupportedCheck"/>,
        /// <see cref="LzmaDecodingFlag.TellAnyCheck"/>, <see cref="LzmaDecodingFlag.IgnoreCheck"/>,
        /// <see cref="LzmaDecodingFlag.Concatenated"/>, <see cref="LzmaDecodingFlag.FailFast"/>
        /// </param>
        /// <returns>
        /// - <see cref="LzmaRet.Ok"/>: Initialization was successful.
        /// - <see cref="LzmaRet.MemError"/>: Cannot allocate memory.
        /// - <see cref="LzmaRet.OptionsError"/>: Unsupported flags
        /// - <see cref="LzmaRet.ProgError"/>
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_auto_decoder(
            LzmaStream strm,
            ulong memlimit,
            LzmaDecodingFlag flags);
        internal lzma_auto_decoder LzmaAutoDecoder;

        /// <summary>
        /// Initialize .lzma decoder (legacy file format)
        /// </summary>
        /// <remarks>
        /// Valid `action' arguments to lzma_code() are LZMA_RUN and LZMA_FINISH.
        /// There is no need to use LZMA_FINISH, but it's allowed because it may
        /// simplify certain types of applications.
        /// </remarks>
        /// <param name="strm">
        /// Pointer to properly prepared lzma_stream
        /// </param>
        /// <param name="memlimit">
        /// Memory usage limit as bytes. Use <see cref="UInt64.MaxValue"/> to effectively disable the limiter.
        /// liblzma 5.2.3 and earlier don't allow 0 here and return LZMA_PROG_ERROR; later versions treat 0 as if 1 had been specified.
        /// </param>
        /// <returns>
        /// - <see cref="LzmaRet.Ok"/>
        /// - <see cref="LzmaRet.MemError"/>
        /// - <see cref="LzmaRet.ProgError"/>
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_alone_decoder(
            LzmaStream strm,
            ulong memlimit);
        internal lzma_alone_decoder LzmaAloneDecoder;

        /// <summary>
        /// Initialize .lz (lzip) decoder (a foreign file format)
        /// </summary>
        /// <remarks>
        /// This decoder supports the .lz format version 0 and the unextended .lz
        /// format version 1:
        ///
        ///   - Files in the format version 0 were produced by lzip 1.3 and older.
        ///     Such files aren't common but may be found from file archives
        ///     as a few source packages were released in this format. People
        ///     might have old personal files in this format too. Decompression
        ///     support for the format version 0 was removed in lzip 1.18.
        ///
        ///   - lzip 1.3 added decompression support for .lz format version 1 files.
        ///     Compression support was added in lzip 1.4. In lzip 1.6 the .lz format
        ///     version 1 was extended to support the Sync Flush marker. This extension
        ///     is not supported by liblzma. lzma_code() will return LZMA_DATA_ERROR
        ///     at the location of the Sync Flush marker. In practice files with
        ///     the Sync Flush marker are very rare and thus liblzma can decompress
        ///     almost all .lz files.
        ///
        /// Just like with lzma_stream_decoder() for .xz files, LZMA_CONCATENATED
        /// should be used when decompressing normal standalone .lz files.
        ///
        /// The .lz format allows putting non-.lz data at the end of a file after at
        /// least one valid .lz member. That is, one can append custom data at the end
        /// of a .lz file and the decoder is required to ignore it. In liblzma this
        /// is relevant only when LZMA_CONCATENATED is used. In that case lzma_code()
        /// will return LZMA_STREAM_END and leave lzma_stream.next_in pointing to
        /// the first byte of the non-.lz data. An exception to this is if the first
        /// 1-3 bytes of the non-.lz data are identical to the .lz magic bytes
        /// (0x4C, 0x5A, 0x49, 0x50; "LZIP" in US-ASCII). In such a case the 1-3 bytes
        /// will have been ignored by lzma_code(). If one wishes to locate the non-.lz
        /// data reliably, one must ensure that the first byte isn't 0x4C. Actually
        /// one should ensure that none of the first four bytes of trailing data are
        /// equal to the magic bytes because lzip >= 1.20 requires it by default.
        /// </remarks>
        /// <param name="strm">
        /// Pointer to properly prepared lzma_stream
        /// </param>
        /// <param name="memlimit">
        /// Memory usage limit as bytes. Use UINT64_MAX to effectively disable the limiter.
        /// </param>
        /// <param name="flags">
        /// Bitwise-or of flags, or zero for no flags.
        /// All decoder flags listed above are supported although only LZMA_CONCATENATED 
        /// and (in very rare cases) LZMA_IGNORE_CHECK are actually useful.
        /// LZMA_TELL_NO_CHECK, LZMA_TELL_UNSUPPORTED_CHECK, and LZMA_FAIL_FAST do nothing. 
        /// LZMA_TELL_ANY_CHECK is supported for consistency only as CRC32 is
        /// always used in the .lz format.
        /// </param>
        /// <returns>
        /// - <see cref="LzmaRet.Ok"/>: Initialization was successful.
        /// - <see cref="LzmaRet.MemError"/>: Cannot allocate memory.
        /// - <see cref="LzmaRet.OptionsError"/>: Unsupported flags
        /// - <see cref="LzmaRet.ProgError"/>
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_lzip_decoder(
            LzmaStream strm,
            ulong memlimit,
            uint flags);
        internal lzma_lzip_decoder LzmaLZipDecoder;
        #endregion

        #region Hardware - PhyMem & CPU Threads
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_physmem();
        internal lzma_physmem LzmaPhysMem;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint lzma_cputhreads();
        internal lzma_cputhreads LzmaCpuThreads;
        #endregion

        #region Memory - Memusage, MemlimitGet, MemlimitSet (DISABLED)
#if LZMA_MEM_ENABLE
        /// <summary>
        /// Get the memory usage of decoder filter chain
        /// </summary>
        /// <remarks>
        /// <para>This function is currently supported only when *strm has been initialized
        /// with a function that takes a memlimit argument. With other functions, you
        /// should use e.g. lzma_raw_encoder_memusage() or lzma_raw_decoder_memusage()
        /// to estimate the memory requirements.</para>
        ///
        /// <para>This function is useful e.g. after LZMA_MEMLIMIT_ERROR to find out how big
        /// the memory usage limit should have been to decode the input. Note that
        /// this may give misleading information if decoding .xz Streams that have
        /// multiple Blocks, because each Block can have different memory requirements.</para>
        /// </remarks>
        /// <returns>
        /// How much memory is currently allocated for the filter decoders.
        /// If no filter chain is currently allocated, some non-zero value is still returned,
        /// which is less than or equal to what any filter chain would indicate as its  memory requirement.
        ///
        /// If this function isn't supported by *strm or some other error occurs, zero is returned.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_memusage(LzmaStream strm);
        internal lzma_memusage LzmaMemusage;

        /// <summary>
        /// This function is supported only when *strm has been initialized with
        /// a function that takes a memlimit argument.
        /// </summary>
        /// <returns>
        /// On success, the current memory usage limit is returned
        /// (always non-zero). On error, zero is returned.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_memlimit_get(LzmaStream strm);
        internal lzma_memlimit_get LzmaMemlimitGet;

        /// <summary>
        /// Set the memory usage limit
        /// 
        /// This function is supported only when *strm has been initialized with
        /// a function that takes a memlimit argument.
        /// </summary>
        /// <remarks>
        /// liblzma 5.2.3 and earlier has a bug where memlimit value of 0 causes
        /// this function to do nothing (leaving the limit unchanged) and still
        /// return LZMA_OK. Later versions treat 0 as if 1 had been specified (so
        /// lzma_memlimit_get() will return 1 even if you specify 0 here).
        ///
        /// liblzma 5.2.6 and earlier had a bug in single-threaded .xz decoder
        /// (lzma_stream_decoder()) which made it impossible to continue decoding
        /// after LZMA_MEMLIMIT_ERROR even if the limit was increased using
        /// lzma_memlimit_set(). Other decoders worked correctly.
        /// </remarks>
        /// <returns>
        /// - LZMA_OK: New memory usage limit successfully set.
        /// - LZMA_MEMLIMIT_ERROR: The new limit is too small. The limit was not changed.
        /// - LZMA_PROG_ERROR: Invalid arguments, e.g. *strm doesn't support memory usage limit.
        /// </returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_memlimit_set(LzmaStream strm);
        internal lzma_memlimit_set LzmaMemlimitSet;
#endif
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

        #region Memlimit - Memlimit
        #endregion
        #endregion
    }
}
