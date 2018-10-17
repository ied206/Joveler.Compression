# Joveler.ZLib

C# pinvoke library for [zlib](https://zlib.net/).

Targets .Net Standard 1.3 and 2.0, supports Windows and Linux.

Based on [zlibnet](https://zlibnet.codeplex.com) by [@hardon](https://www.codeplex.com/site/users/view/hardon).

## Install

Joveler.ZLib can be installed via [nuget](https://www.nuget.org/packages/Joveler.ZLib/).

[![NuGet](https://buildstats.info/nuget/Joveler.ZLib)](https://www.nuget.org/packages/Joveler.ZLib)

## Features

- ZLibStream, the stream implementation conforms [RFC 1950](https://www.ietf.org/rfc/rfc1950.txt)
- Improved DeflateStream and GZipStream, conforming [RFC 1951](https://www.ietf.org/rfc/rfc1951.txt) and [RFC 1952](https://www.ietf.org/rfc/rfc1952.txt)
- Adler32 and CRC32 checksum

## Support

### Targeted .Net platforms

- .Net Standard 1.3 (.Net Framework 4.6+, .Net Core 1.0+)
- .Net Standard 2.0 (.Net Framework 4.6.1+, .Net Core 2.0+)

If you need .Net Framework 4.5 support, use [ZLibWrapper](https://github.com/ied206/ZLibWrapper) instead.

### Supported platforms

- Windows x86, x64
- Linux x64

### Supported zlib version

- 1.2.11 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## License

zlib license
