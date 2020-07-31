/*
    Derived from Zstandard header files (BSD 2-Clause)
    Copyright (c) 2016-present, Yann Collet, Facebook, Inc. All rights reserved.

    C# Wrapper written by Hajin Jang
    Copyright (C) 2020 Hajin Jang

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joveler.Compression.Zstd
{
    public class ZstdFrame
    {
        #region Magic Numbers
        /// <summary>
        /// valid since v0.8.0
        /// </summary>
        internal readonly byte[] MagicNumber = new byte[4] { 0x28, 0xB5, 0x2F, 0xFD };
        /// <summary>
        /// valid since v0.7.0
        /// </summary>
        internal readonly byte[] MagicDict = new byte[4] { 0x37, 0xA4, 0x30, 0xEC };
        /// <summary>
        /// all 16 values, from 0x184D2A50 to 0x184D2A5F, signal the beginning of a skippable frame
        /// </summary>
        internal readonly byte[] MagicSkippableStart = new byte[4] { 0x50, 0x2A, 0x4D, 0x18 };
        internal readonly byte[] MagicSkippableMask = new byte[4] { 0xF0, 0xFF, 0xFF, 0xFF };
        #endregion

        #region Const
        internal const int BlockSizeLogMax = 17;
        internal const int BlockSizeMax = 1 << BlockSizeLogMax;

        internal const long ContentSizeUnknown = 0L - 1;
        internal const long ContentSizeError = 0L - 2;
        #endregion

        #region Simple API
        #endregion
    }
}
