# Joveler.Compression.XZ

C# pinvoke library for [XZ Utils](https://tukaani.org/xz/).

Targets .Net Standard 2.0, supports Windows and Linux.

## Install

Joveler.Compression.XZ can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.XZ/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.XZ)](https://www.nuget.org/packages/Joveler.Compression.XZ)

## Features

- XZStream, the stream for [.xz file format](https://tukaani.org/xz/xz-file-format.txt).

## Support

### Targeted .Net platforms

- .Net Framework 4.5.1
- .Net Standard 1.3 (.Net Framework 4.6+, .Net Core 1.0+)
- .Net Standard 2.0 (.Net Framework 4.6.1+, .Net Core 2.0+)

### Supported platforms

| Platform | Architecture | Tested |
|----------|--------------|--------|
| Windows  | x86, x64     | Yes    |
| Linux    | x64, armhf   | Yes    |
|          | arm64        | No     |

#### Tested linux distributions

| Architecture | Distribution | Note |
|--------------|--------------|------|
| x64          | Ubuntu 18.04 |      |
| armhf        | Debian 9     | Emulated on QEMU's virt board |

### Supported XZ Utils version

- 5.2.2
- 5.2.3
- 5.2.4 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## License

MIT license
