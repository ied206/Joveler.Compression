# Joveler.Compression.LZ4

[LZ4](https://github.com/lz4/lz4) pinvoke library for .NET.

## Install

Joveler.Compression.LZ4 can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.LZ4/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.LZ4)](https://www.nuget.org/packages/Joveler.Compression.LZ4)

## Performance

**WARNING**: Due to LZ4's performant nature, P/Invoke overhead has a more negative effect on LZ4 than on conventional compression algorithms.

The overhead becomes trivial enough if you handle big files and focus on multithreaded high-level compression.

In decompression, pure managed implementation is much faster than this native wrapper.

See project README for more details.

## Features

- LZ4FrameStream, the stream for [lz4 frame format](https://github.com/lz4/lz4/blob/master/doc/lz4_Frame_format.md).
- (EXPERIMENTAL) Parallel compression support on LZ4FrameStream.

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

## Usage

Please refer to the project homepage.
