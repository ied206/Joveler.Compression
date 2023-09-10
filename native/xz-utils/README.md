# Native Library Compile

This document explains how the embedded native binaries are compiled.

## Source

xz-utils source can be obtained from [homepage](https://tukaani.org/xz/).

## Windows - x86, x64, arm64

Compile with MSYS2 and `liblzma-msys2.sh`.

### 5.4.0

xz 5.4.0 MSVC vcxproj files has an issue regarding `lzma_stream_decoder_mt`.

Use MSYS2 as a workaround.

### 5.2.x

| Arch  | Obtain Method |
|-------|---------------|
| x86   | From official release, `xz-(ver)-windows\bin_i686-sse2\liblzma.dll` with strip |
| x64   | From official release, `xz-(ver)-windows\bin_x86-64\liblzma.dll` with strip |
| arm64 | Manual compile with MSVC 2019 |

#### Manual Compile ARM64 DLL

1. Open `windows\vs2019\xz_win.sln` with MSVC 2019
1. Select `liblzma_dll` project
1. Open `Property` in context menu
1. Create ARM64 target platform
   - Open `Configuration Manager`
   - Create ARM64 solution platform, using x64 as a template
1. Choose `ReleaseMT - All Platforms` build target
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
1. Build the project and obtain `liblzma.dll`

## Linux - x64, armhf, arm64

Linux .so files are built with default optimization.

Run `liblzma-posix.sh` with proper arguments.
 
## macOS - x64, arm64

macOS .dylib files are built with default optimization.

Run `liblzma-posix.sh` with proper arguments.

`xz` MachO binary for testing must be configured with `--disable-shared --enable-static`.
- [Reference](https://github.com/therootcompany/xz-static)
