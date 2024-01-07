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

using System;
using System.Runtime.InteropServices;

namespace Joveler.Compression.Zstd
{
    // Compression Context enum/struct
    #region (public) enum Strategy
    /// <summary>
    /// Compression strategies, listed from fastest to strongest
    /// </summary>
    /// <remarks>
    /// note : new strategies _might_ be added in the future.
    ///        Only the order (from fast to strong) is guaranteed 
    /// </remarks>
    public enum Strategy
    {
        Fast = 1,
        DFast = 2,
        Greedy = 3,
        Lazy = 4,
        Lazy2 = 5,
        BtLazy2 = 6,
        BtOpt = 7,
        BtUltra = 8,
        BtUltra2 = 9,
    }
    #endregion

    #region (internal) enum CParameter
    /// <summary>
    /// Compression parameters.
    /// Note: When compressing with a ZSTD_CDict these parameters are superseded by the parameters used to construct the ZSTD_CDict.
    /// See ZSTD_CCtx_refCDict() for more info (superseded-by-cdict).
    /// </summary>
    internal enum CParameter
    {
        // Compression parameters
        /// <summary>
        /// Set compression parameters according to pre-defined cLevel table.
        /// 
        /// Note that exact compression parameters are dynamically determined,
        /// depending on both compression level and srcSize (when known).
        /// Default level is ZSTD_CLEVEL_DEFAULT==3.
        /// 
        /// Special: value 0 means default, which is controlled by ZSTD_CLEVEL_DEFAULT.
        /// </summary>
        /// <remarks>
        /// Note 1 : it's possible to pass a negative compression level.
        /// Note 2 : setting a level does not automatically set all other compression parameters
        ///   to default. Setting this will however eventually dynamically impact the compression
        ///   parameters which have not been manually set. The manually set
        ///   ones will 'stick'.
        /// </remarks>
        CompressionLevel = 100,

        // Advanced compression parameters
        // It's possible to pin down compression parameters to some specific values.
        // In which case, these values are no longer dynamically selected by the compressor
        /// <summary>
        /// Maximum allowed back-reference distance, expressed as power of 2.
        /// 
        /// This will set a memory budget for streaming decompression,
        /// with larger values requiring more memory
        /// and typically compressing more.
        /// Must be clamped between ZSTD_WINDOWLOG_MIN and ZSTD_WINDOWLOG_MAX.
        /// 
        /// Special: value 0 means "use default windowLog".
        /// </summary>
        /// <remarks>
        /// Note: Using a windowLog greater than ZSTD_WINDOWLOG_LIMIT_DEFAULT
        ///       requires explicitly allowing such size at streaming decompression stage.
        /// </remarks>
        WindowLog = 101,
        /// <summary>
        /// Size of the initial probe table, as a power of 2.
        /// Resulting memory usage is (1 << (hashLog+2)).
        /// Must be clamped between ZSTD_HASHLOG_MIN and ZSTD_HASHLOG_MAX.
        /// Larger tables improve compression ratio of strategies <= dFast,
        /// and improve speed of strategies > dFast.
        /// Special: value 0 means "use default hashLog".
        /// </summary>
        HashLog = 102,
        /// <summary>
        /// Size of the multi-probe search table, as a power of 2.
        /// Resulting memory usage is (1 << (chainLog+2)).
        /// Must be clamped between ZSTD_CHAINLOG_MIN and ZSTD_CHAINLOG_MAX.
        /// Larger tables result in better and slower compression.
        /// This parameter is useless for "fast" strategy.
        /// It's still useful when using "dfast" strategy,
        /// in which case it defines a secondary probe table.
        /// Special: value 0 means "use default chainLog".
        /// </summary>
        ChainLog = 103,
        /// <summary>
        /// Number of search attempts, as a power of 2.
        /// More attempts result in better and slower compression.
        /// This parameter is useless for "fast" and "dFast" strategies.
        /// Special: value 0 means "use default searchLog".
        /// </summary>
        SearchLog = 104,
        /// <summary>
        /// Minimum size of searched matches.
        /// Note that Zstandard can still find matches of smaller size,
        /// it just tweaks its search algorithm to look for this size and larger.
        /// Larger values increase compression and decompression speed, but decrease ratio.
        /// Must be clamped between ZSTD_MINMATCH_MIN and ZSTD_MINMATCH_MAX.
        /// Note that currently, for all strategies < btopt, effective minimum is 4.
        ///                    , for all strategies > fast, effective maximum is 6.
        /// Special: value 0 means "use default minMatchLength". 
        /// </summary>
        MinMatch = 105,
        /// <summary>
        /// Impact of this field depends on strategy.
        /// For strategies btopt, btultra & btultra2:
        ///     Length of Match considered "good enough" to stop search.
        ///     Larger values make compression stronger, and slower.
        /// For strategy fast:
        ///     Distance between match sampling.
        ///     Larger values make compression faster, and weaker.
        /// Special: value 0 means "use default targetLength".
        /// </summary>
        TargetLength = 106,
        /// <summary>
        /// See ZSTD_strategy enum definition.
        /// The higher the value of selected strategy, the more complex it is,
        /// resulting in stronger and slower compression.
        /// Special: value 0 means "use default strategy".
        /// </summary>
        Strategy = 107,

        // LDM mode parameters
        /// <summary>
        /// Enable long distance matching.
        /// This parameter is designed to improve compression ratio
        /// for large inputs, by finding large matches at long distance.
        /// It increases memory usage and window size.
        /// Note: enabling this parameter increases default ZSTD_c_windowLog to 128 MB
        /// except when expressly set to a different value.
        /// </summary>
        EnableLongDistanceMatching = 160,
        /// <summary>
        /// Size of the table for long distance matching, as a power of 2.
        /// Larger values increase memory usage and compression ratio,
        /// but decrease compression speed.
        /// Must be clamped between ZSTD_HASHLOG_MIN and ZSTD_HASHLOG_MAX
        /// default: windowlog - 7.
        /// Special: value 0 means "automatically determine hashlog".
        /// </summary>
        LdmHashLog = 161,
        /// <summary>
        /// Minimum match size for long distance matcher.
        /// Larger/too small values usually decrease compression ratio.
        /// Must be clamped between ZSTD_LDM_MINMATCH_MIN and ZSTD_LDM_MINMATCH_MAX.
        /// Special: value 0 means "use default value" (default: 64). */
        /// </summary>
        LdmMinMatch = 162,
        /// <summary>
        /// Log size of each bucket in the LDM hash table for collision resolution.
        /// Larger values improve collision resolution but decrease compression speed.
        /// The maximum value is ZSTD_LDM_BUCKETSIZELOG_MAX.
        /// Special: value 0 means "use default value" (default: 3).
        /// </summary>
        LdmBucketSizeLog = 163,
        /// <summary>
        /// Frequency of inserting/looking up entries into the LDM hash table.
        /// Must be clamped between 0 and (ZSTD_WINDOWLOG_MAX - ZSTD_HASHLOG_MIN).
        /// Default is MAX(0, (windowLog - ldmHashLog)), optimizing hash table usage.
        /// Larger values improve compression speed.
        /// Deviating far from default value will likely result in a compression ratio decrease.
        /// Special: value 0 means "automatically determine hashRateLog".
        /// </summary>
        LdmHashRateLog = 164,

        // frame parameters
        /// <summary>
        /// Content size will be written into frame header _whenever known_ (default:1)
        /// Content size must be known at the beginning of compression.
        /// This is automatically the case when using ZSTD_compress2(),
        /// For streaming scenarios, content size must be provided with ZSTD_CCtx_setPledgedSrcSize()
        /// </summary>
        ContentSizeFlag = 200,
        /// <summary>
        /// A 32-bits checksum of content is written at end of frame (default:0)
        /// </summary>
        ChecksumFlag = 201,
        /// <summary>
        /// When applicable, dictionary's ID is written into frame header (default:1)
        /// </summary>
        DictIdFlag = 202,

        // multi-threading parameters
        // These parameters are only useful if multi-threading is enabled (compiled with build macro ZSTD_MULTITHREAD).
        // They return an error otherwise.
        /// <summary>
        /// Select how many threads will be spawned to compress in parallel.
        /// When nbWorkers >= 1, triggers asynchronous mode when used with ZSTD_compressStream* () :
        /// ZSTD_compressStream* () consumes input and flush output if possible, but immediately gives back control to caller,
        /// while compression work is performed in parallel, within worker threads.
        /// (note : a strong exception to this rule is when first invocation of ZSTD_compressStream2() sets ZSTD_e_end :
        ///  in which case, ZSTD_compressStream2() delegates to ZSTD_compress2(), which is always a blocking call).
        /// More workers improve speed, but also increase memory usage.
        /// Default value is `0`, aka "single-threaded mode" : no worker is spawned, compression is performed inside Caller's thread, all invocations are blocking
        /// </summary>
        NbWorkers = 400,
        /// <summary>
        /// Size of a compression job. This value is enforced only when nbWorkers >= 1.
        /// Each compression job is completed in parallel, so this value can indirectly impact the nb of active threads.
        /// 0 means default, which is dynamically determined based on compression parameters.
        /// Job size must be a minimum of overlap size, or 1 MB, whichever is largest.
        /// The minimum size is automatically and transparently enforced.
        /// </summary>
        JobSize = 401,
        /// <summary>
        /// Control the overlap size, as a fraction of window size.
        /// The overlap size is an amount of data reloaded from previous job at the beginning of a new job.
        /// It helps preserve compression ratio, while each job is compressed in parallel.
        /// This value is enforced only when nbWorkers >= 1.
        /// Larger values increase compression ratio, but decrease speed.
        /// Possible values range from 0 to 9 :
        /// - 0 means "default" : value will be determined by the library, depending on strategy
        /// - 1 means "no overlap"
        /// - 9 means "full overlap", using a full window size.
        /// Each intermediate rank increases/decreases load size by a factor 2 :
        /// 9: full window;  8: w/2;  7: w/4;  6: w/8;  5:w/16;  4: w/32;  3:w/64;  2:w/128;  1:no overlap;  0:default
        /// default value varies between 6 and 9, depending on strategy
        /// </summary>
        OverlapLog = 402,
    }
    #endregion

    #region (internal) struct Bounds
    [StructLayout(LayoutKind.Sequential)]
    internal struct Bounds
    {
        public UIntPtr Error; // size_t
        public int LowerBound;
        public int UpperBound;
    }
    #endregion

    #region (internal) enum ResetDirective
    internal enum ResetDirective
    {
        ResetSessionOnly = 1,
        ResetParameters = 2,
        ResetSessionAndParameters = 3,
    }
    #endregion

    // Decompression Context enum/struct
    #region (internal) enum DParameter
    /// <summary>
    /// Select a size limit (in power of 2) beyond which
    /// the streaming API will refuse to allocate memory buffer
    /// in order to protect the host from unreasonable memory requirements.
    /// This parameter is only useful in streaming mode, since no internal buffer is allocated in single-pass mode.
    /// By default, a decompression context accepts window sizes <= (1 << ZSTD_WINDOWLOG_LIMIT_DEFAULT).
    /// Special: value 0 means "use default maximum windowLog".
    /// </summary>
    internal enum DParameter
    {
        WindowLogMax = 100,
    }
    #endregion

    // Streaming
    #region (internal) InBuffer
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe class InBuffer
    {
        /// <summary>
        /// start of input buffer
        /// </summary>
        public byte* Src;
        /// <summary>
        /// size of input buffer
        /// </summary>
        public UIntPtr Size;
        /// <summary>
        /// position where reading stopped. Will be updated. Necessarily 0 <= pos <= size
        /// </summary>
        public UIntPtr Pos;
    }
    #endregion

    #region (internal) OutBuffer
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe class OutBuffer
    {
        /// <summary>
        /// start of output buffer
        /// </summary>
        public byte* Dst;
        /// <summary>
        /// size of output buffer
        /// </summary>
        public UIntPtr Size;
        /// <summary>
        /// position where writing stopped. Will be updated. Necessarily 0 <= pos <= size
        /// </summary>
        public UIntPtr Pos;
    }
    #endregion

    // Compression Stream enum/struct
    #region (internal) EndDirective
    internal enum EndDirective
    {
        /// <summary>
        /// collect more data, encoder decides when to output compressed result, for optimal compression ratio
        /// </summary>
        Continue = 0,
        /// <summary>
        /// flush any data provided so far,
        /// it creates (at least) one new block, that can be decoded immediately on reception;
        /// frame will continue: any future data can still reference previously compressed data, improving compression.
        /// note : multithreaded compression will block to flush as much output as possible.
        /// </summary>
        Flush = 1,
        /// <summary>
        /// flush any remaining data _and_ close current frame.
        /// note that frame is only closed after compressed data is fully flushed (return value == 0).
        /// After that point, any additional data starts a new frame.
        /// note : each frame is independent(does not reference any content from previous frame).
        /// note : multithreaded compression will block to flush as much output as possible
        /// </summary>
        End = 2,
    }
    #endregion

    #region (internal) class Sequence
    // ZSTD_Sequence
    [StructLayout(LayoutKind.Sequential)]
    internal class Sequence
    {
        /// <summary>
        /// Match pos in dst
        /// </summary>
        public uint MatchPos;
        /// <summary>
        /// If seqDef.offset > 3, then this is seqDef.offset - 3
        /// If seqDef.offset < 3, then this is the corresponding repeat offset
        /// But if seqDef.offset < 3 and litLength == 0, this is the
        ///   repeat offset before the corresponding repeat offset
        /// And if seqDef.offset == 3 and litLength == 0, this is the
        ///  most recent repeat offset - 1 
        /// </summary>
        public uint Offset;
        /// <summary>
        /// Literal length
        /// </summary>
        public uint LitLength;
        /// <summary>
        /// Match length
        /// </summary>
        public uint MatchLength;
        /// <summary>
        /// 0 when seq not rep and seqDef.offset otherwise
        /// when litLength == 0 this will be <= 4, otherwise <= 3 like normal 
        /// </summary>
        public uint Rep;
    }
    #endregion

    #region (internal) class CompressionParameters
    // ZSTD_compressionParameters
    [StructLayout(LayoutKind.Sequential)]
    internal class CompressionParameters
    {
        /// <summary>
        /// largest match distance : larger == more compression, more memory needed during decompression
        /// </summary>
        public uint WindowLog;
        /// <summary>
        /// fully searched segment : larger == more compression, slower, more memory (useless for fast)
        /// </summary>
        public uint ChainLog;
        /// <summary>
        /// dispatch table : larger == faster, more memory
        /// </summary>
        public uint HashLog;
        /// <summary>
        /// nb of searches : larger == more compression, slower
        /// </summary>
        public uint SearchLog;
        /// <summary>
        /// match length searched : larger == faster decompression, sometimes less compression
        /// </summary>
        public uint MinMatch;
        /// <summary>
        /// acceptable match size for optimal parser (only) : larger == more compression, slower
        /// </summary>
        public uint TargetLength;
        /// <summary>
        /// see ZSTD_strategy definition above
        /// </summary>
        public Strategy Strategy;
    }
    #endregion

    #region (internal) class FrameParameters
    // ZSTD_frameParameters
    [StructLayout(LayoutKind.Sequential)]
    internal class FrameParameters
    {
        /// <summary>
        /// 1: content size will be in frame header (when known)
        /// </summary>
        public int contentSizeFlag;
        /// <summary>
        /// 1: generate a 32-bits checksum using XXH64 algorithm at end of frame, for error detection
        /// </summary>
        public int checksumFlag;
        /// <summary>
        /// 1: no dictID will be saved into frame header (dictID is only useful for dictionary compression)
        /// </summary>
        public int noDictIDFlag;
    }
    #endregion
}
