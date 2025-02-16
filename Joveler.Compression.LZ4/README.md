# Joveler.Compression.LZ4

[LZ4](https://github.com/lz4/lz4) pinvoke library for .NET.

## Install

Joveler.Compression.LZ4 can be installed via [nuget](https://www.nuget.org/packages/Joveler.Compression.LZ4/).

[![NuGet](https://buildstats.info/nuget/Joveler.Compression.LZ4)](https://www.nuget.org/packages/Joveler.Compression.LZ4)

## Performance

Due to LZ4's performant nature, P/Invoke overhead has a more negative effect on LZ4 than on conventional compression algorithms.

If you mostly handle big files and focus on multithreaded high-level compression, `Joveler.Compression.LZ4` is the right choice.

If you mostly handle small files and are focused on performant single-threaded use, consider using [K4os.Compression.LZ4](https://github.com/MiloszKjewski/K4os.Compression.LZ4), the pure C# implementation. Its performance generally ties in with the native pinvoke method. 

Suppose your use case involves mostly decompression, use [K4os.Compression.LZ4](https://github.com/MiloszKjewski/K4os.Compression.LZ4) as it is consistently faster.

Read the [Benchmark](#Benchmark) section for more details.

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

## Benchmark

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

### Decompression

[K4os.Compression.LZ4](https://github.com/MiloszKrajewski/K4os.Compression.LZ4) is about twice faster than `Joveler.Compression.LZ4`.

| Method | SrcFileName       | Level   | Mean       |
|--------|-------------------|---------|------------|
| lz4    | bible_en_utf8.txt  | Default | 4,598.377 μs |
| K4os   | bible_en_utf8.txt  | Default | 2,728.262 μs |
| lz4    | bible_en_utf8.txt  | Fastest | 4,966.349 μs |
| K4os   | bible_en_utf8.txt  | Fastest | 2,807.782 μs |
| lz4    | bible_kr_utf8.txt  | Default | 5,673.352 μs |
| K4os   | bible_kr_utf8.txt  | Default | 3,845.430 μs |
| lz4    | bible_kr_utf8.txt  | Fastest | 6,114.985 μs |
| K4os   | bible_kr_utf8.txt  | Fastest | 3,753.987 μs |
| lz4    | ooffice.dll       | Default | 7,170.132 μs |
| K4os   | ooffice.dll       | Default | 4,635.963 μs | 
| lz4    | ooffice.dll       | Fastest | 7,587.325 μs | 
| K4os   | ooffice.dll       | Fastest | 4,743.326 μs | 
| lz4    | reymont.pdf       | Default | 6,546.353 μs | 
| K4os   | reymont.pdf       | Default | 4,529.416 μs | 
| lz4    | reymont.pdf       | Fastest | 7,243.303 μs | 
| K4os   | reymont.pdf       | Fastest | 4,760.949 μs | 
