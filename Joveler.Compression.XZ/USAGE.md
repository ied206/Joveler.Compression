# Usage

## Initialization

Joveler.Compression.XZ requires explicit loading of the liblzma library.

You must call `XZInit.GlobalInit()` before using Joveler.Compression.XZ. Please put this code snippet in your application init code:

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
    string libPath = Path.Combine(arch, "liblzma.dll");

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

**WARNING**: Caller process and callee library must have the same architecture!

### Embedded binary

Joveler.Compression.XZ comes with sets of static binaries of `liblzma 5.2.5`. They will be copied into the build directory at build time.

#### For .NET Framework 4.5.1+

| Platform         | Binary                      | Note            |
|------------------|-----------------------------|-----------------|
| Windows x86      | `$(OutDir)\x86\liblzma.dll` | Official binary |
| Windows x64      | `$(OutDir)\x64\liblzma.dll` | Official binary |

- Create an empty file named `Joveler.Compression.XZ.Precompiled.Exclude` in the project directory to prevent copy of the package-embedded binary.

#### For .NET Standard 2.0+

| Platform         | Binary                                      | Note                     |
|------------------|---------------------------------------------|--------------------------|
| Windows x86      | `$(OutDir)\runtimes\win-x86\liblzma.dll`    | Official binary          |
| Windows x64      | `$(OutDir)\runtimes\win-x64\liblzma.dll`    | Official binary          |
| Ubuntu 18.04 x64 | `$(OutDir)\runtimes\linux-x64\liblzma.so`   | Compiled in Ubuntu 18.04 |
| Debian 9 armhf   | `$(OutDir)\runtimes\linux-arm\liblzma.so`   | Compiled in Debian 10    |
| Debian 9 arm64   | `$(OutDir)\runtimes\linux-arm64\liblzma.so` | Compiled in Debian 10    |
| macOS 10.15      | `$(OutDir)\runtimes\osx-x64\liblzma.dylib`  | Compiled in Catalina     |

- If you call `XZInit.GlobalInit()` without `libPath` parameter on Linux or macOS, it will search for system-installed liblzma.
- Linux binaries are not portable. They may not work on your distribution. In that case, call parameter-less `XZInit.GlobalInit()` to use system-installed liblzma.

### Custom binary

To use custom liblzma binary instead, call `XZInit.GlobalInit()` with a path to the custom binary.

### Cleanup

To unload the liblzma library explicitly, call `XZInit.GlobalCleanup()`.

## XZStream

`XZStream` handles compressing and decompressing of [.xz file format](https://tukaani.org/xz/xz-file-format.txt).

### Constructor

```csharp
// Create a compressing XZStream instance
public XZStream(Stream baseStream, XZCompressOptions compOpts)
// Create a multi-threaded compressing XZStream instance
public XZStream(Stream baseStream, XZCompressOptions compOpts, XZThreadedCompressOptions threadOpts)
// Create a decompressing XZStream instance
public XZStream(Stream baseStream, XZDecompressOptions decompOpts)
```

#### XZCompressOptions

You can tune xz compress options with this class.

| Property | Summary |
|----------|---------|
| Level | Compression level. The Default is `ZLibCompLevel.Default` (6). |
| ExtremeFlag | Use a slower variant to get a little bit better compression ratio hopefully. |
| BufferSize | Size of the internal buffer. The default is 64KB. |
| LeaveOpen | Whether to leave the base stream object open after disposing of the xz stream object. |

It also contains more advanced options.

**NOTE**: xz file created in single-threaded mode will not be able to be decompressed in parallel in the future versions of xz-utils. It is because xz-utils does not divide the compressed stream into blocks when the multi-threaded compression is not enabled.

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

| Property | Summary |
|----------|---------|
| Threads  | Number of worker threads to use. |

It also contains more advanced options.

**NOTE**: When you create XZStream with this parameter, the future versions of xz-utils may be able to be decompressed created xz file in parallel. It is even true when you used only 1 thread with threaded compression. It is because xz-utils only divide the compressed stream into blocks in threaded compression.

**WARNING**: In multi-threaded compression, each thread may allocate more memory than the single-thread mode. It is true even if you run multi-threaded mode with 1 thread because xz-utils aggressively buffers input and output in parallel compression. Use `XZInit.EncoderMemUsage()` to check exact memory requirement for your config.

#### XZDecompressOptions

You can tune xz decompress options with this class.

| Property | Summary |
|----------|---------|
| BufferSize | Size of the internal buffer. The default is 64KB. |
| LeaveOpen | Whether to leave the base stream object open after disposing of the xz stream object. |

It also contains more advanced options. 

**WARNING**: Threaded decompression is not supported yet in the xz library.

### Examples

#### Compress file to .xz

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

#### Compress file to .xz in parallel

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

#### Decompress file from .xz

```csharp
using Joveler.Compression.XZ;

XZDecompressOptions decompOpts = new XZDecompressOptions();

using (FileStream fsComp = new FileStream("test.xz", FileMode.Create))
using (FileStream fsDecomp = new FileStream("file_decomp.bin", FileMode.Open))
using (XZStream zs = new XZStream(fsComp, LzmaMode.Decompress))
{
    zs.CopyTo(fsDecomp);
}
```

## Crc32Checksum

`Crc32Checksum` is the class designed to compute CRC32 checksum.

Use `Append()` methods to compute the checksum.  
Use `Checksum` property to get checksum value.
Use `Reset()` methods to reset `Checksum` property.

**NOTE**: xz-utils provides about twice faster CRC32 implementation than zlib. 

### Examples

#### `Append(ReadOnlySpan<byte> buffer)`, `Append(byte[] buffer, int offset, int count)`

```cs
using Joveler.Compression.XZ.Checksum;

Crc32Checksum crc = new Crc32Checksum();
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

```cs
using Joveler.Compression.XZ.Checksum;

using (FileStream fs = new FileStream("read.txt", FileMode.Open))
{
    Crc32Checksum crc = new Crc32Checksum();

    // Append(Stream stream)
    crc.Append(fs);
    Console.WriteLine($"0x{crc.Checksum:X8}");
}
```

## Crc32Algorithm

`Crc32Algorithm` is the class designed to compute CRC32 checksum.

It inherits and implements [HashAlgorithm](https://docs.microsoft.com/en-US/dotnet/api/system.security.cryptography.hashalgorithm).
