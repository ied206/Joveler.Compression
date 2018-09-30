# Usage

## Initialization

Joveler.LZ4 requires explicit loading of lz4 library.

You must call `LZ4Init.GlobalInit()` before using Joveler.LZ4.

Put this snippet in your application's init code:

```csharp
public static void InitNativeLibrary
{
    string libPath = null;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
                libPath = Path.Combine("x64", "liblz4.dll");
                break;
            case Architecture.X86:
                libPath = Path.Combine("x86", "liblz4.dll");
                break;
        }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
                libPath = Path.Combine("x64", "liblz4.so");
                break;
        }
    }

    if (libPath == null)
        throw new PlatformNotSupportedException();

    LZ4Init.AssemblyInit(libPath);
}
```

**WARNING**: Caller process and callee library must have the same architecture!

### Embedded binary

Joveler.LZ4 comes with sets of static binaries of `lz4 1.8.3`.  
They will be copied into the build directory at build time.

| Platform    | Binary                     |
|-------------|----------------------------|
| Windows x86 | `$(OutDir)\x86\liblz4.dll` |
| Windows x64 | `$(OutDir)\x64\liblz4.dll` |
| Linux x64   | `$(OutDir)\x64\liblz4.so`  |

### Custom binary

To use custom lz4 binary instead, call `LZ4Init.GlobalInit()` with a path to the custom binary.

#### NOTES

- Create an empty file named `Joveler.Compression.LZ4.Precompiled.Exclude` in project directory to prevent copy of package-embedded binary.

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
