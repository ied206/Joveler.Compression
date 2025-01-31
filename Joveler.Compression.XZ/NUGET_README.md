# Joveler.Compression.XZ

[XZ Utils](https://tukaani.org/xz/) (liblzma) pinvoke library for .NET.

## Features

- XZStream, the stream for [.xz file format](https://tukaani.org/xz/xz-file-format.txt).
- Fast native implementation of CRC32 and CRC64 checksums.

## Usage

Please refer the [project homepage](https://github.com/ied206/Joveler.Compression/blob/master/Joveler.Compression.XZ/USAGE.md).

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

### Supported XZ Utils versions

- 5.6.4 (Included)

