﻿/*
    Derived from Zstandard header files (BSD 2-Clause)
    Copyright (c) 2016-present, Yann Collet, Facebook, Inc. All rights reserved.

    C# Wrapper written by Hajin Jang
    Copyright (C) 2020-present Hajin Jang

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
    #region ZstdException
    public class ZstdException : Exception
    {
        public nuint ReturnCode { get; set; }

        public ZstdException(nuint code)
            : base(ForgeErrorMessage(code))
        {
            ReturnCode = code;
        }

        public ZstdException(nuint code, Exception innerException)
            : base(ForgeErrorMessage(code), innerException)
        {
            ReturnCode = code;
        }

        public ZstdException(nuint code, string msg)
            : base(msg)
        {
            ReturnCode = code;
        }

        public ZstdException(nuint code, string msg, Exception innerException)
            : base(msg, innerException)
        {
            ReturnCode = code;
        }

        private static string ForgeErrorMessage(nuint code, string? msg = null)
        {
            if (ZstdInit.Lib == null)
                throw new ObjectDisposedException(nameof(ZstdInit));

            if (msg == null)
            {
                // ZSTD_getErrorName returns const char*, which does not require freeing string.
                IntPtr strPtr = ZstdInit.Lib.GetErrorName!(code);
                msg = Marshal.PtrToStringAnsi(strPtr);
            }
            return msg ?? $"ZSTD Unknown Error";
        }

        internal static void CheckReturnValue(nuint code)
        {
            ZstdInit.Manager.EnsureLoaded();

            if (ZstdInit.Lib == null)
                throw new ObjectDisposedException(nameof(ZstdInit));

            if (ZstdInit.Lib.IsError!(code) != 0)
                throw new ZstdException(code);
        }

        internal static void CheckReturnValueWithCStream(nuint code, IntPtr cstream)
        {
            ZstdInit.Manager.EnsureLoaded();

            if (ZstdInit.Lib == null)
                throw new ObjectDisposedException(nameof(ZstdInit));

            if (ZstdInit.Lib.IsError!(code) != 0)
            {
                nuint resetCode = ZstdInit.Lib.CCtxReset!(cstream, ResetDirective.ResetSessionOnly);
                if (ZstdInit.Lib.IsError(resetCode) != 0)
                    throw new ZstdException(code, new ZstdException(resetCode));
                else
                    throw new ZstdException(code);
            }
        }

        internal static void CheckReturnValueWithDStream(nuint code, IntPtr dstream)
        {
            ZstdInit.Manager.EnsureLoaded();

            if (ZstdInit.Lib == null)
                throw new ObjectDisposedException(nameof(ZstdInit));

            if (ZstdInit.Lib.IsError!(code) != 0)
            {
                nuint resetCode = ZstdInit.Lib.DctxReset!(dstream, ResetDirective.ResetSessionOnly);
                if (ZstdInit.Lib.IsError(resetCode) != 0)
                    throw new ZstdException(code, new ZstdException(resetCode));
                else
                    throw new ZstdException(code);
            }
        }
    }
    #endregion
}
