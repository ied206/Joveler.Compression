# Usage

## Initialization

Joveler.Compression.LZ4 requires explicit loading of the lz4 library.

You must call `LZ4Init.GlobalInit()` before using Joveler.Compression.LZ4.

Put this snippet in your application's init code:

```csharp
public static void InitNativeLibrary()
{
    const string x64 = "x64";
    const string x86 = "x86";
    const string armhf = "armhf";
    const string arm64 = "arm64";

    const string dllName = "liblz4.dll";
    const string soName = "liblz4.so";

    string libPath = null;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
                libPath = Path.Combine(x86, dllName);
                break;
            case Architecture.X64:
                libPath = Path.Combine(x64, dllName);
                break;
        }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
                libPath = Path.Combine(x64, soName);
                break;
            case Architecture.Arm:
                libPath = Path.Combine(armhf, soName);
                break;
            case Architecture.Arm64:
                libPath = Path.Combine(arm64, soName);
                break;
        }
    }

    if (libPath == null)
        throw new PlatformNotSupportedException();

    LZ4Init.GlobalInit(libPath);
}
```

**WARNING**: Caller process and callee library must have the same architecture!

### Embedded binary

Joveler.Compression.LZ4 comes with sets of static binaries of `lz4 1.9.2`.  
They are copied into the build directory at build time.

| Platform    | Binary                      | Note |
|-------------|-----------------------------|------|
| Windows x86 | `$(OutDir)\x86\liblz4.dll`  |      |
| Windows x64 | `$(OutDir)\x64\liblz4.dll`  |      |
| Linux x64   | `$(OutDir)\x64\liblz4.so`   | Compiled in Ubuntu 18.04 |
| Linux armhf | `$(OutDir)\armhf\liblz4.so` | Compiled in Debian 9     |
| Linux arm64 | `$(OutDir)\arm64\liblz4.so` | Compiled in Debian 9     |

### Custom binary

To use custom lz4 binary instead, call `LZ4Init.GlobalInit()` with a path to the custom binary.

#### NOTES

- Create an empty file named `Joveler.Compression.LZ4.Precompiled.Exclude` in the project directory to prevent a copy of the package-embedded binary.
- Untested on arm64, because .Net Core 2.1 arm64 runtime has an [issue](https://github.com/dotnet/coreclr/issues/19578).

### Cleanup

To unload the lz4 library explicitly, call `LZ4Init.GlobalCleanup()`.

## LZ4FrameStream

`LZ4FrameStream` is the stream for compressing and decompressing [lz4 frame format](https://github.com/lz4/lz4/blob/dev/doc/lz4_Frame_format.md).

### Constructor

```csharp
// Create a compressing LZ4FrameStream instance
public LZ4FrameStream(Stream baseStream, LZ4FrameCompressOptions compOpts)
// Create a decompressing LZ4FrameStream instance
public LZ4FrameStream(Stream baseStream, LZ4FrameDecompressOptions compOpts)
```

#### LZ4FrameCompressOptions

You can tune lz4 frame format compress options with this class.

| Property | Summary |
|----------|---------|
| Level | Compression level. The Default is `LZ4CompLevel.Fast`. Use `LZ4CompLevel.High` to turn on LZ4-HC mode. |
| BufferSize | Size of the internal buffer. The default is 16KB as lz4 library recommends. |
| LeaveOpen | Whether to leave the base stream object open after disposing of the lz4 stream object. |

It also contains more advanced options.

#### LZ4FrameDecompressOptions

You can tune lz4 frame format decompress options with this class.

| Property | Summary |
|----------|---------|
| BufferSize | Size of the internal buffer. The default is 16KB as lz4 library recommends. |
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
