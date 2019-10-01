# Joveler.Compression.LZ4

C# pinvoke library for [LZ4](https://github.com/lz4/lz4).

Targets .Net Standard 2.0, supports Windows and Linux.

## Status

Joveler.Compression.LZ4 is similar or slower (~10x) than pure C# implementation, [K4os.Compression.LZ4](https://github.com/MiloszKrajewski/K4os.Compression.LZ4). Overhead of P/Invoke can be one reason. Will not be uploaded to nuget unless it surpasses `K4os.Compression.LZ4`'s performance.

## Features

- LZ4FrameStream, the stream for [lz4 frame format](https://github.com/lz4/lz4/blob/master/doc/lz4_Frame_format.md).

## Support

### Targeted .Net platforms

- .Net Framework 4.5.1
- .Net Standard 2.0 (.Net Framework 4.6.1+, .Net Core 2.0+)

### Supported OS platforms

| Platform | Architecture  | Tested |
|----------|---------------|--------|
| Windows  | x86           | Yes    |
|          | x64           | Yes    |
| Linux    | x64           | Yes    |
|          | armhf         | Yes    |
|          | arm64         | Yes    |

#### Tested linux distributions

| Architecture  | Distribution | Note |
|---------------|--------------|------|
| x64           | Ubuntu 18.04 |      |
| armhf         | Debian 9     | Emulated on QEMU's virt board |
| arm64         | Debian 9     | Emulated on QEMU's virt board |

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
