# Joveler.Compression.LZ4

[LZ4](https://github.com/lz4/lz4) pinvoke library for .NET.

## Install

Joveler.Compression.LZ4 can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.LZ4/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.LZ4)](https://www.nuget.org/packages/Joveler.Compression.LZ4)

## Performance

**WARNING**: The library supports many customizable options, but performance is a bit slow due to pinvoke overhead. See [README.md](https://github.com/ied206/Joveler.Compression/blob/v4.1.0/Joveler.Compression.LZ4/README.md) for details.

## Features

- LZ4FrameStream, the stream for [lz4 frame format](https://github.com/lz4/lz4/blob/master/doc/lz4_Frame_format.md).
- (EXPERIMENTAL) Parallel compression support on LZ4FrameStream.

## Tested liblz4 versions

- 1.9.4 (Included)

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

### Supported LZ4 versions

- 1.10.0 (Included)
