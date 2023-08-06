# Usage

## Initialization

Joveler.Compression.XZ requires explicit loading of the liblzma library.

### Init Code Snippet

You must call `XZInit.GlobalInit()` before using Joveler.Compression.XZ. Please put this code snippet in your application init code:

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
        libPath = Path.Combine(libDir, "liblzma.dll");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        libPath = Path.Combine(libDir, "liblzma.so");
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        libPath = Path.Combine(libDir, "liblzma.dylib");

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
    string libPath = Path.Combine(arch, "liblzma.dll");

    if (!File.Exists(libPath))
        throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

    Magic.GlobalInit(libPath);
}
```

### Embedded binary

Joveler.Compression.XZ comes with sets of static binaries of `liblzma 5.4.3`. They will be copied into the build directory at build time.

#### On .NET/.NET Core & .NET Standard

| Platform              | Binary                                       | License       | C Runtime     |
|-----------------------|----------------------------------------------|---------------|---------------|
| Windows x86           | `$(OutDir)\runtimes\win-x86\liblzma.dll`     | Public Domain | Universal CRT |
| Windows x64           | `$(OutDir)\runtimes\win-x64\liblzma.dll`     | Public Domain | Universal CRT |
| Windows arm64         | `$(OutDir)\runtimes\win-arm64\liblzma.dll`   | Public Domain | Universal CRT |
| Ubuntu 20.04 x64      | `$(OutDir)\runtimes\linux-x64\liblzma.so`    | Public Domain | glibc         |
| Debian 12 armhf       | `$(OutDir)\runtimes\linux-arm\liblzma.so`    | Public Domain | glibc         |
| Debian 12 arm64       | `$(OutDir)\runtimes\linux-arm64\liblzma.so`  | Public Domain | glibc         |
| macOS Big Sur x64     | `$(OutDir)\runtimes\osx-x64\liblzma.dylib`   | Public Domain | libSystem     |
| macOS Monterey arm64  | `$(OutDir)\runtimes\osx-arm64\liblzma.dylib` | Public Domain | libSystem     |

- Bundled Windows binaires now target [Universal CRT](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170) for better interopability with MSVC.
    - .NET Core/.NET 5+ runs on UCRT, so no action is required in most cases.
    - If you encounter a dependency issue on Windows Vista, 7 or 8.1, try [installing UCRT manually](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170).
- If you call `XZInit.GlobalInit()` without the `libPath` parameter on Linux or macOS, it will search for system-installed liblzma.
- Linux binaries are not portable. They may not work on your distribution.
    - You may call parameter-less `XZInit.GlobalInit()` to use system-installed liblzma.

#### On .NET Framework

| Platform         | Binary                        | License       | C Runtime     |
|------------------|-------------------------------|---------------|---------------|
| Windows x86      | `$(OutDir)\x86\liblzma.dll`   | Public Domain | Universal CRT |
| Windows x64      | `$(OutDir)\x64\liblzma.dll`   | Public Domain | Universal CRT |
| Windows arm64    | `$(OutDir)\arm64\liblzma.dll` | Public Domain | Universal CRT |

- Create an empty file named `Joveler.Compression.XZ.Precompiled.Exclude` in the project directory to prevent copying of the package-embedded binary.

### Custom binary

To use the custom liblzma binary instead, call `XZInit.GlobalInit()` with a path to the custom binary.

## Cleanup

To unload the liblzma library explicitly, call `XZInit.GlobalCleanup()`.

## XZStream

`XZStream` handles compressing and decompressing of [.xz file format](https://tukaani.org/xz/xz-file-format.txt).

### Constructor

```csharp
// Create a compressing XZStream instance.
public XZStream(Stream baseStream, XZCompressOptions compOpts)
// Create a multi-threaded compressing XZStream instance.
public XZStream(Stream baseStream, XZCompressOptions compOpts, XZThreadedCompressOptions threadOpts)
// Create a decompressing XZStream instance.
public XZStream(Stream baseStream, XZDecompressOptions decompOpts)
// Create a multi-threaded decompressing XZStream instance.
public XZStream(Stream baseStream, XZDecompressOptions decompOpts, XZThreadedDecompressOptions threadOpts)
```

#### XZCompressOptions

You can tune xz compress options with this class.

| Property | Summary |
|----------|---------|
| Level | Compression level. The Default is `LzmaCompLevel.Default` (6). |
| ExtremeFlag | Use a slower variant to get a little bit better compression ratio hopefully. |
| BufferSize | Size of the internal buffer. The default is 1MB. |
| LeaveOpen | Whether to leave the base stream object open after disposing of the xz stream object. |

It may contain more advanced options.

**NOTE**: xz file created in single-threaded mode cannot be decompressed in parallel. xz-utils does not divide the compressed stream into blocks when the multi-threaded compression is not enabled.

**WARNING**: Beware of high memory usage at a high compression level.

| Level  | DictSize | CompCPU | CompMem | DecMem  |
|--------|----------|---------|---------|---------|
| 0      | 256 KiB  | 0       |   3 MiB |   1 MiB |
| 1      |   1 MiB  | 1       |   9 MiB |   2 MiB |
| 2      |   2 MiB  | 2       |  17 MiB |   3 MiB |
| 3      |   4 MiB  | 3       |  32 MiB |   5 MiB |
| 4      |   4 MiB  | 4       |  48 MiB |   5 MiB |
| 5      |   8 MiB  | 5       |  94 MiB |   9 MiB |
| 6 (D)  |   8 MiB  | 6       |  94 MiB |   9 MiB |
| 7      |  16 MiB  | 6       | 186 MiB |  17 MiB |
| 8      |  32 MiB  | 6       | 370 MiB |  33 MiB |
| 9      |  64 MiB  | 6       | 674 MiB |  65 MiB |

#### XZThreadedCompressOptions

If you want to compress in parallel, pass an instance of this class to the `XZStream` constructor.

| Property  | Summary |
|-----------|---------|
| Threads   | Number of worker threads to use. |

It may contain more advanced options.

**NOTE**: You must use threaded compression to let xz-utils decompress in parallel, even if you are using only 1 thread. 

**WARNING**: If possible, always check the available system memory. Modern CPUs have a lot of cores, and each thread will allocate its own buffer.

**WARNING**: In multi-threaded compression, each thread may allocate more memory than the single-thread mode, including compressing in 1 thread. xz-utils aggressively buffers input and output in parallel compression. Use `XZInit.EncoderMemUsage()` to check the exact memory requirement for your config.

#### XZDecompressOptions

You can tune xz decompress options with this class.

| Property | Summary |
|----------|---------|
| BufferSize | Size of the internal buffer. The default is 1MB. |
| LeaveOpen | Whether to leave the base stream object open after disposing of the xz stream object. |

It also contains more advanced options. 

#### XZThreadedDecompressOptions

If you want to decompress in parallel, pass an instance of this class to the `XZStream` constructor.

| Property  | Summary |
|-----------|---------|
| Threads   | Number of worker threads to use. |
| MemlimitThreading | Memory usage soft limit to reduce the number of threads. |

It may contain more advanced options.

**WARNING**: xz-utils can decompress an xz file only if it had been compressed in parallel.

**WARNING**: Threaded decompression will take more memory! Tweak `MemlimitThreading` to pass a guideline of maximum memory usage.

### Examples

#### Compress a file to .xz

```csharp
using Joveler.Compression.XZ;

// Compress in single-threaded mode
XZCompressOptions compOpts = new XZCompressOptions
{
    Level = LzmaCompLevel.Default,
};

using (FileStream fsOrigin = new FileStream("file_origin.bin", FileMode.Open))
using (FileStream fsComp = new FileStream("test.xz", FileMode.Create))
using (XZStream zs = new XZStream(fsComp, compOpts))
{
    fsOrigin.CopyTo(zs);
}
```

#### Compress a file to .xz in parallel

```csharp
using Joveler.Compression.XZ;

// Warning: This config takes up a massive amount of memory!
XZCompressOptions compOpts = new XZCompressOptions
{
    Level = LzmaCompLevel.Level9,
    ExtremeFlag = true,
};
XZThreadedCompressOptions threadOpts = new XZThreadedCompressOptions
{
    Threads = Environment.ProcesserCount,
};

using (FileStream fsOrigin = new FileStream("file_origin.bin", FileMode.Open))
using (FileStream fsComp = new FileStream("test.xz", FileMode.Create))
using (XZStream zs = new XZStream(fsComp, compOpts, threadOpts))
{
    fsOrigin.CopyTo(zs);
}
```

#### Decompress a file from .xz

```csharp
using Joveler.Compression.XZ;

XZDecompressOptions decompOpts = new XZDecompressOptions();

using (FileStream fsComp = new FileStream("test.xz", FileMode.Create))
using (FileStream fsDecomp = new FileStream("file_decomp.bin", FileMode.Open))
using (XZStream zs = new XZStream(fsCompp, decompOpts))
{
    zs.CopyTo(fsDecomp);
}
```

#### Decompress a file from .xz in parallel

```csharp
using Joveler.Compression.XZ;

XZDecompressOptions decompOpts = new XZDecompressOptions();
XZThreadedDecompressOptions threadOpts = new XZThreadedDecompressOptions
{
    Threads = Environment.ProcesserCount,
};

// Limit maximum memory liblzma is allowed to use.
// The following values are taken from the xz CLI program code.
switch (XZInit.Lib.PlatformBitness)
{
    case DynLoader.PlatformBitness.Bit32:
        threadOpts.MemlimitThreading = Math.Min(XZHardware.PhysMem() / 4, 1400U << 20);
        break;s
    case DynLoader.PlatformBitness.Bit64:
        threadOpts.MemlimitThreading = XZHardware.PhysMem() / 4;
        break;
}

using (FileStream fsComp = new FileStream("test.xz", FileMode.Create))
using (FileStream fsDecomp = new FileStream("file_decomp.bin", FileMode.Open))
using (XZStream zs = new XZStream(fsComp, decompOpts, threadOpts))
{
    zs.CopyTo(fsDecomp);
}
```

## Crc32Checksum

`Crc32Checksum` is the class designed to compute CRC32 checksum.

- Use the `Append()` method to compute the checksum.  
- Use the `Checksum` property to get the checksum value.
- Use the `Reset()` method to reset the `Checksum` property.

### Examples

#### `Append(ReadOnlySpan<byte> buffer)`, `Append(byte[] buffer, int offset, int count)`

```cs
using Joveler.Compression.XZ.Checksum;

Crc32Checksum crc = new Crc32Checksum();
byte[] bin = Encoding.UTF8.GetBytes("ABCDEF");

// Append(ReadOnlySpan<byte> buffer)
crc.Append(bin.AsSpan(2, 3));
Console.WriteLine($"0x{crc.Checksum:X4}");

// Append(byte[] buffer, int offset, int count)
crc.Reset();
crc.Append(bin, 2, 3);
Console.WriteLine($"0x{crc.Checksum:X4}");
```

#### `Append(Stream stream)`

```csharp
using Joveler.Compression.XZ.Checksum;

using (FileStream fs = new FileStream("read.txt", FileMode.Open))
{
    Crc32Checksum crc = new Crc32Checksum();

    // Append(Stream stream)
    crc.Append(fs);
    Console.WriteLine($"0x{crc.Checksum:X4}");
}
```

## Crc32Algorithm

`Crc32Algorithm` is the class designed to compute CRC32 checksum.

It inherits and implements [HashAlgorithm](https://docs.microsoft.com/en-US/dotnet/api/system.security.cryptography.hashalgorithm).


## Crc64Checksum

`Crc64Checksum` is the class designed to compute CRC64 checksum.

Use the `Append()` method to compute the checksum.  
Use the `Checksum` property to get the checksum value.
Use the `Reset()` method to reset the `Checksum` property.

### Examples

#### `Append(ReadOnlySpan<byte> buffer)`, `Append(byte[] buffer, int offset, int count)`

```cs
using Joveler.Compression.XZ.Checksum;

Crc64Checksum crc = new Crc64Checksum();
byte[] bin = Encoding.UTF8.GetBytes("ABCDEF");

// Append(ReadOnlySpan<byte> buffer)
crc.Append(bin.AsSpan(2, 3));
Console.WriteLine($"0x{crc.Checksum:X8}");

// Append(byte[] buffer, int offset, int count)
crc.Reset();
crc.Append(bin, 2, 3);
Console.WriteLine($"0x{crc.Checksum:X8}");
```

#### `Append(Stream stream)`

```csharp
using Joveler.Compression.XZ.Checksum;

using (FileStream fs = new FileStream("read.txt", FileMode.Open))
{
    Crc64Checksum crc = new Crc64Checksum();

    // Append(Stream stream)
    crc.Append(fs);
    Console.WriteLine($"0x{crc.Checksum:X8}");
}
```

## Crc64Algorithm

`Crc64Algorithm` is the class designed to compute the CRC64 checksum.

It inherits and implements [HashAlgorithm](https://docs.microsoft.com/en-US/dotnet/api/system.security.cryptography.hashalgorithm).
