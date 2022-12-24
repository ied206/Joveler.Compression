# Benchmark

## 2022-06-03

### Test Environment

| Environment | Info            |
|-------------|-----------------|
| CPU         | AMD Ryzen 5800X |
| OS          | Windows 10 21H2 |
| Runtime     | .NET 6.0.5      |

### Test Method

- Test Files
    - Banner.bmp
    - Banner.svg
    - Type4.txt
- Compression Codec
    - Native methods from Joveler.Compression
    - Managed methods 
        - [SharpCompress v0.31.0](https://github.com/adamhathcock/sharpcompress) - `zlib`, `xz`
        - [K4os.Compression.LZ4](https://github.com/MiloszKrajewski/K4os.Compression.LZ4) - `lz4`
        - [ZstdSharp v0.6.1](https://github.com/oleg-st/ZstdSharp) - `zstd`

### Compression

|       Method | SrcFileName |   Level |          Mean |        Error |       StdDev |        Median |
|------------- |------------ |-------- |--------------:|-------------:|-------------:|--------------:|
|   LZ4_Native |  Banner.bmp |    Best |  30,656.59 us |   111.487 us |   104.285 us |  30,628.74 us |
|  LZ4_Managed |  Banner.bmp |    Best |  31,752.52 us |    79.429 us |    70.412 us |  31,742.31 us |
|  ZLib_Native |  Banner.bmp |    Best |  11,084.07 us |    28.152 us |    26.334 us |  11,084.33 us |
| ZLib_Managed |  Banner.bmp |    Best |  19,787.20 us |    84.655 us |    79.186 us |  19,787.72 us |
|    XZ_Native |  Banner.bmp |    Best |  26,667.78 us |    69.476 us |    64.988 us |  26,666.11 us |
|  ZSTD_Native |  Banner.bmp |    Best | 200,979.92 us | 1,737.601 us | 1,625.353 us | 201,099.70 us |
| ZSTD_Managed |  Banner.bmp |    Best | 172,544.75 us |   918.031 us |   858.727 us | 172,396.63 us |
|   LZ4_Native |  Banner.bmp | Default |   9,065.73 us |    42.044 us |    39.328 us |   9,062.55 us |
|  LZ4_Managed |  Banner.bmp | Default |   9,316.23 us |    45.016 us |    42.108 us |   9,316.79 us |
|  ZLib_Native |  Banner.bmp | Default |   2,176.38 us |     7.524 us |     7.038 us |   2,174.17 us |
| ZLib_Managed |  Banner.bmp | Default |   3,549.29 us |     8.590 us |     7.615 us |   3,548.86 us |
|    XZ_Native |  Banner.bmp | Default |  25,399.77 us |    96.178 us |    85.259 us |  25,396.52 us |
|  ZSTD_Native |  Banner.bmp | Default |     407.84 us |     3.020 us |     2.677 us |     406.94 us |
| ZSTD_Managed |  Banner.bmp | Default |     412.37 us |     1.837 us |     1.718 us |     412.36 us |
|   LZ4_Native |  Banner.bmp | Fastest |     365.27 us |     1.971 us |     1.844 us |     364.86 us |
|  LZ4_Managed |  Banner.bmp | Fastest |     136.97 us |     0.580 us |     0.542 us |     136.76 us |
|  ZLib_Native |  Banner.bmp | Fastest |     751.17 us |     3.749 us |     3.130 us |     751.41 us |
| ZLib_Managed |  Banner.bmp | Fastest |   1,159.32 us |     2.105 us |     1.969 us |   1,159.10 us |
|    XZ_Native |  Banner.bmp | Fastest |   3,107.32 us |     6.125 us |     5.430 us |   3,107.15 us |
|  ZSTD_Native |  Banner.bmp | Fastest |     289.07 us |     1.037 us |     0.970 us |     288.95 us |
| ZSTD_Managed |  Banner.bmp | Fastest |     278.71 us |     0.526 us |     0.439 us |     278.63 us |
|   LZ4_Native |  Banner.svg |    Best |     582.98 us |     5.126 us |     4.795 us |     581.32 us |
|  LZ4_Managed |  Banner.svg |    Best |     430.62 us |     3.302 us |     2.927 us |     431.57 us |
|  ZLib_Native |  Banner.svg |    Best |     240.92 us |     0.981 us |     0.918 us |     240.68 us |
| ZLib_Managed |  Banner.svg |    Best |     343.09 us |     1.143 us |     1.069 us |     342.90 us |
|    XZ_Native |  Banner.svg |    Best |   4,045.96 us |    30.008 us |    28.069 us |   4,056.25 us |
|  ZSTD_Native |  Banner.svg |    Best |  91,252.96 us |   791.890 us |   740.735 us |  91,077.57 us |
| ZSTD_Managed |  Banner.svg |    Best |  90,710.81 us |   648.762 us |   606.852 us |  90,838.90 us |
|   LZ4_Native |  Banner.svg | Default |     326.72 us |     2.526 us |     2.363 us |     327.33 us |
|  LZ4_Managed |  Banner.svg | Default |     180.39 us |     1.600 us |     1.418 us |     180.87 us |
|  ZLib_Native |  Banner.svg | Default |     231.27 us |     1.012 us |     0.897 us |     231.23 us |
| ZLib_Managed |  Banner.svg | Default |     321.70 us |     1.235 us |     1.095 us |     321.32 us |
|    XZ_Native |  Banner.svg | Default |   3,438.41 us |    23.676 us |    22.147 us |   3,442.01 us |
|  ZSTD_Native |  Banner.svg | Default |     205.86 us |     0.633 us |     0.592 us |     205.75 us |
| ZSTD_Managed |  Banner.svg | Default |     194.07 us |     1.044 us |     0.976 us |     194.47 us |
|   LZ4_Native |  Banner.svg | Fastest |     989.58 us |    93.001 us |   274.215 us |   1,081.13 us |
|  LZ4_Managed |  Banner.svg | Fastest |      30.61 us |     0.080 us |     0.075 us |      30.62 us |
|  ZLib_Native |  Banner.svg | Fastest |     104.03 us |     0.987 us |     0.924 us |     104.14 us |
| ZLib_Managed |  Banner.svg | Fastest |     135.68 us |     0.856 us |     0.800 us |     135.55 us |
|    XZ_Native |  Banner.svg | Fastest |     593.46 us |     2.538 us |     2.374 us |     593.01 us |
|  ZSTD_Native |  Banner.svg | Fastest |      96.69 us |     0.418 us |     0.371 us |      96.59 us |
| ZSTD_Managed |  Banner.svg | Fastest |      78.16 us |     0.267 us |     0.249 us |      78.11 us |
|   LZ4_Native |   Type4.txt |    Best |     383.66 us |     2.065 us |     1.932 us |     383.48 us |
|  LZ4_Managed |   Type4.txt |    Best |     282.00 us |     1.141 us |     1.067 us |     281.60 us |
|  ZLib_Native |   Type4.txt |    Best |     330.09 us |     2.168 us |     2.028 us |     329.07 us |
| ZLib_Managed |   Type4.txt |    Best |     458.78 us |     0.921 us |     0.817 us |     458.64 us |
|    XZ_Native |   Type4.txt |    Best |  10,436.20 us |   196.419 us |   183.731 us |  10,518.22 us |
|  ZSTD_Native |   Type4.txt |    Best |  81,666.38 us |   325.966 us |   254.493 us |  81,699.74 us |
| ZSTD_Managed |   Type4.txt |    Best |  85,178.81 us |   366.417 us |   324.819 us |  85,263.15 us |
|   LZ4_Native |   Type4.txt | Default |     340.22 us |     2.416 us |     2.260 us |     341.05 us |
|  LZ4_Managed |   Type4.txt | Default |     250.01 us |     0.454 us |     0.402 us |     250.04 us |
|  ZLib_Native |   Type4.txt | Default |     330.64 us |     2.093 us |     1.958 us |     330.83 us |
| ZLib_Managed |   Type4.txt | Default |     461.59 us |     0.702 us |     0.622 us |     461.41 us |
|    XZ_Native |   Type4.txt | Default |   6,221.98 us |    48.288 us |    45.169 us |   6,221.97 us |
|  ZSTD_Native |   Type4.txt | Default |     199.41 us |     0.536 us |     0.502 us |     199.43 us |
| ZSTD_Managed |   Type4.txt | Default |     195.41 us |     0.763 us |     0.714 us |     195.28 us |
|   LZ4_Native |   Type4.txt | Fastest |   1,535.59 us |   101.234 us |   298.490 us |   1,603.18 us |
|  LZ4_Managed |   Type4.txt | Fastest |      10.74 us |     0.055 us |     0.046 us |      10.74 us |
|  ZLib_Native |   Type4.txt | Fastest |     269.54 us |     1.960 us |     1.834 us |     269.71 us |
| ZLib_Managed |   Type4.txt | Fastest |     385.28 us |     1.180 us |     1.104 us |     385.36 us |
|    XZ_Native |   Type4.txt | Fastest |   1,326.87 us |     2.878 us |     2.551 us |   1,326.77 us |
|  ZSTD_Native |   Type4.txt | Fastest |     105.38 us |     0.599 us |     0.560 us |     105.35 us |
| ZSTD_Managed |   Type4.txt | Fastest |     100.93 us |     0.389 us |     0.364 us |     100.90 us |

### Decompression

|       Method | SrcFileName |   Level |          Mean |        Error |       StdDev |        Median |
|------------- |------------ |-------- |--------------:|-------------:|-------------:|--------------:|
|   LZ4_Native |  Banner.bmp |    Best |  30,656.59 μs |   111.487 μs |   104.285 μs |  30,628.74 μs |
|  LZ4_Managed |  Banner.bmp |    Best |  31,752.52 μs |    79.429 μs |    70.412 μs |  31,742.31 μs |
|  ZLib_Native |  Banner.bmp |    Best |  11,084.07 μs |    28.152 μs |    26.334 μs |  11,084.33 μs |
| ZLib_Managed |  Banner.bmp |    Best |  19,787.20 μs |    84.655 μs |    79.186 μs |  19,787.72 μs |
|    XZ_Native |  Banner.bmp |    Best |  26,667.78 μs |    69.476 μs |    64.988 μs |  26,666.11 μs |
|  ZSTD_Native |  Banner.bmp |    Best | 200,979.92 μs | 1,737.601 μs | 1,625.353 μs | 201,099.70 μs |
| ZSTD_Managed |  Banner.bmp |    Best | 172,544.75 μs |   918.031 μs |   858.727 μs | 172,396.63 μs |
|   LZ4_Native |  Banner.bmp | Default |   9,065.73 μs |    42.044 μs |    39.328 μs |   9,062.55 μs |
|  LZ4_Managed |  Banner.bmp | Default |   9,316.23 μs |    45.016 μs |    42.108 μs |   9,316.79 μs |
|  ZLib_Native |  Banner.bmp | Default |   2,176.38 μs |     7.524 μs |     7.038 μs |   2,174.17 μs |
| ZLib_Managed |  Banner.bmp | Default |   3,549.29 μs |     8.590 μs |     7.615 μs |   3,548.86 μs |
|    XZ_Native |  Banner.bmp | Default |  25,399.77 μs |    96.178 μs |    85.259 μs |  25,396.52 μs |
|  ZSTD_Native |  Banner.bmp | Default |     407.84 μs |     3.020 μs |     2.677 μs |     406.94 μs |
| ZSTD_Managed |  Banner.bmp | Default |     412.37 μs |     1.837 μs |     1.718 μs |     412.36 μs |
|   LZ4_Native |  Banner.bmp | Fastest |     365.27 μs |     1.971 μs |     1.844 μs |     364.86 μs |
|  LZ4_Managed |  Banner.bmp | Fastest |     136.97 μs |     0.580 μs |     0.542 μs |     136.76 μs |
|  ZLib_Native |  Banner.bmp | Fastest |     751.17 μs |     3.749 μs |     3.130 μs |     751.41 μs |
| ZLib_Managed |  Banner.bmp | Fastest |   1,159.32 μs |     2.105 μs |     1.969 μs |   1,159.10 μs |
|    XZ_Native |  Banner.bmp | Fastest |   3,107.32 μs |     6.125 μs |     5.430 μs |   3,107.15 μs |
|  ZSTD_Native |  Banner.bmp | Fastest |     289.07 μs |     1.037 μs |     0.970 μs |     288.95 μs |
| ZSTD_Managed |  Banner.bmp | Fastest |     278.71 μs |     0.526 μs |     0.439 μs |     278.63 μs |
|   LZ4_Native |  Banner.svg |    Best |     582.98 μs |     5.126 μs |     4.795 μs |     581.32 μs |
|  LZ4_Managed |  Banner.svg |    Best |     430.62 μs |     3.302 μs |     2.927 μs |     431.57 μs |
|  ZLib_Native |  Banner.svg |    Best |     240.92 μs |     0.981 μs |     0.918 μs |     240.68 μs |
| ZLib_Managed |  Banner.svg |    Best |     343.09 μs |     1.143 μs |     1.069 μs |     342.90 μs |
|    XZ_Native |  Banner.svg |    Best |   4,045.96 μs |    30.008 μs |    28.069 μs |   4,056.25 μs |
|  ZSTD_Native |  Banner.svg |    Best |  91,252.96 μs |   791.890 μs |   740.735 μs |  91,077.57 μs |
| ZSTD_Managed |  Banner.svg |    Best |  90,710.81 μs |   648.762 μs |   606.852 μs |  90,838.90 μs |
|   LZ4_Native |  Banner.svg | Default |     326.72 μs |     2.526 μs |     2.363 μs |     327.33 μs |
|  LZ4_Managed |  Banner.svg | Default |     180.39 μs |     1.600 μs |     1.418 μs |     180.87 μs |
|  ZLib_Native |  Banner.svg | Default |     231.27 μs |     1.012 μs |     0.897 μs |     231.23 μs |
| ZLib_Managed |  Banner.svg | Default |     321.70 μs |     1.235 μs |     1.095 μs |     321.32 μs |
|    XZ_Native |  Banner.svg | Default |   3,438.41 μs |    23.676 μs |    22.147 μs |   3,442.01 μs |
|  ZSTD_Native |  Banner.svg | Default |     205.86 μs |     0.633 μs |     0.592 μs |     205.75 μs |
| ZSTD_Managed |  Banner.svg | Default |     194.07 μs |     1.044 μs |     0.976 μs |     194.47 μs |
|   LZ4_Native |  Banner.svg | Fastest |     989.58 μs |    93.001 μs |   274.215 μs |   1,081.13 μs |
|  LZ4_Managed |  Banner.svg | Fastest |      30.61 μs |     0.080 μs |     0.075 μs |      30.62 μs |
|  ZLib_Native |  Banner.svg | Fastest |     104.03 μs |     0.987 μs |     0.924 μs |     104.14 μs |
| ZLib_Managed |  Banner.svg | Fastest |     135.68 μs |     0.856 μs |     0.800 μs |     135.55 μs |
|    XZ_Native |  Banner.svg | Fastest |     593.46 μs |     2.538 μs |     2.374 μs |     593.01 μs |
|  ZSTD_Native |  Banner.svg | Fastest |      96.69 μs |     0.418 μs |     0.371 μs |      96.59 μs |
| ZSTD_Managed |  Banner.svg | Fastest |      78.16 μs |     0.267 μs |     0.249 μs |      78.11 μs |
|   LZ4_Native |   Type4.txt |    Best |     383.66 μs |     2.065 μs |     1.932 μs |     383.48 μs |
|  LZ4_Managed |   Type4.txt |    Best |     282.00 μs |     1.141 μs |     1.067 μs |     281.60 μs |
|  ZLib_Native |   Type4.txt |    Best |     330.09 μs |     2.168 μs |     2.028 μs |     329.07 μs |
| ZLib_Managed |   Type4.txt |    Best |     458.78 μs |     0.921 μs |     0.817 μs |     458.64 μs |
|    XZ_Native |   Type4.txt |    Best |  10,436.20 μs |   196.419 μs |   183.731 μs |  10,518.22 μs |
|  ZSTD_Native |   Type4.txt |    Best |  81,666.38 μs |   325.966 μs |   254.493 μs |  81,699.74 μs |
| ZSTD_Managed |   Type4.txt |    Best |  85,178.81 μs |   366.417 μs |   324.819 μs |  85,263.15 μs |
|   LZ4_Native |   Type4.txt | Default |     340.22 μs |     2.416 μs |     2.260 μs |     341.05 μs |
|  LZ4_Managed |   Type4.txt | Default |     250.01 μs |     0.454 μs |     0.402 μs |     250.04 μs |
|  ZLib_Native |   Type4.txt | Default |     330.64 μs |     2.093 μs |     1.958 μs |     330.83 μs |
| ZLib_Managed |   Type4.txt | Default |     461.59 μs |     0.702 μs |     0.622 μs |     461.41 μs |
|    XZ_Native |   Type4.txt | Default |   6,221.98 μs |    48.288 μs |    45.169 μs |   6,221.97 μs |
|  ZSTD_Native |   Type4.txt | Default |     199.41 μs |     0.536 μs |     0.502 μs |     199.43 μs |
| ZSTD_Managed |   Type4.txt | Default |     195.41 μs |     0.763 μs |     0.714 μs |     195.28 μs |
|   LZ4_Native |   Type4.txt | Fastest |   1,535.59 μs |   101.234 μs |   298.490 μs |   1,603.18 μs |
|  LZ4_Managed |   Type4.txt | Fastest |      10.74 μs |     0.055 μs |     0.046 μs |      10.74 μs |
|  ZLib_Native |   Type4.txt | Fastest |     269.54 μs |     1.960 μs |     1.834 μs |     269.71 μs |
| ZLib_Managed |   Type4.txt | Fastest |     385.28 μs |     1.180 μs |     1.104 μs |     385.36 μs |
|    XZ_Native |   Type4.txt | Fastest |   1,326.87 μs |     2.878 μs |     2.551 μs |   1,326.77 μs |
|  ZSTD_Native |   Type4.txt | Fastest |     105.38 μs |     0.599 μs |     0.560 μs |     105.35 μs |
| ZSTD_Managed |   Type4.txt | Fastest |     100.93 μs |     0.389 μs |     0.364 μs |     100.90 μs |