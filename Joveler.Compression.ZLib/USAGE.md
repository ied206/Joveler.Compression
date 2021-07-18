# Usage

## Initialization

Joveler.Compression.ZLib requires the explicit loading of a zlib library.

You must call `ZLibInit.GlobalInit()` before using Joveler.Compression.ZLib. Please put this code snippet in your application init code:

#### For .NET Framework 4.5.1+

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
    string libPath = Path.Combine(arch, "zlibwapi.dll");

    if (!File.Exists(libPath))
        throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

    Magic.GlobalInit(libPath);
}
```

#### For .NET Standard 2.0+:

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
        libPath = Path.Combine(libDir, "zlibwapi.dll");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        libPath = Path.Combine(libDir, "libz.so");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        libPath = Path.Combine(libDir, "libz.dylib");

    if (libPath == null)
        throw new PlatformNotSupportedException($"Unable to find native library.");
    if (!File.Exists(libPath))
        throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

    Magic.GlobalInit(libPath);
}
```

**WARNING**: Caller process and callee library must have the same architecture!

### Embedded binary

Joveler.Compression.ZLib comes with sets of static binaries of `zlib 1.2.11`. They are copied into the build directory at build time.

#### For .NET Framework 4.5.1+

| Platform         | Binary                      | Note               |
|------------------|-----------------------------|--------------------|
| Windows x86      | `$(OutDir)\x86\zlibwpi.dll` | Compiled w/ VS2019 wo assembly optimization, due to [the bug](https://github.com/madler/zlib/issues/274)|
| Windows x64      | `$(OutDir)\x64\zlibwpi.dll` | Compiled w/ VS2019 |

- Create an empty file named `Joveler.Compression.ZLib.Precompiled.Exclude` in the project directory to prevent a copy of the package-embedded binary.
- Joveler.Compression.ZLib recognizes only `zlibwapi.dll (stdcall)` , not `zlib1.dll (cdecl)` on Windows.

#### For .NET Standard 2.0+

| Platform      | Binary                                   | Note                     |
|---------------|------------------------------------------|--------------------------|
| Windows x86   | `$(OutDir)\runtimes\win-x86\zlibwpi.dll` | Compiled w/ VS2019 wo assembly optimization, due to [the bug](https://github.com/madler/zlib/issues/274) |
| Windows x64   | `$(OutDir)\runtimes\win-x64\zlibwpi.dll` | Compiled w/ VS2019       |
| Windows arm64 | `$(OutDir)\runtimes\win-x64\zlibwpi.dll` | Compiled w/ VS2019       |
| Linux x64     | `$(OutDir)\runtimes\linux-x64\libz.so`   | Compiled in Ubuntu 18.04 |
| Linux armhf   | `$(OutDir)\runtimes\linux-arm\libz.so`   | Compiled in Debian 10    |
| Linux arm64   | `$(OutDir)\runtimes\linux-arm64\libz.so` | Compiled in Debian 10    |
| macOS x64     | `$(OutDir)\runtimes\osx-x64\libz.dylib`  | Compiled in Catalina     |

- Joveler.Compression.ZLib recognizes only `zlibwapi.dll (stdcall)` , not `zlib1.dll (cdecl)` on Windows.
- If you call `ZLibInit.GlobalInit()` without `libPath` parameter on Linux or macOS, it will search for system-installed zlib.
- Linux binaries are not portable. They may not work on your distribution. In that case, call parameter-less `ZLibInit.GlobalInit()` to use system-installed zlib.

### Custom binary

To use custom zlib binary instead, call `ZLibInit.GlobalInit()` with a path to the custom binary.

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