# Joveler.Compression.ZLib

C# pinvoke library for [zlib](https://zlib.net/).

Targets .Net Standard 2.0, supports Windows and Linux.

`DefalteStream` and its familiy is based on the code of [zlibnet](https://zlibnet.codeplex.com) by [@hardon](https://www.codeplex.com/site/users/view/hardon).

## Install

Joveler.Compression.ZLib can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.ZLib/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.ZLib)](https://www.nuget.org/packages/Joveler.Compression.ZLib)

## Features

- ZLibStream, the stream implementation conforms [RFC 1950](https://www.ietf.org/rfc/rfc1950.txt)
- Improved DeflateStream and GZipStream, conforming [RFC 1951](https://www.ietf.org/rfc/rfc1951.txt) and [RFC 1952](https://www.ietf.org/rfc/rfc1952.txt)
- Adler32 and CRC32 checksum

## Support

### Targeted .Net platforms

- .Net Framework 4.5.1
- .Net Standard 2.0 (.Net Framework 4.6.1+, .Net Core 2.0+)

If you need .Net Framework 4.5 support, use [ZLibWrapper](https://www.nuget.org/packages/Joveler.ZLibWrapper) instead.  
If you need .Net Standard 1.3 support, use [v2.1.2](https://www.nuget.org/packages/Joveler.Compression.ZLib/2.1.2) instead.

### Supported OS platforms

| Platform | Architecture  | Tested |
|----------|---------------|--------|
| Windows  | x86           | Yes    |
|          | x64           | Yes    |
| Linux    | x64           | Yes    |
|          | armhf         | Yes    |
|          | arm64         | Yes    |

#### Tested linux distributions

| Architecture  | Distribution | Note |
|---------------|--------------|------|
| x64           | Ubuntu 18.04 |      |
| armhf         | Debian 9     | Emulated on QEMU's virt board |
| arm64         | Debian 9     | Emulated on QEMU's virt board |

### Supported zlib version

- 1.2.11 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## License

zlib license
