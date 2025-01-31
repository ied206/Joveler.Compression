# Joveler.Compression.ZLib

Cross-platform [zlib](https://zlib.net/) pinvoke library for .NET.

`DefalteStream` and its familiy is based on the code of [zlibnet](https://zlibnet.codeplex.com) by [@hardon](https://www.codeplex.com/site/users/view/hardon).

## Install

Joveler.Compression.ZLib can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.ZLib/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.ZLib)](https://www.nuget.org/packages/Joveler.Compression.ZLib)

## Features

Joveler.Compression.ZLib exposes fast zlib capabilities with backed by zlib-ng.

- ZLibStream, the stream implementation conforms [RFC 1950](https://www.ietf.org/rfc/rfc1950.txt).
- Improved DeflateStream and GZipStream, conforming [RFC 1951](https://www.ietf.org/rfc/rfc1951.txt) and [RFC 1952](https://www.ietf.org/rfc/rfc1952.txt).
- (EXPERIMENTAL) Parallel compression support on ZLibStream, DeflateStream and GZipStream.
- Fast native implementation of Adler32 and CRC32 checksum.

## How is Joveler.Compression.ZLib different from System.IO.Compression?

.NET BCL also provides [System.IO.Compression](https://learn.microsoft.com/en-us/dotnet/api/system.io.compression) for handling zlib streams.

Here is the list where this library differs from .NET BCL:

1. Joveler.Compression.ZLib ships with fast zlib-ng fork, and performs better than .NET BCL.
1. Joveler.Compression.ZLib supports parallel compression.
1. System.IO.Compression lacks some zlib capabilities, which Joveler.Compression.ZLib does.
    - System.IO.Compression did not support ZLibStream and GZipStream until .NET 6.0, and still does not in .NET Framework.
    - System.IO.Compression did not support best compressoin level until .NET 6.0, and still does not in .NET Framework.
    - System.IO.Compression does not expose Adler32 and CRC32 checksum functions.

## Support

### Targeted .NET platforms

- .NET 8.0
- .NET Standard 2.0
- .NET Framework 4.6.2

### Supported OS platforms

| Platform | Architecture | Tested |
|----------|--------------|--------|
| Windows  | x86          | Yes    |
|          | x64          | Yes    |
|          | arm64        | Yes    |
| Linux    | x64          | Yes    |
|          | armhf        | Yes    |
|          | arm64        | Yes    |
| macOS    | x64          | Yes    |
|          | arm64        | Yes    |

### Supported zlib versions

- zlib-ng 2.2.3 compat ABI (Included)
    - Compatible with traditional zlib ABI, such as `zlib1.dll`.
- zlib 1.3
    - Supports both `zlib1.dll` and `zlibwapi.dll` on Windows.
- zlib-ng 2.2.3 modern ABI

## Changelog

See [CHANGELOG.md](./CHANGELOG.md).

## Usage

See [USAGE.md](./USAGE.md).

## License

`Joveler.Compression.ZLib` is licensed under [zlib license](./LICENSE).

## Performance

### Compression

In multithread compression, performance of `Joveler.Compression.ZLib` scales linearly.

| Method        | SrcFileName       | Level   | Mean         |
|---------------|-------------------|---------|--------------|
| zlib-ng       | bible_en_utf8.txt | Best    | 143,856 μs   |
| zlib-ng-T2    | bible_en_utf8.txt | Best    | 75,710 μs    |
| BCL           | bible_en_utf8.txt | Best    | 288,823 μs   |
| SharpCompress | bible_en_utf8.txt | Best    | 382,411 μs   |
| zlib-ng       | bible_en_utf8.txt | Default | 64,329 μs    |
| zlib-ng-T2    | bible_en_utf8.txt | Default | 34,439 μs    |
| BCL           | bible_en_utf8.txt | Default | 146,495 μs   |
| SharpCompress | bible_en_utf8.txt | Default | 202,771 μs   |
| zlib-ng       | bible_kr_utf8.txt | Best    | 189,140 μs   |
| zlib-ng-T2    | bible_kr_utf8.txt | Best    | 97,509 μs    |
| BCL           | bible_kr_utf8.txt | Best    | 295,126 μs   |
| SharpCompress | bible_kr_utf8.txt | Best    | 402,066 μs   |
| zlib-ng       | bible_kr_utf8.txt | Default | 75,758 μs    |
| zlib-ng-T2    | bible_kr_utf8.txt | Default | 39,735 μs    |
| BCL           | bible_kr_utf8.txt | Default | 190,129 μs   |
| SharpCompress | bible_kr_utf8.txt | Default | 262,210 μs   |
| zlib-ng       | ooffice.dll       | Best    | 208,351 μs   |
| zlib-ng-T2    | ooffice.dll       | Best    | 106,654 μs   |
| BCL           | ooffice.dll       | Best    | 385,086 μs   |
| SharpCompress | ooffice.dll       | Best    | 541,706 μs   |
| zlib-ng       | ooffice.dll       | Default | 91,696 μs    |
| zlib-ng-T2    | ooffice.dll       | Default | 48,151 μs    |
| BCL           | ooffice.dll       | Default | 224,886 μs   |
| SharpCompress | ooffice.dll       | Default | 316,129 μs   |
| zlib-ng       | reymont.pdf       | Best    | 276,046 μs   |
| zlib-ng-T2    | reymont.pdf       | Best    | 141,333 μs   |
| BCL           | reymont.pdf       | Best    | 718,390 μs   |
| SharpCompress | reymont.pdf       | Best    | 1,017,190 μs |
| zlib-ng       | reymont.pdf       | Default | 126,523 μs   |
| zlib-ng-T2    | reymont.pdf       | Default | 66,190 μs    |
| BCL           | reymont.pdf       | Default | 260,016 μs   |
| SharpCompress | reymont.pdf       | Default | 369,338 μs   |
