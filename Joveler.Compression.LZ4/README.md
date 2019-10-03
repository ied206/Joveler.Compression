# Joveler.Compression.LZ4

C# pinvoke library for [LZ4](https://github.com/lz4/lz4).

Targets .Net Standard 2.0, supports Windows and Linux.

## Performance

### Decompression

Decompression of `Joveler.Compression.LZ4` is similar or slightly faster than `K4os.Compression.LZ4`.

#### Benchmark Results

- Date : 20191004
- Commit : [829cadc](https://github.com/ied206/Joveler.Compression/commit/829cadc0a07b061029100d0d5675e560dacaf5bc)
- Command : `cd Benchmark && dotnet run -c Release -- decomp`

| Compression             | File       | Level   | Mean          | Compare      |
|-------------------------|------------|---------|---------------|--------------|
| Joveler.Compression.LZ4 | Banner.bmp | Best    |    315.028 us |   13% Faster |
| K4os.Compression.LZ4    | Banner.bmp | Best    |    361.436 us |              |
| Joveler.Compression.LZ4 | Banner.bmp | Default |    404.360 us |    1% Slower |
| K4os.Compression.LZ4    | Banner.bmp | Default |    399.565 us |              |
| Joveler.Compression.LZ4 | Banner.bmp | Fastest |    339.002 us |   14% Faster |
| K4os.Compression.LZ4    | Banner.bmp | Fastset |    393.509 us |              |
| Joveler.Compression.LZ4 | Banner.svg | Best    |     11.194 us |   24% Faster |
| K4os.Compression.LZ4    | Banner.svg | Best    |     14.659 us |              |
| Joveler.Compression.LZ4 | Banner.svg | Default |     11.937 us |   24% Faster |
| K4os.Compression.LZ4    | Banner.svg | Default |     15.637 us |              |
| Joveler.Compression.LZ4 | Banner.svg | Fastest |     12.006 us |   24% Faster |
| K4os.Compression.LZ4    | Banner.svg | Fastset |     15.694 us |              |
| Joveler.Compression.LZ4 |  Type4.txt | Best    |      9.613 us |    5% Faster |
| K4os.Compression.LZ4    |  Type4.txt | Best    |     10.168 us |              |
| Joveler.Compression.LZ4 |  Type4.txt | Default |      9.703 us |    6% Faster |
| K4os.Compression.LZ4    |  Type4.txt | Default |     10.296 us |              |
| Joveler.Compression.LZ4 |  Type4.txt | Fastest |      9.597 us |   10% Faster |
| K4os.Compression.LZ4    |  Type4.txt | Fastset |     10.713 us |              |

### Compression

Compression of `Joveler.Compression.LZ4` is about 50% faster to 2000% slower than the pure C# implementation, [K4os.Compression.LZ4](https://github.com/MiloszKrajewski/K4os.Compression.LZ4).

#### Benchmark Results

- Date : 20191004
- Commit : [829cadc](https://github.com/ied206/Joveler.Compression/commit/829cadc0a07b061029100d0d5675e560dacaf5bc)
- Command : `cd Benchmark && dotnet run -c Release -- comp`

| Compression             | File       | Level   | Mean          | Compare      |
|-------------------------|------------|---------|---------------|--------------|
| Joveler.Compression.LZ4 | Banner.bmp | Best    |  48,631.84 us |   54% Faster |
| K4os.Compression.LZ4    | Banner.bmp | Best    | 105,982.07 us |              |
| Joveler.Compression.LZ4 | Banner.bmp | Default |  12,572.43 us |   49% Faster |
| K4os.Compression.LZ4    | Banner.bmp | Default |  24,698.41 us |              |
| Joveler.Compression.LZ4 | Banner.bmp | Fastest |     765.22 us |  202% Slower |
| K4os.Compression.LZ4    | Banner.bmp | Fastset |     253.25 us |              |
| Joveler.Compression.LZ4 | Banner.svg | Best    |   1,104.02 us |   40% Slower |
| K4os.Compression.LZ4    | Banner.svg | Best    |     789.44 us |              |
| Joveler.Compression.LZ4 | Banner.svg | Default |     771.97 us |  106% Slower |
| K4os.Compression.LZ4    | Banner.svg | Default |     374.76 us |              |
| Joveler.Compression.LZ4 | Banner.svg | Fastest |     647.33 us |  973% Slower |
| K4os.Compression.LZ4    | Banner.svg | Fastset |      60.16 us |              |
| Joveler.Compression.LZ4 |  Type4.txt | Best    |    10.6491 us |  697% Slower |
| K4os.Compression.LZ4    |  Type4.txt | Best    |     1.3365 us |              |
| Joveler.Compression.LZ4 |  Type4.txt | Default |    14.9995 us | 1912% Slower |
| K4os.Compression.LZ4    |  Type4.txt | Default |     0.7456 us |              |
| Joveler.Compression.LZ4 |  Type4.txt | Fastest |     8.2716 us | 1368% Slower |
| K4os.Compression.LZ4    |  Type4.txt | Fastset |     0.5636 us |              |

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
