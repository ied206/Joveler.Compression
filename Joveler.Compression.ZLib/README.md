# Joveler.Compression.ZLib

[zlib](https://zlib.net/) pinvoke library for .NET.

`DefalteStream` and its familiy is based on the code of [zlibnet](https://zlibnet.codeplex.com) by [@hardon](https://www.codeplex.com/site/users/view/hardon).

## Install

Joveler.Compression.ZLib can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.ZLib/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.ZLib)](https://www.nuget.org/packages/Joveler.Compression.ZLib)

## Features

- ZLibStream, the stream implementation conforms [RFC 1950](https://www.ietf.org/rfc/rfc1950.txt)
- Improved DeflateStream and GZipStream, conforming [RFC 1951](https://www.ietf.org/rfc/rfc1951.txt) and [RFC 1952](https://www.ietf.org/rfc/rfc1952.txt)
- Adler32 and CRC32 checksum

## Support

### Targeted .NET platforms

- .NET Core 3.1+
- .NET Standard 2.0 (.NET Framework 4.6.1+, .NET Core 2.0+)
- .NET Framework 4.6.2+

#### Discontinued target frameworks

| Platform | Last Supported Version |
|----------|------------------------|
| .NET Framework 4.5 | [ZLibWrapper](https://www.nuget.org/packages/Joveler.ZLibWrapper) |
| .NET Standard 1.3 | [v2.1.2](https://www.nuget.org/packages/Joveler.Compression.ZLib/2.1.2) |
| .NET Framework 4.5.1, 4.6, 4.6.1 | [v4.1.0](https://www.nuget.org/packages/Joveler.Compression.ZLib/4.1.0) |

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

macOS arm64 should be supported on theory, but I do not have access to an Apple Sillicon device to test. Please contribute if you have an ARM64 macOS machine.

#### Tested linux distributions

| Architecture  | Distribution | Note |
|---------------|--------------|------|
| x64           | Ubuntu 18.04 | Tested on AppVeyor CI |
| armhf         | Debian 10    | Emulated on QEMU      |
| arm64         | Debian 10    | Emulated on QEMU      |

### Supported zlib version

- 1.2.11 (Included)

## Changelog

See [CHANGELOG.md](./CHANGELOG.md).

## Usage

See [USAGE.md](./USAGE.md).

## License

`Joveler.Compression.ZLib` is licensed under [zlib license](./LICENSE).
