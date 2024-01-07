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
- Fast Adler32 and CRC32 checksum.

## How is Joveler.Compression.ZLib different from System.IO.Compression?

.NET BCL also provides [System.IO.Compression](https://learn.microsoft.com/en-us/dotnet/api/system.io.compression) for handling zlib streams.

Here is the list where this library differs from .NET BCL:

1. Joveler.Compression.ZLib ships with fast zlib-ng fork, and performs better than .NET BCL.
1. System.IO.Compression lacks some zlib capabilities, which Joveler.Compression.ZLib does.
    - System.IO.Compression did not support ZLibStream and GZipStream until .NET 6.0, and still does not in .NET Framework.
    - System.IO.Compression did not support best compressoin level until .NET 6.0, and still does not in .NET Framework.
    - System.IO.Compression does not expose Adler32 and CRC32 checksum functions.

## Support

### Targeted .NET platforms

- .NET Core 3.1
- .NET Standard 2.0
- .NET Framework 4.6

#### Discontinued target frameworks

| Platform | Last Supported Version |
|----------|------------------------|
| .NET Framework 4.5 | [ZLibWrapper](https://www.nuget.org/packages/Joveler.ZLibWrapper) |
| .NET Standard 1.3 | [v2.1.2](https://www.nuget.org/packages/Joveler.Compression.ZLib/2.1.2) |
| .NET Framework 4.5.1 | [v4.1.0](https://www.nuget.org/packages/Joveler.Compression.ZLib/4.1.0) |

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

### Supported zlib version

- zlib-ng 2.1.3 compat ABI (Included)
    - Compatible with traditional zlib ABI, such as `zlib1.dll`.
- zlib 1.3
    - Supports both `zlib1.dll` and `zlibwapi.dll` on Windows.
- zlib-ng 2.1.3 modern ABI

## Changelog

See [CHANGELOG.md](./CHANGELOG.md).

## Usage

See [USAGE.md](./USAGE.md).

## License

`Joveler.Compression.ZLib` is licensed under [zlib license](./LICENSE).
