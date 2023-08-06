# Joveler.Compression.XZ

[XZ Utils](https://tukaani.org/xz/) (liblzma) pinvoke library for .NET.

## Install

Joveler.Compression.XZ can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.XZ/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.XZ)](https://www.nuget.org/packages/Joveler.Compression.XZ)

## Features

- XZStream, the stream for [.xz file format](https://tukaani.org/xz/xz-file-format.txt).
- Fast native implementation of CRC32 and CRC64 checksums.

## Support

### Targeted .NET platforms

- .NET Core 3.1
- .NET Standard 2.0
- .NET Framework 4.6

#### Discontinued frameworks

| Platform | Last Supported Version |
|----------|------------------------|
| .NET Standard 1.3 | [v1.1.2](https://www.nuget.org/packages/Joveler.Compression.XZ/1.1.2) |
| .NET Framework 4.5.1 | [v4.1.0](https://www.nuget.org/packages/ManagedWimLib/4.1.0) |

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

### Supported XZ Utils version

- 5.4.3 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## Changelog

See [CHANGELOG.md](./CHANGELOG.md).

## License

`Joveler.Compression.XZ` is licensed under [MIT license](./LICENSE).
