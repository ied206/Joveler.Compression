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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/*
 * This file includes definition from external C library.
 * Should suppress error and warning from code analyzer, due to this file's C-style naming.
 */
#pragma warning disable 169

namespace Joveler.Compression.LZ4
{
    #region (internal) struct FrameInfo
    /// <summary>
    /// makes it possible to set or read frame parameters.
    /// It's not required to set all fields, as long as the structure was initially memset() to zero.
    /// For all fields, 0 sets it to default value
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct FrameInfo
    {
        /// <summary>
        /// max64KB, max256KB, max1MB, max4MB; 0 == default (LZ4F_max64KB)
        /// </summary>
        public FrameBlockSizeId BlockSizeId = FrameBlockSizeId.Max64KB;
        /// <summary>
        /// LZ4F_blockLinked, LZ4F_blockIndependent; 0 == default (LZ4F_blockLinked)
        /// </summary>
        public FrameBlockMode BlockMode = FrameBlockMode.BlockLinked;
        /// <summary>
        /// 1: add a 32-bit checksum of frame's decompressed data; 0 == default (disabled)
        /// </summary>
        public FrameContentChecksum ContentChecksumFlag = FrameContentChecksum.NoContentChecksum;
        /// <summary>
        /// read-only field : LZ4F_frame or LZ4F_skippableFrame
        /// </summary>
        public FrameType FrameType = FrameType.Frame;
        /// <summary>
        /// Size of uncompressed content ; 0 == unknown
        /// </summary>
        public ulong ContentSize = 0;
        /// <summary>
        /// Dictionary ID, sent by the compressor to help decoder select the correct dictionary; 0 == no dictID provided
        /// </summary>
        public uint DictId = 0;
        /// <summary>
        /// 1: each block followed by a checksum of block's compressed data; 0 == default (disabled)
        /// </summary>
        public FrameBlockChecksum BlockChecksumFlag = FrameBlockChecksum.NoBlockChecksum;

        public FrameInfo()
        {

        }

        public FrameInfo(FrameBlockSizeId blockSizeId, FrameBlockMode blockMode, FrameContentChecksum contentChecksumFlag,
            FrameType frameType, ulong contentSize, uint dictId, FrameBlockChecksum blockChecksumFlag)
        {
            BlockSizeId = blockSizeId;
            BlockMode = blockMode;
            ContentChecksumFlag = contentChecksumFlag;
            FrameType = frameType;
            ContentSize = contentSize;
            DictId = dictId;
            BlockChecksumFlag = blockChecksumFlag;
        }
    }
    #endregion

    #region (internal) class FramePreferences
    /// <summary>
    /// makes it possible to supply detailed compression parameters to the stream interface.
    /// It's not required to set all fields, as long as the structure was initially memset() to zero.
    /// All reserved fields must be set to zero.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class FramePreferences
    {
        public FrameInfo FrameInfo;
        /// <summary>
        ///  0 == default (fast mode); values above LZ4HC_CLEVEL_MAX count as LZ4HC_CLEVEL_MAX; values below 0 trigger "fast acceleration", proportional to value
        /// </summary>
        public LZ4CompLevel CompressionLevel;
        /// <summary>
        /// 1 == always flush, to reduce usage of internal buffers
        /// </summary>
        public uint AutoFlush;
        /// <summary>
        /// 1 == parser favors decompression speed vs compression ratio. Only works for high compression modes (>= LZ4HC_CLEVEL_OPT_MIN)
        /// </summary>
        /// <remarks>
        /// v1.8.2+
        /// </remarks>
        public uint FavorDecSpeed;
        /// <summary>
        /// must be zero for forward compatibility
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        private readonly uint[] _reserved = new uint[3];
    }
    #endregion

    #region (internal) class FrameCompressOptions
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class FrameCompressOptions
    {
        /// <summary>
        ///  1 == src content will remain present on future calls to LZ4F_compress(); skip copying src content within tmp buffer
        /// </summary>
        public uint StableSrc;
        /// <summary>
        /// must be zero for forward compatibility
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        private readonly uint[] _reserved = [0, 0, 0];
    }
    #endregion

    #region (internal) class FrameDecompressOptions
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class FrameDecompressOptions
    {
        /// <summary>
        /// pledges that last 64KB decompressed data will remain available unmodified between invocations.
        /// This optimization skips storage operations in tmp buffers.
        /// </summary>
        public uint StableDst;
        /// <summary>
        /// disable checksum calculation and verification, even when one is present in frame, to save CPU time.
        /// Setting this option to 1 once disables all checksums for the rest of the frame.
        /// </summary>
        public uint SkipChecksums;
        /// <summary>
        /// must be set to zero for forward compatibility
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        private readonly uint[] _reserved = [0, 0];
    }
    #endregion

    #region (public) enum FrameBlockSizeId
    /// <summary>
    /// The larger the block size, the (slightly) better the compression ratio, though there are diminishing returns.
    /// Larger blocks also increase memory usage on both compression and decompression sides.
    /// </summary>
    public enum FrameBlockSizeId : uint
    {
        Default = 0,
        Max64KB = 4,
        Max256KB = 5,
        Max1MB = 6,
        Max4MB = 7,
    }
    #endregion

    #region (public) enum FrameBlockMode
    /// <summary>
    /// Linked blocks sharply reduce inefficiencies when using small blocks, they compress better.
    /// However, some LZ4 decoders are only compatible with independent blocks.
    /// </summary>
    public enum FrameBlockMode : uint
    {
        BlockLinked = 0,
        BlockIndependent = 1,
    }
    #endregion

    #region (public) enum FrameContentChecksum
    public enum FrameContentChecksum : uint
    {
        NoContentChecksum = 0,
        ContentChecksumEnabled = 1,
    }
    #endregion

    #region (public) enum FrameBlockChecksum
    public enum FrameBlockChecksum : uint
    {
        NoBlockChecksum = 0,
        BlockChecksumEnabled = 1,
    }
    #endregion

    #region (public) enum FrameType
    public enum FrameType : uint
    {
        Frame = 0,
        SkippableFrame = 1,
    }
    #endregion

    #region (public) enum LZ4CompLevel
    /// <summary>
    /// 0: default (fast mode), 0 through 2 is identical (default). 3 ~ 12 : HC mode. 
    /// </summary>
    /// <remarks>
    /// values > 12 count as 12; values < 0 trigger "fast acceleration".
    /// </remarks>
    public enum LZ4CompLevel : int
    {
        /// <summary>
        /// Fast compression
        /// </summary>
        Default = 0,
        VeryFast = -1,
        Fast = 0,
        High = 9,
        VeryHigh = 12,
        Level0 = 0,
        Level1 = 1,
        Level2 = 2,
        /// <summary>
        /// LZ4HC_CLEVEL_MIN
        /// </summary>
        Level3 = 3,
        Level4 = 4,
        Level5 = 5,
        Level6 = 6,
        Level7 = 7,
        Level8 = 8,
        /// <summary>
        /// LZ4HC_CLEVEL_DEFAULT
        /// </summary>
        Level9 = 9,
        /// <summary>
        /// LZ4HC_CLEVEL_OPT_MIN
        /// </summary>
        Level10 = 10,
        Level11 = 11,
        /// <summary>
        /// LZ4HC_CLEVEL_MAX
        /// </summary>
        Level12 = 12,
    }
    #endregion

    #region 
    public enum XXHashErrorCode: int
    {
        Ok = 0,
        Error,
    }
    #endregion
}
