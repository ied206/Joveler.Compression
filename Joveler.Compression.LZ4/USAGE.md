# Usage

## Initialization

Joveler.Compression.LZ4 requires explicit loading of the lz4 library.

### Init Code Snippet

You must call `LZ4Init.GlobalInit()` before using Joveler.Compression.LZ4. Please put this code snippet in your application init code:

**WARNING**: The caller process and callee library must have the same architecture!

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
        libPath = Path.Combine(libDir, "liblz4.dll");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        libPath = Path.Combine(libDir, "liblz4.so");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        libPath = Path.Combine(libDir, "liblz4.dylib");

    if (libPath == null)
        throw new PlatformNotSupportedException($"Unable to find native library.");
    if (!File.Exists(libPath))
        throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

    Magic.GlobalInit(libPath);
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
    string libPath = Path.Combine(arch, "liblz4.dll");

    if (!File.Exists(libPath))
        throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

    Magic.GlobalInit(libPath);
}
```

### Embedded binary

Joveler.Compression.LZ4 comes with sets of static binaries of `lz4 1.10.0`. They are copied into the build directory at build time.

#### On .NET/.NET Core & .NET Standard

| Platform      | Minimum Target | Binary                                      | License      | C Runtime     |
|---------------|----------------|---------------------------------------------|--------------|---------------|
| Windows x86   | Windows 7 SP1  | `$(OutDir)\runtimes\win-x86\liblz4.dll`     | BSD 2-Clause | Universal CRT |
| Windows x64   | Windows 7 SP1  | `$(OutDir)\runtimes\win-x64\liblz4.dll`     | BSD 2-Clause | Universal CRT |
| Windows arm64 | Windows 7 SP1  | `$(OutDir)\runtimes\win-arm64\liblz4.dll`   | BSD 2-Clause | Universal CRT |
| Linux x64     | Ubuntu 20.04   | `$(OutDir)\runtimes\linux-x64\liblz4.so`    | BSD 2-Clause | glibc         |
| Linux armhf   | Ubuntu 20.04   | `$(OutDir)\runtimes\linux-arm\liblz4.so`    | BSD 2-Clause | glibc         |
| Linux arm64   | Ubuntu 20.04   | `$(OutDir)\runtimes\linux-arm64\liblz4.so`  | BSD 2-Clause | glibc         |
| macOS x64     | macOS 11       | `$(OutDir)\runtimes\osx-x64\liblz4.dylib`   | BSD 2-Clause | libSystem     |
| macOS arm64   | macOS 11       | `$(OutDir)\runtimes\osx-arm64\liblz4.dylib` | BSD 2-Clause | libSystem     |

- Bundled Windows binaires targets [Universal CRT](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170) to ensure interoperability with modern .NET runtime.
    - If you encounter a dependency issue on Windows 7 or 8.1, try [installing UCRT manually](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170).
- If you call `LZ4Init.GlobalInit()` without the `libPath` parameter on Linux or macOS, it will search for system-installed liblz4.
- Linux binaries are not portable. They may not work on your distribution.
    - You may call parameter-less `LZ4Init.GlobalInit()` to use system-installed liblz4.

#### On .NET Framework

| Platform         | Binary                       | License       | C Runtime     |
|------------------|------------------------------|---------------|---------------|
| Windows x86      | `$(OutDir)\x86\liblz4.dll`   | BSD 2-Clause  | Universal CRT |
| Windows x64      | `$(OutDir)\x64\liblz4.dll`   | BSD 2-Clause  | Universal CRT |
| Windows arm64    | `$(OutDir)\arm64\liblz4.dll` | BSD 2-Clause  | Universal CRT |

- Create an empty file named `Joveler.Compression.LZ4.Precompiled.Exclude` in the project directory to prevent copying of the package-embedded binary.

### Custom binary

To use custom lz4 binary instead, call `LZ4Init.GlobalInit()` with a path to the custom binary.

### Cleanup

To unload the lz4 library explicitly, call `LZ4Init.GlobalCleanup()`.

## LZ4FrameStream

`LZ4FrameStream` is the stream for compressing and decompressing [lz4 frame format](https://github.com/lz4/lz4/blob/dev/doc/lz4_Frame_format.md).

### Constructor

```csharp
// Create a compressing LZ4FrameStream instance
public LZ4FrameStream(Stream baseStream, LZ4FrameCompressOptions compOpts)
// Create a parallel compressing LZ4FrameStream instance (EXPERIMENTAL)
public LZ4FrameStream(Stream baseStream, LZ4FrameCompressOptions compOpts, LZ4FrameParallelCompressOptions pcompOpts)
// Create a decompressing LZ4FrameStream instance
public LZ4FrameStream(Stream baseStream, LZ4FrameDecompressOptions compOpts)
```

#### LZ4FrameCompressOptions

You can tune lz4 frame format compress options with this class.

| Property | Summary |
|----------|---------|
| Level | Compression level. The Default is `LZ4CompLevel.Fast`. Use `LZ4CompLevel.High` to turn on LZ4-HC mode. |
| BufferSize | Size of the internal buffer. The default is 1MB. This value is ignored in parallel compression, and use BlockSizeId instead. |
| BlockSizeId | Controls size of the block in lz4 frame. |
| ContentChecksumFlag | Whter to terminate frame with XXH32 checksum. |
| LeaveOpen | Whether to leave the base stream object open after disposing of the lz4 stream object. |

It also contains more advanced options.

#### LZ4FrameParallelCompressOptions **(EXPERIMENTAL)**

| Property    | Summary |
|-------------|---------|
| Threads     | The number of threads to use for parallel compression. |
| WaitTimeout | Controls timeout to allow Write() to return early. Set to null to block until compress & write is complete. Set to TimeSpan value to enable an upper limit on blocking. Timeout value is kept as best effort. |

#### LZ4FrameDecompressOptions

You can tune lz4 frame format decompress options with this class.

| Property | Summary |
|----------|---------|
| BufferSize | Size of the internal buffer. The default is 1MB. |
| LeaveOpen | Whether to leave the base stream object open after disposing of the lz4 stream object. |

### Examples

#### Compress file to lz4 frame format

```csharp
using Joveler.Compression.LZ4;

LZ4FrameCompressOptions compOpts = new LZ4FrameCompressOptions()
{
    Level = LZ4CompLevel.Default,
};

using (FileStream fsOrigin = new FileStream("file_origin.bin", FileMode.Open))
using (FileStream fsComp = new FileStream("test.lz4", FileMode.Create))
using (LZ4FrameStream zs = new LZ4FrameStream(fsComp, compOpts))
{
    fsOrigin.CopyTo(zs);
}
```

#### Compress file, in parallel

```csharp
using Joveler.Compression.LZ4;

LZ4FrameCompressOptions compOpts = new LZ4FrameCompressOptions()
{
    Level = LZ4CompLevel.Default,
};
LZ4FrameParallelCompressOptions pcompOpts = new LZ4FrameParallelCompressOptions()
{
    Threads = Environment.ProcessorCount,
};

using (FileStream fsOrigin = new FileStream("file_origin.bin", FileMode.Open))
using (FileStream fsComp = new FileStream("test.lz4", FileMode.Create))
using (LZ4FrameStream zs = new LZ4FrameStream(fsComp, compOpts, pcompOpts))
{
    fsOrigin.CopyTo(zs);
}
```

#### Decompress file from lz4 frame format

```csharp
using Joveler.Compression.LZ4;

LZ4FrameDecompressOptions decompOpts = new LZ4FrameDecompressOptions();

using (FileStream fsComp = new FileStream("test.lz4", FileMode.Create))
using (FileStream fsDecomp = new FileStream("file_decomp.bin", FileMode.Open))
using (LZ4FrameStream zs = new LZ4FrameStream(fsComp, decompOpts))
{
    zs.CopyTo(fsDecomp);
}
```

## XXH32Stream

`XXH32Stream` is the class designed to compute XXH32 hash.

- Write into a stream to provide data to hash.
- Call `Digest()` method to get a hashed result.
compute the checksum. It is valid even after the stream had been disposed.
- Use the `Reset()` methods to reuse Stream instance.

For small data, calling `XXH32()` static methods would be more faster.

This stream must be explicitly disposed.

### Example

```cs
using Joveler.Compression.LZ4.XXHash;

byte[] bin = Encoding.UTF8.GetBytes("ABCDEF");
using (XXH32Stream xxh32 = new XXH32Stream())
{
    xxh32.Write(bin, 0, bin.Length);
}
uint digest = xxh32.Digest();
Console.WriteLine($"0x{digest:X4}");

digest = XXH32Stream.XXH32(bin);
Console.WriteLine($"0x{digest:X4}");

digest = XXH32Stream.XXH32(XXH32Stream.XXH32Init, bin);
Console.WriteLine($"0x{digest:X4}");
```

## XXH32Algorithm

`XXH32Algorithm` is the class designed to compute XXH32 hash.

It inherits and implements [HashAlgorithm](https://docs.microsoft.com/en-US/dotnet/api/system.security.cryptography.hashalgorithm).

## XXH64Stream

`XXH64Stream` is the class designed to compute XXH64 hash.

- Write into a stream to provide data to hash.
- Call `Digest()` method to get a hashed result.
compute the checksum. It is valid even after the stream had been disposed.
- Use the `Reset()` methods to reuse Stream instance.

For small data, calling `XXH64()` static methods would be more faster.

This stream must be explicitly disposed.

### Example

```cs
using Joveler.Compression.LZ4.XXHash;

byte[] bin = Encoding.UTF8.GetBytes("ABCDEF");
using (XXH64Stream xxh64 = new XXH64Stream())
{
    xxh64.Write(bin, 0, bin.Length);
}
ulong digest = xxh64.Digest();
Console.WriteLine($"0x{digest:X8}");

digest = XXH64Stream.XXH64(bin);
Console.WriteLine($"0x{digest:X8}");

digest = XXH64Stream.XXH64(XXH64Stream.XXH64Init, bin);
Console.WriteLine($"0x{digest:X8}");
```

## XXH64Algorithm

`XXH64Algorithm` is the class designed to compute the XXH64 hash.

It inherits and implements [HashAlgorithm](https://docs.microsoft.com/en-US/dotnet/api/system.security.cryptography.hashalgorithm).
