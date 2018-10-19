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
- .Net Standard 1.3 (.Net Framework 4.6+, .Net Core 1.0+)
- .Net Standard 2.0 (.Net Framework 4.6.1+, .Net Core 2.0+)

### Supported platforms

- Windows x86, x64
- Linux x64

### Supported LZ4 version

- 1.8.1.2
- 1.8.2
- 1.8.3 (Included)

## Usage

See [USAGE.md](./USAGE.md).

## License

BSD 2-Clause license
