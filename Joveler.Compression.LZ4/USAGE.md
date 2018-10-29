# Usage

## Initialization

Joveler.Compression.LZ4 requires explicit loading of lz4 library.

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

Joveler.Compression.LZ4 comes with sets of static binaries of `lz4 1.8.3`.  
They will be copied into the build directory at build time.

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

- Create an empty file named `Joveler.Compression.LZ4.Precompiled.Exclude` in project directory to prevent copy of package-embedded binary.
- Untested on arm64, because .Net Core 2.1 arm64 runtime has an [issue](https://github.com/dotnet/coreclr/issues/19578).

### Cleanup

To unload lz4 library explicitly, call `LZ4Init.GlobalCleanup()`.

## Compression

### LZ4FrameStream

The stream for [lz4 frame format](https://github.com/lz4/lz4/blob/dev/doc/lz4_Frame_format.md).

### Constructor

```csharp
public LZ4FrameStream(Stream stream, LZ4Mode mode)
    : this(stream, mode, 0, false) { }
public LZ4FrameStream(Stream stream, LZ4Mode mode, LZ4CompLevel compressionLevel)
    : this(stream, mode, compressionLevel, false) { }
public LZ4FrameStream(Stream stream, LZ4Mode mode, bool leaveOpen)
    : this(stream, mode, 0, leaveOpen) { }
public unsafe LZ4FrameStream(Stream stream, LZ4Mode mode, LZ4CompLevel compressionLevel, bool leaveOpen)
```

- LZ4CompLevel

Select a compression level. The Default is `LZ4CompLevel.Fast`. Use `LZ4CompLevel.High` to turn on LZ4-HC mode.

### Examples

#### Compress file to lz4

```csharp
using (FileStream fsOrigin = new FileStream("file_origin.bin", FileMode.Open))
using (FileStream fsComp = new FileStream("test.lz4", FileMode.Create))
using (LZ4FrameStream zs = new LZ4FrameStream(fsComp, LZ4Mode.Compress, LZ4CompLevel.Default))
{
    fsOrigin.CopyTo(zs);
}
```

#### Decompress file from lz4

```csharp
using (FileStream fsComp = new FileStream("test.lz4", FileMode.Create))
using (FileStream fsDecomp = new FileStream("file_decomp.bin", FileMode.Open))
using (LZ4FrameStream zs = new LZ4FrameStream(fsComp, LZ4Mode.Decompress))
{
    zs.CopyTo(fsDecomp);
}
```
