# Build lz4 native library

This document explains how the embedded native binaries are compiled.

## Source

LZ4 source can be obtained from [GitHub](https://github.com/lz4/lz4/releases).

## Windows - x86, x64, arm64

All build methods provided here build with size optimization.

lz4 implementation benefits a lot from code being cached into CPU. Size optimized code (`-Os`) is about 2x ~ 3x faster than perf optimized code (`-O3`) in C# warpped test.

### LLVM-mingw build

Build with cmake, and use llvm-mingw.

Run `lz4-clang-cmake.ps1` script on MSYS2 with proper arguments.

### MSVC Build

Windows .dll files are compiled with size optimization.

1. Open `build\VS2017\lz4.sln` with MSVC 2017 or later
1. Select `liblz4-dll` project
1. Open `Property` in context menu
1. Create ARM64 target platform
   - Open `Configuration Manager`
   - Create ARM64 solution platform, using x64 as a template
1. Choose `Release - All Platforms` build target
1. Set build configurations
1. Set build configurations
   - Korean
      - Set `C/C++` - `최적화` - `최적화` as `최대 최적화(크기 우선)(/O1)`
      - Set `C/C++` - `최적화` - `크기 또는 속도` as `코드 크기 우선(/Os)`
      - Set `C/C++` - `코드 생성` - `런타임 라이브러리` as `다중 스레드(/MT)`
      - Set `Linker` - `디버깅` - `디버그 정보 생성` as `아니요`
   - English
      - Set `C/C++` - `Optimization` - `Optimization` as `Minimum Size (/O1)`
      - Set `C/C++` - `Optimization` - `Small or Fast` as `Favor Small Code (/Os)`
      - Set `C/C++` - `Code Generation` - `Use Run-Time Library` as `Multi Thread(/MT)`
      - Set `Linker` - `Debugging` - `Generate Debug Info` as `None`
1. Build the project and obtain `liblz4.dll`

### Benchmark

#### Test Result

`-O3` build is much slower than `-Os` in the real world benchmark.

- Compression: `Clang -O3` takes about ~3x times to operate than `MSVC -Os` and `Clang -Os`.
- Decompression: `Clang -O3` takes about ~3x times to operate than `MSVC -Os` and `Clang -Os`.

#### Test Environment
```
BenchmarkDotNet v0.13.7, Windows 10 (10.0.19045.3324/22H2/2022Update)
AMD Ryzen 7 5800X, 1 CPU, 16 logical and 8 physical cores
.NET SDK 7.0.400
  [Host]     : .NET 6.0.21 (6.0.2123.36311), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.21 (6.0.2123.36311), X64 RyuJIT AVX2
```

#### `MSVC -Os` build:

- Compression

|                   Method |        SrcFileName |   Level |            Mean |         Error |        StdDev |          Median | CompRatio |
|------------------------- |------------------- |-------- |-----------------|---------------|---------------|-----------------|-----------|
|        'lz4 (n_Joveler)' |  bible_en_utf8.txt |    Best |   305,741.96 μs |  1,598.058 μs |  1,494.825 μs |   305,502.35 μs |     0.326 |
|           'lz4 (m_K4os)' |  bible_en_utf8.txt |    Best |   304,533.80 μs |    643.798 μs |    570.710 μs |   304,477.22 μs |     0.326 |
|        'lz4 (n_Joveler)' |  bible_en_utf8.txt | Default |   167,725.61 μs |  1,222.782 μs |  1,021.078 μs |   168,048.37 μs |     0.332 |
|           'lz4 (m_K4os)' |  bible_en_utf8.txt | Default |   180,631.87 μs |    311.879 μs |    260.433 μs |   180,706.93 μs |     0.332 |
|        'lz4 (n_Joveler)' |  bible_en_utf8.txt | Fastest |     9,659.87 μs |     76.630 μs |     67.931 μs |     9,661.19 μs |     0.487 |
|           'lz4 (m_K4os)' |  bible_en_utf8.txt | Fastest |    11,013.66 μs |     95.550 μs |     74.599 μs |    11,009.19 μs |     0.487 |

- Decompression

|                   Method |        SrcFileName |   Level |           Mean |         Error |        StdDev |
|--------------------------|--------------------|---------|----------------|---------------|---------------|
|        'lz4 (n_Joveler)' |  bible_en_utf8.txt |    Best |   5,290.126 μs |    82.6839 μs |    91.9030 μs |
|           'lz4 (m_K4os)' |  bible_en_utf8.txt |    Best |   5,228.397 μs |    82.2880 μs |    72.9462 μs |
|        'lz4 (n_Joveler)' |  bible_en_utf8.txt | Default |   5,405.272 μs |   101.5412 μs |   108.6479 μs |
|           'lz4 (m_K4os)' |  bible_en_utf8.txt | Default |   5,289.075 μs |    94.4705 μs |    88.3678 μs |
|        'lz4 (n_Joveler)' |  bible_en_utf8.txt | Fastest |   5,521.221 μs |    70.2738 μs |    62.2959 μs |
|           'lz4 (m_K4os)' |  bible_en_utf8.txt | Fastest |   5,456.930 μs |    66.1838 μs |    61.9084 μs |


#### `Clang -Os` build:

- Compression

|            Method |        SrcFileName |   Level |          Mean |        Error |       StdDev | CompRatio |
|------------------ |------------------- |-------- |--------------:|-------------:|-------------:|----------:|
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt |    Best | 267,321.05 μs | 1,122.537 μs |   995.100 μs |     0.326 |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt |    Best | 299,269.79 μs |   595.904 μs |   528.253 μs |     0.326 |
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt | Default | 170,366.15 μs |   852.800 μs |   755.985 μs |     0.332 |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt | Default | 181,571.25 μs |   300.562 μs |   281.145 μs |     0.332 |
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt | Fastest |   9,458.57 μs |    26.206 μs |    23.231 μs |     0.487 |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt | Fastest |  10,775.27 μs |    16.506 μs |    15.439 μs |     0.487 |

- Decompression

|            Method |        SrcFileName |   Level |          Mean |       Error |      StdDev |
|-------------------|--------------------|---------|---------------|-------------|-------------|
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt |    Best | 5,586.132 μs | 106.3311 μs | 113.7730 μs |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt |    Best | 5,269.631 μs | 101.4746 μs | 124.6199 μs |
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt | Default | 5,549.950 μs |  89.2034 μs |  83.4410 μs |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt | Default | 5,417.259 μs | 106.2162 μs | 122.3187 μs |
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt | Fastest | 5,764.209 μs |  77.5546 μs |  68.7501 μs |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt | Fastest | 5,488.818 μs |  99.3807 μs |  92.9607 μs |

#### `Clang -O3` build:

- Compression

|            Method |        SrcFileName |   Level |            Mean |        Error |       StdDev | CompRatio |
|------------------ |------------------- |-------- |----------------:|-------------:|-------------:|----------:|
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt |    Best |   525,643.17 μs | 2,709.793 μs | 2,402.161 μs |     0.326 |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt |    Best |   296,394.92 μs |   355.836 μs |   297.139 μs |     0.326 |
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt | Default |   356,432.95 μs | 2,160.464 μs | 2,020.899 μs |     0.332 |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt | Default |   181,279.57 μs |   193.452 μs |   171.490 μs |     0.332 |
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt | Fastest |    32,904.33 μs |   106.615 μs |    99.728 μs |     0.487 |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt | Fastest |    10,970.98 μs |   188.104 μs |   231.009 μs |     0.487 |

- Decompression

|            Method |        SrcFileName |   Level |          Mean |       Error |      StdDev |
|-------------------|--------------------|---------|---------------|-------------|-------------|
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt |    Best | 13,450.454 μs | 233.3756 μs | 218.2996 μs |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt |    Best |  5,424.127 μs |  66.0034 μs |  58.5103 μs |
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt | Default | 13,308.310 μs | 227.3635 μs | 212.6760 μs |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt | Default |  5,438.824 μs |  73.4869 μs |  68.7397 μs |
| 'lz4 (n_Joveler)' |  bible_en_utf8.txt | Fastest | 14,171.889 μs | 133.8003 μs | 125.1568 μs |
|    'lz4 (m_K4os)' |  bible_en_utf8.txt | Fastest |  5,496.780 μs |  41.3654 μs |  34.5420 μs |

## Linux - x64, armhf, arm64

Linux .so files are built with default optimization.

Run `lz4-posix.sh` with proper arguments.

## macOS - x64, arm64

macOS .dylib files are built with default optimization.

Run `lz4-posix.sh` with proper arguments.
