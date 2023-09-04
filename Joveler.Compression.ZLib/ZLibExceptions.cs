/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

    C# Wrapper based on zlibnet v1.3.3 (https://zlibnet.codeplex.com/)
    Copyright (C) @hardon (https://www.codeplex.com/site/users/view/hardon)
    
    Maintained by Hajin Jang
    Copyright (C) 2017-present Hajin Jang

    zlib license

    This software is provided 'as-is', without any express or implied
    warranty.  In no event will the authors be held liable for any damages
    arising from the use of this software.

    Permission is granted to anyone to use this software for any purpose,
    including commercial applications, and to alter it and redistribute it
    freely, subject to the following restrictions:

    1. The origin of this software must not be misrepresented; you must not
       claim that you wrote the original software. If you use this software
       in a product, an acknowledgment in the product documentation would be
       appreciated but is not required.
    2. Altered source versions must be plainly marked as such, and must not be
       misrepresented as being the original software.
    3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Runtime.Serialization;

namespace Joveler.Compression.ZLib
{
    #region ZLibException
    [Serializable]
    public class ZLibException : Exception
    {
        public ZLibRet ReturnCode { get; set; }

        public ZLibException(ZLibRet errorCode)
            : base(ForgeErrorMessage(errorCode))
        {
            ReturnCode = errorCode;
        }

        public ZLibException(ZLibRet errorCode, string msg)
            : base(ForgeErrorMessage(errorCode, msg))
        {
            ReturnCode = errorCode;
        }

        private static string ForgeErrorMessage(ZLibRet errorCode, string msg = null)
        {
            return msg == null ? $"[{errorCode}]" : $"[{errorCode}] {msg}";
        }

        internal static void CheckReturnValue(ZLibRet ret, ZStreamBase zs = null)
        {
            if (ret != ZLibRet.Ok)
            {
                if (zs == null)
                    throw new ZLibException(ret);
                else
                    throw new ZLibException(ret, zs.LastErrorMsg);
            }
        }

        #region Serializable
        protected ZLibException(SerializationInfo info, StreamingContext ctx)
        {
            ReturnCode = (ZLibRet)info.GetValue(nameof(ReturnCode), typeof(ZLibRet));
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
    #endregion

    
}
