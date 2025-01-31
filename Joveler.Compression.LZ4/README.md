# Joveler.Compression.LZ4

[LZ4](https://github.com/lz4/lz4) pinvoke library for .NET.

## Install

Joveler.Compression.LZ4 can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.LZ4/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.LZ4)](https://www.nuget.org/packages/Joveler.Compression.LZ4)

## Performance

Please keep in mind that `Joveler.Compression.LZ4` have been uploaded to the nuget because it supports many customizable options, not best in performance.

Decompression of `Joveler.Compression.LZ4` is similar or slightly faster than the pure C# implementation, [K4os.Compression.LZ4](https://github.com/MiloszKrajewski/K4os.Compression.LZ4). In the meanwhile the compression is about 50% faster to 2000% slower. Please be careful about performance when using it on production.

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

See [USAGE.md](./USAGE.md).

## Changelog

See [CHANGELOG.md](./CHANGELOG.md).

## License

`Joveler.Compression.LZ4` is licensed under [BSD 2-Clause license](./LICENSE).

## Performance

### Compression

In singlethread compression, `Joveler.Compression.LZ4` ties with [K4os.Compression.LZ4](https://github.com/MiloszKrajewski/K4os.Compression.LZ4).

In multithread compression, performance of `Joveler.Compression.LZ4` scales linearly when data requires a time to be compressed.

| Method | SrcFileName       | Level   | Mean       |
|--------|-------------------|---------|------------|
| lz4    | bible_en_utf8.txt | Default | 155,165 μs |
| lz4-T2 | bible_en_utf8.txt | Default | 156,913 μs |
| K4os   | bible_en_utf8.txt | Default | 171,872 μs |
| lz4    | bible_en_utf8.txt | Fastest | 8,658 μs   |
| lz4-T2 | bible_en_utf8.txt | Fastest | 10,089 μs  |
| K4os   | bible_en_utf8.txt | Fastest | 9,562 μs   |
| lz4    | bible_kr_utf8.txt | Default | 194,444 μs |
| lz4-T2 | bible_kr_utf8.txt | Default | 172,994 μs |
| K4os   | bible_kr_utf8.txt | Default | 207,119 μs |
| lz4    | bible_kr_utf8.txt | Fastest | 10,219 μs  |
| lz4-T2 | bible_kr_utf8.txt | Fastest | 10,460 μs  |
| K4os   | bible_kr_utf8.txt | Fastest | 11,406 μs  |
| lz4    | ooffice.dll       | Default | 140,671 μs |
| lz4-T2 | ooffice.dll       | Default | 101,661 μs |
| K4os   | ooffice.dll       | Default | 156,425 μs |
| lz4    | ooffice.dll       | Fastest | 10,762 μs  |
| lz4-T2 | ooffice.dll       | Fastest | 9,557 μs   |
| K4os   | ooffice.dll       | Fastest | 13,531 μs  |
| lz4    | reymont.pdf       | Default | 335,907 μs |
| lz4-T2 | reymont.pdf       | Default | 218,780 μs |
| K4os   | reymont.pdf       | Default | 363,534 μs |
| lz4    | reymont.pdf       | Fastest | 13,985 μs  |
| lz4-T2 | reymont.pdf       | Fastest | 10,868 μs  |
| K4os   | reymont.pdf       | Fastest | 15,343 μs  |
