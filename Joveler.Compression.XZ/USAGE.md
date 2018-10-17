# Usage

## Initialization

Joveler.Compression.XZ requires explicit loading of liblzma library.

You must call `XZInit.GlobalInit()` before using Joveler.Compression.XZ.

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
                libPath = Path.Combine("x64", "liblzma.dll");
                break;
            case Architecture.X86:
                libPath = Path.Combine("x86", "liblzma.dll");
                break;
        }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
                libPath = Path.Combine("x64", "liblzma.so");
                break;
        }
    }

    if (libPath == null)
        throw new PlatformNotSupportedException();

    XZInit.AssemblyInit(libPath);
}
```

**WARNING**: Caller process and callee library must have the same architecture!

### Embedded binary

Joveler.Compression.XZ comes with sets of static binaries of `liblzma 5.2.4`.  
They will be copied into the build directory at build time.

| Platform    | Binary                      |
|-------------|-----------------------------|
| Windows x86 | `$(OutDir)\x86\liblzma.dll` |
| Windows x64 | `$(OutDir)\x64\liblzma.dll` |
| Linux x64   | `$(OutDir)\x64\liblzma.so`  |

### Custom binary

To use custom liblzma binary instead, call `XZInit.GlobalInit()` with a path to the custom binary.

#### NOTES

- Linux x64 version of embedded `liblzma.so` was statically compiled in Ubuntu 18.04.
- Create an empty file named `Joveler.Compression.XZ.Precompiled.Exclude` in project directory to prevent copy of package-embedded binary.

### Cleanup

To unload liblzma library explicitly, call `XZInit.GlobalCleanup()`.

## Compression

### XZStream

The stream for [.xz file format](https://tukaani.org/xz/xz-file-format.txt).

### Constructor

```csharp
public XZStream(Stream stream, LzmaMode mode)
    : this(stream, mode, DefaultPreset, 1, false) { }
public XZStream(Stream stream, LzmaMode mode, uint preset)
    : this(stream, mode, preset, 1, false) { }
public XZStream(Stream stream, LzmaMode mode, uint preset, int threads)
    : this(stream, mode, preset, threads, false) { }
public XZStream(Stream stream, LzmaMode mode, bool leaveOpen)
    : this(stream, mode, 0, 1, leaveOpen) { }
public XZStream(Stream stream, LzmaMode mode, uint preset, bool leaveOpen)
    : this(stream, mode, preset, 1, leaveOpen) { }
public XZStream(Stream stream, LzmaMode mode, uint preset, int threads, bool leaveOpen)
```

- Preset

Select a compression preset level. 0 to 9 is allowed. Default value (`XZStream.DefaultPreset`) is 6.

- Threads

Specify the number of worker threads to use. Setting threads to a special value 0 makes xz use as many threads as there are CPU cores on the system.

The actual number of threads can be less than threads if the input file is not big enough for threading with the given settings or if using more threads would exceed the memory usage limit.

Threaded decompression is not supported.

**WARNING**: Beware of high memory usage in high preset or many threads.

| Preset | DictSize | CompCPU | CompMem | DecMem  |
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

### Examples

#### Compress file to xz

```csharp
using (FileStream fsOrigin = new FileStream("file_origin.bin", FileMode.Open))
using (FileStream fsComp = new FileStream("test.xz", FileMode.Create))
using (XZStream zs = new XZStream(fsComp, LzmaMode.Compress, XZStream.DefaultPreset))
{
    fsOrigin.CopyTo(zs);
}
```

#### Decompress file from xz

```csharp
using (FileStream fsComp = new FileStream("test.xz", FileMode.Create))
using (FileStream fsDecomp = new FileStream("file_decomp.bin", FileMode.Open))
using (XZStream zs = new XZStream(fsComp, LzmaMode.Decompress))
{
    zs.CopyTo(fsDecomp);
}
```
