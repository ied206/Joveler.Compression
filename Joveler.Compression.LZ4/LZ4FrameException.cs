/*
    Derived from LZ4 header files (BSD 2-Clause)
    Copyright (c) 2011-2016, Yann Collet

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2020 Hajin Jang

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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Joveler.Compression.LZ4
{
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class LZ4FrameException : Exception
    {
        public ulong ReturnCode { get; set; }

        private static string FrameGetErrorName(UIntPtr code)
        {
            LZ4Init.Manager.EnsureLoaded();

            IntPtr strPtr = LZ4Init.Lib.GetErrorName(code);
            return Marshal.PtrToStringAnsi(strPtr);
        }

        public LZ4FrameException(UIntPtr code) : base(FrameGetErrorName(code))
        {
            ReturnCode = code.ToUInt64();
        }

        public static void CheckReturnValue(UIntPtr code)
        {
            LZ4Init.Manager.EnsureLoaded();

            if (LZ4Init.Lib.FrameIsError(code) != 0)
                throw new LZ4FrameException(code);
        }

        #region Serializable
        protected LZ4FrameException(SerializationInfo info, StreamingContext ctx)
        {
            ReturnCode = info.GetUInt64(nameof(ReturnCode));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            info.AddValue(nameof(ReturnCode), ReturnCode);
            base.GetObjectData(info, context);
        }
        #endregion
    }
}
