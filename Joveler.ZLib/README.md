# Joveler.ZLib

C# pinvoke library for [zlib](https://zlib.net/).

Targets .Net Standard 2.0, supports Windows and Linux.

Based on [zlibnet](https://zlibnet.codeplex.com) by [@hardon](https://www.codeplex.com/site/users/view/hardon).

## Install

Joveler.ZLib can be installed via [nuget](https://www.nuget.org/packages/Joveler.ZLib/).

[![NuGet](https://buildstats.info/nuget/Joveler.ZLib)](https://www.nuget.org/packages/Joveler.ZLib)

## Features

- ZLibStream, the stream implementation conforms [RFC 1950](https://www.ietf.org/rfc/rfc1950.txt)
- Improved DeflateStream and GZipStream, conforming [RFC 1951](https://www.ietf.org/rfc/rfc1951.txt) and [RFC 1952](https://www.ietf.org/rfc/rfc1952.txt)
- Adler32 and CRC32 checksum

## Support

### Supported platforms

- Windows x86, x64
- Linux x64

### Supported zlib version

- 1.2.11 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## License

zlib license
