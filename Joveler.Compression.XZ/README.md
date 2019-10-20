# Joveler.Compression.XZ

[XZ Utils](https://tukaani.org/xz/) (liblzma) pinvoke library for .Net.

Targets .Net Standard 2.0, supports multiple platforms such as Windows, Linux and macOS.

## Install

Joveler.Compression.XZ can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.XZ/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.XZ)](https://www.nuget.org/packages/Joveler.Compression.XZ)

## Features

- XZStream, the stream for [.xz file format](https://tukaani.org/xz/xz-file-format.txt).

## Support

### Targeted .Net platforms

- .Net Framework 4.5.1
- .Net Standard 2.0 (.Net Framework 4.6.1+, .Net Core 2.0+)

If you need .Net Standard 1.3 support, use [v1.1.2](https://www.nuget.org/packages/Joveler.Compression.XZ/1.1.2) instead.

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
- 5.2.4 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## License

MIT license
