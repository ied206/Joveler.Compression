# Joveler.Compression.LZ4

C# pinvoke library for [LZ4](https://github.com/lz4/lz4).

Targets .Net Standard 2.0, supports multiple platforms such as Windows, Linux and macOS.

## Install

Joveler.Compression.LZ4 can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.LZ4/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.LZ4)](https://www.nuget.org/packages/Joveler.Compression.LZ4)

## Performance

Please keep in mind that `Joveler.Compression.LZ4` have been uploaded to the nuget because it supports many customizable options, not best in performance.

Decompression of `Joveler.Compression.LZ4` is similar or slightly faster than the pure C# implementation, [K4os.Compression.LZ4](https://github.com/MiloszKrajewski/K4os.Compression.LZ4). In the meanwhile the compression is about 50% faster to 2000% slower. Please be careful about performance when using it on production.

## Features

- LZ4FrameStream, the stream for [lz4 frame format](https://github.com/lz4/lz4/blob/master/doc/lz4_Frame_format.md).

## Support

### Targeted .Net platforms

- .Net Framework 4.5.1
- .Net Standard 2.0 (.Net Framework 4.6.1+, .Net Core 2.0+)

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

### Supported LZ4 version

- 1.8.1.2
- 1.8.2
- 1.8.3
- 1.9.1
- 1.9.2 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## License

BSD 2-Clause license
