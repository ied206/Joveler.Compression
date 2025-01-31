# Joveler.Compression.ZLib

Cross-platform [zlib](https://zlib.net/) pinvoke library for .NET.

## Features

Joveler.Compression.ZLib exposes fast zlib capabilities with backed by zlib-ng.

- ZLibStream, the stream implementation conforms [RFC 1950](https://www.ietf.org/rfc/rfc1950.txt).
- Improved DeflateStream and GZipStream, conforming [RFC 1951](https://www.ietf.org/rfc/rfc1951.txt) and [RFC 1952](https://www.ietf.org/rfc/rfc1952.txt).
- (EXPERIMENTAL) Parallel compression support on ZLibStream, DeflateStream and GZipStream.
- Fast native implementation of Adler32 and CRC32 checksum.

## Support

### Targeted .NET platforms

- .NET 8.0
- .NET Standard 2.0
- .NET Framework 4.6.2

### Supported OS platforms

| Platform | Architecture | Minimum Target | Tested |
|----------|--------------|----------------|--------|
| Windows  | x86          | Windows 7 SP1  | Yes    |
|          | x64          | Windows 7 SP1  | Yes    |
|          | arm64        | Windows 7 SP1  | Yes    |
| Linux    | x64          | Ubuntu 20.04   | Yes    |
|          | armhf        | Ubuntu 20.04   | Yes    |
|          | arm64        | Ubuntu 20.04   | Yes    |
| macOS    | x64          | macOS 11       | Yes    |
|          | arm64        | macOS 11       | Yes    |

### Supported zlib versions

- zlib-ng 2.2.3 compat ABI (Included)
    - Compatible with traditional zlib ABI, such as `zlib1.dll`.
- zlib 1.3
    - Supports both `zlib1.dll` and `zlibwapi.dll` on Windows.
- zlib-ng 2.2.3 modern ABI

## Usage

Please refer to the project homepage.
