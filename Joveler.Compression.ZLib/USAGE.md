# Usage

## Initialization

Joveler.Compression.ZLib requires the explicit loading of a zlib library.

You must call `ZLibInit.GlobalInit()` before using Joveler.Compression.ZLib. 

- Call `ZLibInit.GlobalInit(string libPath, ZLibInitOptions opts)` to load embedded binaries.
    - If you don't know what you are doing, just follow the init code snippet.
- Call parameterless `ZLibInit.GlobalInit()` on Linux/macOS to load system zlib binary.
- Never call deprecated `ZLibInit.GlobalInit(string libPath)`.
    - This function now contains shim code to make Joveler.Compression.ZLib v5.x compatible with v4.x without any source code modification.
    - It translates `zlibwapi.dll` in path to `zlib1.dll` with cdecl ABI. 
    - To avoid ABI ambiguity, always call `ZLibInit.GlobalInit(string libPath, ZLibInitOptions opts)` directly.

### Init Code Snippet

Please put this code snippet in your application init code:

**WARNING**: Caller process and callee library must have the same architecture!

#### On .NET/.NET Core

```cs
public static void InitNativeLibrary()
{
    string libDir = "runtimes";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        libDir = Path.Combine(libDir, "win-");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        libDir = Path.Combine(libDir, "linux-");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        libDir = Path.Combine(libDir, "osx-");

    switch (RuntimeInformation.ProcessArchitecture)
    {
        case Architecture.X86:
            libDir += "x86";
            break;
        case Architecture.X64:
            libDir += "x64";
            break;
        case Architecture.Arm:
            libDir += "arm";
            break;
        case Architecture.Arm64:
            libDir += "arm64";
            break;
    }
    libDir = Path.Combine(libDir, "native");

    string libPath = null;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        libPath = Path.Combine(libDir, "zlib1.dll");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        libPath = Path.Combine(libDir, "libz.so");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        libPath = Path.Combine(libDir, "libz.dylib");

    if (libPath == null)
        throw new PlatformNotSupportedException($"Unable to find native library.");
    if (!File.Exists(libPath))
        throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

    ZLibInit.GlobalInit(libPath, new ZLibInitOptions());
}
```

#### On .NET Framework

```cs
public static void InitNativeLibrary()
{
    string arch = null;
    switch (RuntimeInformation.ProcessArchitecture)
    {
        case Architecture.X86:
            arch = "x86";
            break;
        case Architecture.X64:
            arch = "x64";
            break;
        case Architecture.Arm64:
            arch = "arm64";
            break;
    }
    string libPath = Path.Combine(arch, "zlib1.dll");

    if (!File.Exists(libPath))
        throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

    ZLibInit.GlobalInit(libPath, new ZLibInitOptions());
}
```

### Embedded binary

Joveler.Compression.ZLib comes with sets of static binaries of `zlib-ng 2.2.3 (compat ABI)`. They will be copied into the build directory at build time.

#### On .NET/.NET Core & .NET Standard

| Platform      | Minimum Target | Binary                                    | License | C Runtime     |
|---------------|----------------|-------------------------------------------|---------|---------------|
| Windows x86   | Windows 7 SP1  | `$(OutDir)\runtimes\win-x86\zlib1.dll`    | zlib    | Universal CRT |
| Windows x64   | Windows 7 SP1  | `$(OutDir)\runtimes\win-x64\zlib1.dll`    | zlib    | Universal CRT |
| Windows arm64 | Windows 7 SP1  | `$(OutDir)\runtimes\win-arm64\zlib1.dll`  | zlib    | Universal CRT |
| Linux x64     | Ubuntu 20.04   | `$(OutDir)\runtimes\linux-x64\libz.so`    | zlib    | glibc         |
| Linux armhf   | Ubuntu 20.04   | `$(OutDir)\runtimes\linux-arm\libz.so`    | zlib    | glibc         |
| Linux arm64   | Ubuntu 20.04   | `$(OutDir)\runtimes\linux-arm64\libz.so`  | zlib    | glibc         |
| macOS x64     | macOS 11       | `$(OutDir)\runtimes\osx-x64\libz.dylib`   | zlib    | libSystem     |
| macOS arm64   | macOS 11       | `$(OutDir)\runtimes\osx-arm64\libz.dylib` | zlib    | libSystem     |

- Precompiled binaries were built from zlib-ng in compat mode, which is compatible with traditional zlib cdecl ABI.
- Bundled Windows binaires targets [Universal CRT](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170) to ensure interoperability with modern .NET runtime.
    - If you encounter a dependency issue on Windows 7 or 8.1, try [installing UCRT manually](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170).
- If you call `ZLibInit.GlobalInit()` without `libPath` parameter on Linux or macOS, it will search for system-installed zlib.
- Linux binaries are not portable. They may not work on your distribution. In that case, call parameter-less `ZLibInit.GlobalInit()` to use system-installed zlib.

#### For .NET Framework 

| Platform         | Binary                      | License | C Runtime     |
|------------------|-----------------------------|---------|---------------|
| Windows x86      | `$(OutDir)\x86\zlib1.dll`   | zlib    | Universal CRT |
| Windows x64      | `$(OutDir)\x64\zlib1.dll`   | zlib    | Universal CRT |
| Windows arm64    | `$(OutDir)\arm64\zlib1.dll` | zlib    | Universal CRT |

- Create an empty file named `Joveler.Compression.ZLib.Precompiled.Exclude` in the project directory to prevent a copy of the package-embedded binary.
- Precompiled binaries were built from zlib-ng in compat mode, which is compatible with traditional zlib cdecl ABI.

#### Supported ABIs

Joveler.Compression.ZLib uses `zlib (cdecl)` ABI by default. Joveler.Compression.ZLib also supports `zlib (stdcall)` ABI and `zlib-ng (modern)` ABI.

`ZLibInitOptions` controls which ABI should be used to load native library. Default `ZLibInitOptions` instance is set to use default `zlib (cdecl)` ABI.

If you want to load a non-default ABI, tweak `ZLibInitOptions` properties. Set it to correct value, or it would crash the process in worst cases. 

```csharp
/// <summary>
/// Controls the ABI used to interface native library.
/// </summary>
/// <remarks>
/// Default values of ZLibInitOptions instance are tuned to load embedded zlib-ng compat binary.
/// </remarks>
public class ZLibInitOptions
{
    /// <summary>
    /// Does the native library have 'stdcall' calling convention? Set it to default unless you know what you are doing.
    /// <para>Set it to false for zlib1.dll (cdecl), and true for zlibwapi.dll (stdcall).</para>
    /// <para>This flag is effective only on Windows x86. Otherwise it will be ignored.</para>
    /// </summary>
    public bool IsWindowsStdcall { get; set; } = false;
    /// <summary>
    /// Does the naive library have zlib-ng 'modern' ABI? Set it to default unless you know what you are doing.
    /// <para>Set it to true only if you are loading one of 'zlib-ng2.dll', 'libz-ng.so' or 'libz-ng.dylib'.</para>
    /// <para>If the native library was built with zlib-ng 'compat' mode, set it to false.</para>
    /// </summary>
    public bool IsZLibNgModernAbi { get; set; } = false;
}
```

| ABI        | Calling Convention (x86) | FileNames                               | Note                          |
|------------|--------------------|-----------------------------------------------|-------------------------------|
| zlib       | cdecl              | `zlib1.dll`, `libz.so`, `libz.dylib`          | Default ABI                   |
| zlib       | stdcall            | `zlibwapi.dll`                                | Effective only on Windows x86 |
| zlib-ng    | cdecl              | `zlib-ng2.dll`, `libz-ng.so`, `libz-ng.dylib` | zlib-ng modern ABI            |

- `zlib` can be compiled into many ABIs due to its long history. 
- Its fork `zlib-ng` introduced its own ABI to avoid symbol conflict with long-standing `zlib` ABI.
- Calling convention difference is only effective on Windows x86.
    - Specifying to use `stdcall` on POSIX or Windows x64/arm64 would be ignored.

### Custom binary

To use custom zlib binary instead, call `ZLibInit.GlobalInit(string libPath, ZLibInitOptions opts)` with a path to the custom binary.

You are required to pass valid `ZLibInitOptions` instance alongside library path.

### Cleanup

To unload the zlib library explicitly, call `ZLibInit.GlobalCleanup()` or `ZLibInit.TryGlobalCleanup()`.

## DeflateStream

`DeflateStream` is the class that processes a data format conforming to [RFC 1951](https://www.ietf.org/rfc/rfc1951.txt).

### Constructor

```csharp
// Create a compressing DeflateStream instance
public DeflateStream(Stream baseStream, ZLibCompressOptions compOpts)
// Create a parallel compressing DeflateStream instance (EXPERIMENTAL)
public DeflateStream(Stream baseStream, ZLibCompressOptions compOpts, ZLibParallelCompressOptions pcompOpts)
// Create a decompressing DeflateStream instance
public DeflateStream(Stream baseStream, ZLibDecompressOptions decompOpts)
```

#### ZLibCompressOptions

You can tune zlib compress options with this class.

| Property | Summary |
|----------|---------|
| Level | Compression level. The Default is `ZLibCompLevel.Default`. |
| BufferSize | Size of the internal buffer. The default is 256KB. This value is ignored in parallel compression, and use ChunkSize instead. |
| LeaveOpen | Whether to leave the base stream object open after disposing of the zlib stream object. |

It also contains more advanced options.

#### ZLibParallelCompressOptions **(EXPERIMENTAL)**

| Property | Summary |
|----------|---------|
| Threads     | The number of threads to use for parallel compression. |
| ChunkSize   | Size of the compress chunk, which would be a unit of data to be compressed per thread. |
| WaitTimeout | Controls timeout to allow Write() to return early. Set to null to block until compress & write is complete. Set to TimeSpan value to enable an upper limit on blocking. Timeout value is kept as best effort. |

#### ZLibDecompressOptions

You can tune zlib decompress options with this class.

| Property | Summary |
|----------|---------|
| BufferSize | Size of the internal buffer. The default is 256KB. |
| LeaveOpen | Whether to leave the base stream object open after disposing of the zlib stream object. |

### Examples

#### Compress a file into deflate stream format

```csharp
using Joveler.Compression.ZLib;

ZLibCompressOptions compOpts = new ZLibCompressOptions()
{
    Level = ZLibCompLevel.Default,
};

using (FileStream fsOrigin = new FileStream("file_origin.bin", FileMode.Open))
using (FileStream fsComp = new FileStream("test.deflate", FileMode.Create))
using (DeflateStream zs = new DeflateStream(fsComp, compOpts))
{
    fsOrigin.CopyTo(zs);
}
```

#### Compress a file into deflate stream format, in parallel

```csharp
using Joveler.Compression.ZLib;

ZLibCompressOptions compOpts = new ZLibCompressOptions()
{
    Level = ZLibCompLevel.Default,
};
ZLibParallelCompressOptions pcompOpts = new ZLibParallelCompressOptions()
{
    Threads = Environment.ProcessorCount,
};

using (FileStream fsOrigin = new FileStream("file_origin.bin", FileMode.Open))
using (FileStream fsComp = new FileStream("test.deflate", FileMode.Create))
using (DeflateStream zs = new DeflateStream(fsComp, compOpts, pcompOpts))
{
    fsOrigin.CopyTo(zs);
}
```

#### Decompress a file from deflate stream format

```csharp
using Joveler.Compression.ZLib;

ZLibDecompressOptions decompOpts = new ZLibDecompressOptions();

using (FileStream fsComp = new FileStream("test.deflate", FileMode.Create))
using (FileStream fsDecomp = new FileStream("file_decomp.bin", FileMode.Open))
using (DeflateStream zs = new DeflateStream(fsComp, decompOpts))
{
    zs.CopyTo(fsDecomp);
}
```

## ZLibStream

`ZLibStream` is the class that processes a data format conforming to [RFC 1950](https://www.ietf.org/rfc/rfc1950.txt).

The zlib data format has its simple header and adler32 footer.

It has an interface same as `DeflateStream`.

## GZipStream

`GZipStream` is the class that processes a data format conforming to [RFC 1952](https://www.ietf.org/rfc/rfc1952.txt).

Use this stream to handle `.gz` files.

It has an interface same as `DeflateStream`.

## Adler32Checksum

`Adler32Checksum` is the class to compute Adler32 checksum.

Use `Append()` methods to compute the checksum.  
Use `Checksum` property to get checksum value.
Use `Reset()` methods to reset `Checksum` property.

### Examples

#### `Append(ReadOnlySpan<byte> buffer)`, `Append(byte[] buffer, int offset, int count)`

```cs
using Joveler.Compression.ZLib.Checksum;

Adler32Checksum adler = new Adler32Checksum();
byte[] bin = Encoding.UTF8.GetBytes("ABCDEF");

// Append(ReadOnlySpan<byte> buffer)
adler.Append(bin.AsSpan(2, 3));
Console.WriteLine($"0x{adler.Checksum:X8}");

// Append(byte[] buffer, int offset, int count)
adler.Reset();
adler.Append(bin, 2, 3);
Console.WriteLine($"0x{adler.Checksum:X8}");
```

#### `Append(Stream stream)`

```csharp
using Joveler.Compression.ZLib.Checksum;

using (FileStream fs = new FileStream("read.txt", FileMode.Open))
{
    Adler32Checksum adler = new Adler32Checksum();

    // Append(Stream stream)
    adler.Append(fs);
    Console.WriteLine($"0x{adler.Checksum:X8}");
}
```

## Adler32Algorithm

`Adler32Algorithm` is the class designed to compute Adler32 checksum.

It inherits and implements [HashAlgorithm](https://docs.microsoft.com/en-US/dotnet/api/system.security.cryptography.hashalgorithm).

## Crc32Checksum

`Crc32Checksum` is the class to compute CRC32 checksum.

It has the same usage as `Adler32Checksum`.

## Crc32Algorithm

`Crc32Algorithm` is the class designed to compute CRC32 checksum.

It inherits and implements [HashAlgorithm](https://docs.microsoft.com/en-US/dotnet/api/system.security.cryptography.hashalgorithm).
