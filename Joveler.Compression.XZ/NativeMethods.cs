/*
    Derived from liblzma header files (Public Domain)

    C# Wrapper written by Hajin Jang
    Copyright (C) 2018-2019 Hajin Jang

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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable InconsistentNaming

namespace Joveler.Compression.XZ
{
    #region NativeMethods
    internal static class NativeMethods
    {
        #region Const
        internal const string MsgInitFirstError = "Please call XZInit.GlobalInit() first!";
        internal const string MsgAlreadyInit = "Joveler.Compression.XZ is already initialized.";
        #endregion

        #region Native Library Loading
        internal static IntPtr hModule = IntPtr.Zero;
        internal static readonly object LoadLock = new object();
        private static bool Loaded => hModule != IntPtr.Zero;

        public static bool IsLoaded()
        {
            lock (LoadLock)
            {
                return Loaded;
            }
        }

        public static void EnsureLoaded()
        {
            lock (LoadLock)
            {
                if (!Loaded)
                    throw new InvalidOperationException(MsgInitFirstError);
            }
        }

        public static void EnsureNotLoaded()
        {
            lock (LoadLock)
            {
                if (Loaded)
                    throw new InvalidOperationException(MsgAlreadyInit);
            }
        }
        #endregion

        #region Windows kernel32 API
        internal static class Win32
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            [SuppressMessage("Globalization", "CA2101:Specify marshaling for P/Invoke string arguments")]
            internal static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

            [DllImport("kernel32.dll")]
            internal static extern int FreeLibrary(IntPtr hModule);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern int SetDllDirectory([MarshalAs(UnmanagedType.LPWStr)] string lpPathName);
        }
        #endregion

        #region Linux libdl API
#pragma warning disable IDE1006 // 명명 스타일
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        internal static class Posix
        {
            internal const int RTLD_NOW = 0x0002;
            internal const int RTLD_GLOBAL = 0x0100;

            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr dlopen(string fileName, int flags);

            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int dlclose(IntPtr handle);

            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
            internal static extern string dlerror();
            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr dlsym(IntPtr handle, string symbol);
        }
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
#pragma warning restore IDE1006 // 명명 스타일
        #endregion

        #region GlobalInit, GlobalCleanup
        internal static void GlobalInit(string libPath)
        {
            lock (LoadLock)
            {
                if (Loaded)
                    throw new InvalidOperationException(MsgAlreadyInit);

#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                {
                    if (libPath == null)
                        throw new ArgumentNullException(nameof(libPath));

                    libPath = Path.GetFullPath(libPath);
                    if (!File.Exists(libPath))
                        throw new ArgumentException("Specified .dll file does not exist");

                    // Set proper directory to search, unless LoadLibrary can fail when loading chained dll files.
                    string libDir = Path.GetDirectoryName(libPath);
                    if (libDir != null && !libDir.Equals(AppDomain.CurrentDomain.BaseDirectory))
                        Win32.SetDllDirectory(libDir);
                    // SetDllDictionary guard
                    try
                    {
                        hModule = Win32.LoadLibrary(libPath);
                        if (hModule == IntPtr.Zero)
                            throw new ArgumentException($"Unable to load [{libPath}]", new Win32Exception());
                    }
                    finally
                    {
                        // Reset dll search directory to prevent dll hijacking
                        Win32.SetDllDirectory(null);
                    }

                    // Check if dll is valid (liblzma.dll)
                    if (Win32.GetProcAddress(hModule, nameof(lzma_version_number)) == IntPtr.Zero)
                    {
                        GlobalCleanup();
                        throw new ArgumentException($"[{libPath}] is not a valid lzma library");
                    }
                }
#if !NET451
                else
                {
                    if (libPath == null)
                        libPath = "/lib/x86_64-linux-gnu/liblzma.so.5"; // Try to call system-installed xz-utils
                    if (!File.Exists(libPath))
                        throw new ArgumentException("Specified .so file does not exist");

                    hModule = Posix.dlopen(libPath, Posix.RTLD_NOW | Posix.RTLD_GLOBAL);
                    if (hModule == IntPtr.Zero)
                        throw new ArgumentException($"Unable to load [{libPath}], {Posix.dlerror()}");

                    // Check if dll is valid (liblzma.dll)
                    if (Posix.dlsym(hModule, nameof(lzma_version_number)) == IntPtr.Zero)
                    {
                        GlobalCleanup();
                        throw new ArgumentException($"[{libPath}] is not a valid lzma library");
                    }
                }
#endif

                // Load functions
                try
                {
                    LoadFunctions();
                }
                catch (Exception)
                {
                    GlobalCleanup();
                    throw;
                }
            }
        }

        internal static void GlobalCleanup()
        {
            lock (LoadLock)
            {
                if (!Loaded)
                    throw new InvalidOperationException(MsgInitFirstError);

                ResetFunctions();
#if !NET451
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                {
                    int ret = Win32.FreeLibrary(hModule);
                    Debug.Assert(ret != 0);
                }
#if !NET451
                else
                {
                    int ret = Posix.dlclose(hModule);
                    Debug.Assert(ret == 0);
                }
#endif

                hModule = IntPtr.Zero;
            }
        }
        #endregion

        #region GetFuncPtr
        private static T GetFuncPtr<T>(string funcSymbol) where T : Delegate
        {
            IntPtr funcPtr;
#if !NET451
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                funcPtr = Win32.GetProcAddress(hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new InvalidOperationException($"Cannot import [{funcSymbol}]", new Win32Exception());
            }
#if !NET451
            else
            {
                funcPtr = Posix.dlsym(hModule, funcSymbol);
                if (funcPtr == IntPtr.Zero)
                    throw new InvalidOperationException($"Cannot import [{funcSymbol}], {Posix.dlerror()}");
            }
#endif

            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }
        #endregion

        #region LoadFunctions, ResetFunctions
        internal static void LoadFunctions()
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
            #endregion

            #region Hardware - PhyMem & CPU Threads
            LzmaPhysMem = GetFuncPtr<lzma_physmem>(nameof(lzma_physmem));
            LzmaCpuThreads = GetFuncPtr<lzma_cputhreads>(nameof(lzma_cputhreads));
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

        internal static void ResetFunctions()
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
            #endregion

            #region Hardware - PhyMem & CPU Threads
            LzmaPhysMem = null;
            LzmaCpuThreads = null;
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
        internal static lzma_code LzmaCode;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void lzma_end(LzmaStream strm);
        internal static lzma_end LzmaEnd;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void lzma_get_progress(
            LzmaStream strm,
            ref ulong progress_in,
            ref ulong progress_out);
        internal static lzma_get_progress LzmaGetProgress;
        #endregion

        #region Container - Encoders and Decoders
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_easy_encoder_memusage(uint preset);
        internal static lzma_easy_encoder_memusage LzmaEasyEncoderMemUsage;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_easy_decoder_memusage(uint preset);
        internal static lzma_easy_decoder_memusage LzmaEasyDecoderMemUsage;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_easy_encoder(
            LzmaStream strm,
            uint preset,
            LzmaCheck check);
        internal static lzma_easy_encoder LzmaEasyEncoder;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_stream_encoder(
            LzmaStream strm,
            [MarshalAs(UnmanagedType.LPArray)] LzmaFilter[] filters,
            LzmaCheck check);
        internal static lzma_stream_encoder LzmaStreamEncoder;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_stream_encoder_mt_memusage(LzmaMt options);
        internal static lzma_stream_encoder_mt_memusage LzmaStreamEncoderMtMemUsage;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LzmaRet lzma_stream_encoder_mt(
            LzmaStream strm,
            LzmaMt options);
        internal static lzma_stream_encoder_mt LzmaStreamEncoderMt;

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
        internal static lzma_stream_decoder LzmaStreamDecoder;
        #endregion

        #region Hardware - PhyMem & CPU Threads
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate ulong lzma_physmem();
        internal static lzma_physmem LzmaPhysMem;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint lzma_cputhreads();
        internal static lzma_cputhreads LzmaCpuThreads;
        #endregion

        #region Check - Crc32, Crc64
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate uint lzma_crc32(
            byte* buf,
            UIntPtr size, // size_t
            uint crc);
        internal static lzma_crc32 LzmaCrc32;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate ulong lzma_crc64(
            byte* buf,
            UIntPtr size, // size_t
            ulong crc);
        internal static lzma_crc64 LzmaCrc64;
        #endregion

        #region Version - LzmaVersionNumber, LzmaVersionString
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint lzma_version_number();
        internal static lzma_version_number LzmaVersionNumber;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr lzma_version_string();
        internal static lzma_version_string LzmaVersionString;
        #endregion
        #endregion
    }
    #endregion
}
