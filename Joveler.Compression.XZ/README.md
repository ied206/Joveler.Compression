# Joveler.Compression.XZ

[XZ Utils](https://tukaani.org/xz/) (liblzma) pinvoke library for .NET.

## Install

Joveler.Compression.XZ can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.XZ/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.XZ)](https://www.nuget.org/packages/Joveler.Compression.XZ)

## Features

- XZStream, the stream for [.xz file format](https://tukaani.org/xz/xz-file-format.txt).

## Support

### Targeted .NET platforms

- .NET Standard 2.1 (.NET Core 3.0+)
- .NET Standard 2.0 (.NET Framework 4.6.1+, .NET Core 2.0+)
- .NET Framework 4.5.1

If you need .NET Standard 1.3 support, use [v1.1.2](https://www.nuget.org/packages/Joveler.Compression.XZ/1.1.2) instead.

### Supported OS platforms

| Platform | Architecture | Tested |
|----------|--------------|--------|
| Windows  | x86          | Yes    |
|          | x64          | Yes    |
| Linux    | x64          | Yes    |
|          | armhf        | Yes    |
|          | arm64        | Yes    |
| macOS    | x64          | Yes    |

#### Tested linux distributions

| Architecture  | Distribution | Note |
|---------------|--------------|------|
| x64           | Ubuntu 18.04 | Tested on AppVeyor CI         |
| armhf         | Debian 10    | Emulated on QEMU's virt board |
| arm64         | Debian 10    | Emulated on QEMU's virt board |

### Supported XZ Utils version

- 5.2.2
- 5.2.3
- 5.2.4
- 5.2.5 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## Changelog

See [CHANGELOG.md](./CHANGELOG.md).

## License

`Joveler.Compression.XZ` is licensed under [MIT license](./LICENSE).
