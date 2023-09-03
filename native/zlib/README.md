# Build zlib native library

This document explains how to obtain zlib native binaries.

## Source

zlib source can be obtained from its [homepage](https://zlib.net).

pigz source can be obtainced from its [homepage](https://zlib.net/pigz/).

## Windows - x86, x64, arm64

### LLVM-mingw Build

Run `zlib-msys2.sh` and `pigz-msys2.sh` script on MSYS2 with proper arguments.

### MSVC Build

Build MSVC .dll files with size optimization.

1. Open `contrib\vstudio\vc14\zlibvc.sln` with MSVC 2017 or later
1. Select `zlibvc` project
1. Open `Property` in context menu
1. Create ARM64 target platform
   - Open `Configuration Manager`
   - Create ARM64 solution platform, using x64 as a template
1. Choose `ReleaseWithoutAsm - All Platforms` build target
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
1. Build the project and obtain `zlibwapi.dll`

## Linux - x64, armhf, arm64

Linux .so files are built with default optimization.

Run `zlib-posix.sh` with proper arguments.

## macOS - x64, arm64

macOS .dylib files are built with default optimization.

Run `zlib-posix.sh` with proper arguments.
