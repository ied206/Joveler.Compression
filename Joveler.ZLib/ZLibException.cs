/*
    Derived from zlib header files (zlib license)
    Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

    C# Wrapper based on zlibnet v1.3.3 (https://zlibnet.codeplex.com/)
    Copyright (C) @hardon (https://www.codeplex.com/site/users/view/hardon)
    Copyright (C) 2017-2018 Hajin Jang

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

namespace Joveler.ZLib
{
    #region ZLibException
    public class ZLibException : Exception
    {
        public ZLibReturnCode ErrorCode;

        public ZLibException(ZLibReturnCode errorCode)
            : base(ForgeErrorMessage(errorCode))
        {
            ErrorCode = errorCode;
        }

        public ZLibException(ZLibReturnCode errorCode, string msg)
            : base(ForgeErrorMessage(errorCode, msg))
        {
            ErrorCode = errorCode;
        }

        private static string ForgeErrorMessage(ZLibReturnCode errorCode, string msg = null)
        {
            return msg == null ? $"[{errorCode}]" : $"[{errorCode}] {msg}";
        }

        internal static void CheckZLibRetOk(ZLibReturnCode ret, ZStreamL32 zs = null)
        {
            if (ret != ZLibReturnCode.OK)
            {
                if (zs == null)
                    throw new ZLibException(ret);
                else
                    throw new ZLibException(ret, zs.LastErrorMsg);
            }
        }

        internal static void CheckZLibRetOk(ZLibReturnCode ret, ZStreamL64 zs = null)
        {
            if (ret != ZLibReturnCode.OK)
            {
                if (zs == null)
                    throw new ZLibException(ret);
                else
                    throw new ZLibException(ret, zs.LastErrorMsg);
            }
        }

        internal static void CheckZLibRetError(ZLibReturnCode ret, ZStreamL32 zs = null)
        {
            if (ret < 0)
            {
                if (zs == null)
                    throw new ZLibException(ret);
                else
                    throw new ZLibException(ret, zs.LastErrorMsg);
            }
        }

        internal static void CheckZLibRetError(ZLibReturnCode ret, ZStreamL64 zs = null)
        {
            if (ret < 0)
            {
                if (zs == null)
                    throw new ZLibException(ret);
                else
                    throw new ZLibException(ret, zs.LastErrorMsg);
            }
        }
    }
    #endregion
}
