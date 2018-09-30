# Usage

## Initialization

Joveler.ZLib requires explicit loading of a zlib library.

You must call `ZLibInit.GlobalInit()` before using Joveler.ZLib.

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
                libPath = Path.Combine("x64", "zlibwapi.dll");
                break;
            case Architecture.X86:
                libPath = Path.Combine("x86", "zlibwapi.dll");
                break;
        }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
                libPath = Path.Combine("x64", "libz.so");
                break;
        }
    }

    if (libPath == null)
        throw new PlatformNotSupportedException();

    ZLibInit.AssemblyInit(libPath);
}
```

**WARNING**: Caller process and callee library must have the same architecture!

### Embedded binary

Joveler.ZLib comes with sets of static binaries of `zlib 1.2.11`.  
They will be copied into the build directory at build time.

| Platform    | Binary                      |
|-------------|-----------------------------|
| Windows x86 | `$(OutDir)\x86\zlibwpi.dll` |
| Windows x64 | `$(OutDir)\x64\zlibwpi.dll` |
| Linux x64   | `$(OutDir)\x64\libz.so`     |

#### Known Issue

- Windows x86 version of embedded `zlibwapi.dll` was compiled without assembly optimization, due to [the bug](https://github.com/madler/zlib/issues/274).

### Custom binary

To use custom zlib binary instead, call `ZLibInit.GlobalInit()` with a path to the custom binary.

#### NOTES

- Joveler.ZLib can only recognize `zlibwapi.dll (stdcall)` , not `zlib1.dll (cdecl)`.  
- Create an empty file named `Joveler.Compression.ZLib.Precompiled.Exclude` in project directory to prevent copy of package-embedded binary.

### Cleanup

To unload zlib library explicitly, call `ZLibInit.GlobalCleanup()`.

## Compression

### DeflateStream

The stream to process a data format conforming [RFC 1951](https://www.ietf.org/rfc/rfc1951.txt).

Its API is similar to `System.IO.Compression.DeflateStream`.

#### Ex) Compression

```cs
using (FileStream fsOrigin = new FileStream("file_origin.bin", FileMode.Open))
using (FileStream fsComp = new FileStream("test.deflate", FileMode.Create))
using (DeflateStream zs = new DeflateStream(fsComp, ZLibMode.Compress, ZLibCompLevel.Default))
{
    fsOrigin.CopyTo(zs);
}
```

`ZLibWrapper.CompressionLevel` has more option compared to `System.IO.Compression.CompressionLevel`:

```cs
public enum ZLibCompLevel : int
{
    Default = -1,
    NoCompression = 0,
    BestSpeed = 1,
    BestCompression = 9,
    Level0 = 0,
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
    Level4 = 4,
    Level5 = 5,
    Level6 = 6,
    Level7 = 7,
    Level8 = 8,
    Level9 = 9,
}
```

#### Ex) Decompression

```cs
using (FileStream fsComp = new FileStream("test.deflate", FileMode.Create))
using (FileStream fsDecomp = new FileStream("file_decomp.bin", FileMode.Open))
using (DeflateStream zs = new DeflateStream(fsComp, ZLibMode.Decompress))
{
    zs.CopyTo(fsDecomp);
}
```

### ZLibStream

A stream to process a data format conforming [RFC 1950](https://www.ietf.org/rfc/rfc1950.txt).

Same usage with `DeflateStream`.

### GZipStream

A stream to process a data format conforming [RFC 1952](https://www.ietf.org/rfc/rfc1952.txt).

Same usage with `DeflateStream`.

### DeflateCompressor

A helper class for `DeflateStream`.

#### Ex) `DeflateCompressor.Compress(Stream stream)`

```cs
using (FileStream fsOrigin = new FileStream("file_origin.bin", FileMode.Open))
using (MemoryStream msComp = DeflateCompressor.Compress(fsOrigin))
{
    // write msComp to file, or send through network, etc
}
```

#### Ex) `DeflateCompressor.Decompress(byte[] buffer)`

```cs
byte[] input = new byte[] { 0x73, 0x74, 0x72, 0x76, 0x71, 0x75, 0x03, 0x00 };
byte[] decompBytes = DeflateCompressor.Decompress(input);
string decompText = Encoding.UTF8.GetString(decompBytes);
Console.WriteLine(decompText); // "ABCDEF"
```

### ZLibCompressor

A helper class for `ZLibStream`.

Same usage with `DeflateCompressor`.

### GZipCompressor

A helper class for `GZipStream`.

Same usage with `DeflateCompressor`.

## Checksum

**NOTE**: To use checksum calculation, you MUST USE `zlibwapi.dll`.

### Adler32Checksum

A class to compute the adler32 checksum.

Use `Append()` methods to compute checksum.  
Use `Checksum` property to get checksum value.

#### Ex) `Append(Stream stream)`

```cs
using (FileStream fs = new FileStream("read.txt", FileMode.Open))
{
    Adler32Checksum adler = new Adler32Checksum();
    adler.Append(fs);
    Console.WriteLine("0x" + adler.Checksum.ToString("X8"));
}
```

#### Ex) `Append(byte[] buffer)`, `Append(byte[] buffer, int offset, int count)`

```cs
Adler32Checksum adler = new Adler32Checksum();
byte[] bin = Encoding.UTF8.GetBytes("ABCDEF");

adler.Append(bin);
Console.WriteLine("0x" + adler.Checksum.ToString("X8")); // 0x057E0196

adler.Append(bin, 2, 3);
Console.WriteLine("0x" + adler.Checksum.ToString("X8")); // 0x0BD60262
```

Static wrapper methods named `Adler32Checksum.Adler32()` behave just like zlib's `adler32()` function.

Example of static wrapper methods:

```cs
byte[] bin = Encoding.UTF8.GetBytes("ABCDEF");

// Call Adler32() without checksum to use initial state.
uint checksum = Adler32Checksum.Adler32(bin);
Console.WriteLine("0x" + checksum.ToString("X8")); // 0x057E0196

// Call Adler32() with checksum to set as current state.
checksum = Adler32Checksum.Adler32(checksum, bin, 2, 3);
Console.WriteLine("0x" + checksum.ToString("X8")); // 0x0BD60262

// Stream can be passed to Adler32() as well as byte
using (MemoryStream ms = new MemoryStream(bin))
{
    checksum = Adler32Checksum.Adler32(ms);
    Console.WriteLine("0x" + checksum.ToString("X8")); // 0x057E0196
}
```

### Crc32Checksum

Same usage with `Adler32Checksum`.

To use static wrapper methods, call `Crc32Checksum.Crc32()` instead of `Adler32Checksum.Adler32()`.

### Adler32Stream

A stream designed to compute adler32 checksum on-the-fly.

#### Ex) Reading from `AdlerStream`

```cs
using (FileStream fs = new FileStream("read.bin", FileMode.Open))
using (Adler32Stream adler = new Adler32Stream(fs))
{
    byte[] buffer = new byte[256];
    adler.Read(buffer, 0, 256);
    Console.WriteLine("0x" + adler.Checksum.ToString("X8"));

    adler.Read(buffer, 0, 128);
    Console.WriteLine("0x" + adler.Checksum.ToString("X8"));
}
```

#### Ex) Writing to `AdlerStream`

```cs
using (FileStream fs = new FileStream("write.bin", FileMode.Create))
using (Adler32Stream adler = new Adler32Stream(fs))
{
    byte[] bin;

    bin = new byte[] { 0x01, 0x02, 0x03 };
    adler.Write(bin, 0, bin.Length);
    Console.WriteLine("0x" + adler.Checksum.ToString("X8")); // 0x000D0007

    bin = new byte[] { 0x04, 0x05, 0x06 };
    adler.Write(bin, 0, bin.Length);
    Console.WriteLine("0x" + adler.Checksum.ToString("X8")); // 0x003E0016
}
```

### Crc32Stream

Same usage with `Adler32Stream`.
