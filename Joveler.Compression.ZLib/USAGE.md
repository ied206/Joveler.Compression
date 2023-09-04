# Usage

## Initialization

Joveler.Compression.ZLib requires the explicit loading of a zlib library.

### Init Code Snippet

You must call `ZLibInit.GlobalInit()` before using Joveler.Compression.ZLib. Please put this code snippet in your application init code:

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
        case Architecture.Arm:
            arch = "armhf";
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

Joveler.Compression.ZLib comes with sets of static binaries of `zlib-ng 2.1.3 (compat ABI)`. They will be copied into the build directory at build time.

#### On .NET/.NET Core & .NET Standard

| Platform              | Binary                                    | License       | C Runtime     |
|-----------------------|-------------------------------------------|---------------|---------------|
| Windows x86           | `$(OutDir)\runtimes\win-x86\zlib1.dll`    | Public Domain | Universal CRT |
| Windows x64           | `$(OutDir)\runtimes\win-x64\zlib1.dll`    | Public Domain | Universal CRT |
| Windows arm64         | `$(OutDir)\runtimes\win-arm64\zlib1.dll`  | Public Domain | Universal CRT |
| Ubuntu 20.04 x64      | `$(OutDir)\runtimes\linux-x64\libz.so`    | Public Domain | glibc         |
| Debian 12 armhf       | `$(OutDir)\runtimes\linux-arm\libz.so`    | Public Domain | glibc         |
| Debian 12 arm64       | `$(OutDir)\runtimes\linux-arm64\libz.so`  | Public Domain | glibc         |
| macOS Big Sur x64     | `$(OutDir)\runtimes\osx-x64\libz.dylib`   | Public Domain | libSystem     |
| macOS Ventura arm64   | `$(OutDir)\runtimes\osx-arm64\libz.dylib` | Public Domain | libSystem     |

- Precompiled binaries were built from zlib-ng in compat mode, which is compatible with traditional zlib cdecl ABI.
- Bundled Windows binaires targets [Universal CRT](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170) to ensure interoperability with modern .NET runtime.
    - If you encounter a dependency issue on Windows Vista, 7 or 8.1, try [installing UCRT manually](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170).
- If you call `ZLibInit.GlobalInit()` without `libPath` parameter on Linux or macOS, it will search for system-installed zlib.
- Linux binaries are not portable. They may not work on your distribution. In that case, call parameter-less `ZLibInit.GlobalInit()` to use system-installed zlib.

#### For .NET Framework 

| Platform              | Binary                                    | License       | C Runtime     |
|-----------------------|-------------------------------------------|---------------|---------------|
| Windows x86           | `$(OutDir)\runtimes\win-x86\zlib1.dll`    | Public Domain | Universal CRT |
| Windows x64           | `$(OutDir)\runtimes\win-x64\zlib1.dll`    | Public Domain | Universal CRT |
| Windows arm64         | `$(OutDir)\runtimes\win-arm64\zlib1.dll`  | Public Domain | Universal CRT |

- Create an empty file named `Joveler.Compression.ZLib.Precompiled.Exclude` in the project directory to prevent a copy of the package-embedded binary.
- Precompiled binaries were built from zlib-ng in compat mode, which is compatible with traditional zlib cdecl ABI.

### Custom binary

To use custom zlib binary instead, call `ZLibInit.GlobalInit()` with a path to the custom binary.

You are required to pass valid `ZLibInitOptions` instance alongside library path. It controls which ABI would be used to load native library.

#### Supported ABIs

Joveler.Compression.ZLib supports both traditional zlib ABIs, and also its modern counterpart zlib-ng ABI.

Joveler.Compression.ZLib uses `zlib (cdecl)` ABI by default. If you want to load other ABI, tweak `ZLibInitOptions` which would be passed into `ZLibInit.GlobalInit()`.

| ABI        | Calling Convention | FileNames                                     | Note         |
|------------|--------------------|-----------------------------------------------|--------------|
| zlib       | cdecl/default      | `zlib1.dll`, `libz.so`, `libz.dylib`          | Default ABI  |
| zlib       | stdcall            | `zlibwapi.dll`                                | Effective only on Windows x86 |
| zlib-ng    | cdecl/default      | `zlib-ng2.dll`, `libz-ng.so`, `libz-ng.dylib` |              |

- `zlib` can be compiled into many ABIs due to its long history. 
- Its fork `zlib-ng` introduced its own ABI to avoid symbol conflict with long-standing `zlib` ABI.
- Calling convention difference is only effective on Windows x86.
    - Specifying to use `stdcall` on POSIX or Windows x64/arm64 would be ignored.

### Cleanup

To unload the zlib library explicitly, call `ZLibInit.GlobalCleanup()`.

## DeflateStream

`DeflateStream` is the class that processes a data format conforming to [RFC 1951](https://www.ietf.org/rfc/rfc1951.txt).

### Constructor

```csharp
// Create a compressing DeflateStream instance
public DeflateStream(Stream baseStream, ZLibCompressOptions compOpts)
// Create a decompressing DeflateStream instance
public DeflateStream(Stream baseStream, ZLibDecompressOptions decompOpts)
```

#### ZLibCompressOptions

You can tune zlib compress options with this class.

| Property | Summary |
|----------|---------|
| Level | Compression level. The Default is `ZLibCompLevel.Default`. |
| BufferSize | Size of the internal buffer. The default is 256KB. |
| LeaveOpen | Whether to leave the base stream object open after disposing of the zlib stream object. |

It also contains more advanced options.

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

It has the same usage as `DeflateStream`.

## GZipStream

`GZipStream` is the class that processes a data format conforming to [RFC 1952](https://www.ietf.org/rfc/rfc1952.txt).

It has the same usage as `DeflateStream`.

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

## Crc32Checksum

`Crc32Checksum` is the class to compute CRC32 checksum.

It has the same usage as `Adler32Checksum`.

**NOTE**: xz-utils provides twice faster CRC32 implementation than zlib. Install [Joveler.Compression.XZ](https://www.nuget.org/packages/Joveler.Compression.XZ/) if you only need CRC32 calculation.

## Adler32Algorithm

`Adler32Algorithm` is the class designed to compute Adler32 checksum.

It inherits and implements [HashAlgorithm](https://docs.microsoft.com/en-US/dotnet/api/system.security.cryptography.hashalgorithm).

## Crc32Algorithm

`Crc32Algorithm` is the class designed to compute CRC32 checksum.

It inherits and implements [HashAlgorithm](https://docs.microsoft.com/en-US/dotnet/api/system.security.cryptography.hashalgorithm).
