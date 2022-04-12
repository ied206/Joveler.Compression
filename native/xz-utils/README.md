# Native Library Compile

This document explains how the embedded native binaries are compiled.

## Source

xz-utils source can be obtained from [homepage](https://tukaani.org/xz/).

## Windows - x86, x64, arm64

| Arch  | Obtain Method |
|-------|---------------|
| x86   | From official release, `xz-(ver)-windows\bin_i686-sse2\liblzma.dll` with strip |
| x64   | From official release, `xz-(ver)-windows\bin_x86-64\liblzma.dll` with strip |
| arm64 | Manual compile with MSVC 2019 |

### Manual Compile ARM64 DLL

1. Open `windows\vs2019\xz_win.sln` with MSVC 2019
1. Select `liblzma_dll` project
1. Open `Property` in context menu
1. Create ARM64 target platform
   - Open `Configuration Manager`
   - Create ARM64 solution platform, using x64 as a template
1. Choose `Release - All Platforms` build target
1. Set build configurations
   - Set `C/C++` - `Optimization` - `Optimization` as `Minimum Size (/O1)`
   - Set `C/C++` - `Optimization` - `Small or Fast` as `Favor Small Code (/Os)`
   - Set `C/C++` - `Code Generation` - `Use Run-Time Library` as `Multi Thread(/MT)`
   - Set `Linker` - `Debugging` - `Generate Debug Info` as `None`
1. Build the project and obtain `liblzma.dll`

## Linux - x64, armhf, arm64

Linux .so files are built with default optimization.

1. Build with standard Makefile
   ```sh
   make -j(N)
   ```
1. Strip `lib/liblzma.(ver).so`
   ```sh
   strip lib/liblzma.(ver).so
   ```
1. Make sure the binary does not have unnecessary dependency
   ```sh
   ldd lib/liblzma.(ver).so
   ```
 
## macOS - x64, arm64

macOS .dylib files are built with default optimization.

1. Build with standard Makefile.
   ```ssh
   make -j(N)
   ```
1. Strip `lib/liblzma.(ver).dylib`
   ```sh
   strip -S -x lib/liblzma.(ver).dylib
   ```
1. Make sure the binary does not have unnecessary dependency
   ```sh
   otool -L `lib/liblzma.(ver).dylib`
   ```

`xz` MachO binary for testing must be configured with `--disable-shared --enable-static`.
- [Reference](https://github.com/therootcompany/xz-static)
