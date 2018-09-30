# Joveler.ZLib

C# wrapper for native zlib.

Targets .Net Standard 2.0, supports Windows and Linux.

Based on [zlibnet](https://zlibnet.codeplex.com) by [@hardon](https://www.codeplex.com/site/users/view/hardon).

| Branch    | Build Status   |
|-----------|----------------|
| Master    | [![CI Master Branch Build Status](https://ci.appveyor.com/api/projects/status/9t1fg4vyavqowb3p/branch/master?svg=true)](https://ci.appveyor.com/project/ied206/joveler-compression/branch/master) |
| Develop   | [![CI Develop Branch Build Status](https://ci.appveyor.com/api/projects/status/9t1fg4vyavqowb3p/branch/develop?svg=true)](https://ci.appveyor.com/project/ied206/joveler-compression/branch/develop) |

## Install

Joveler.ZLib can be installed via [nuget](https://www.nuget.org/packages/Joveler.ZLib/).

[![NuGet](https://buildstats.info/nuget/Joveler.ZLib)](https://www.nuget.org/packages/Joveler.ZLib)

## Features

- ZLibStream, a stream implementation conforms [RFC 1950](https://www.ietf.org/rfc/rfc1950.txt)
- Improved DeflateStream and GZipStream, conforming [RFC 1951](https://www.ietf.org/rfc/rfc1951.txt) and [RFC 1952](https://www.ietf.org/rfc/rfc1952.txt)
- Adler32 and CRC32 checksum

## Support

### Supported platforms

- Windows x86, x64
- Linux x64

### Supported zlib version

- zlib 1.2.11

## Usage

See [USAGE.md](./USAGE.md).

## License

Licensed under zlib license.
