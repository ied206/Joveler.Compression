# zstd Native Library Compile

This document explains how the embedded native binaries are compiled.

## Source

zstd source can be obtained from [GitHub](https://github.com/facebook/zstd/releases).

## Windows - x64, x86, arm64

zstd favors `-O3` than `-Os`. Perf optimized code is about 10% faster than size optimized code.

### LLVM-mingw build (default)

Install cmake, and run `zstd-clang-cmake.ps1` script with proper arguments.

### MSVC build

Windows .dll files are compiled with default optimization.

1. Open `build\VS2017\zstd.sln` with MSVC 2017 or later
1. Select `libzstd-dll` project
1. Open `Property` in context menu
1. Create ARM64 target platform
   - Open `Configuration Manager`
   - Create ARM64 solution platform, using x64 as a template
1. Choose `Release - All Platforms` build target
1. Set build configurations
   - Set `C/C++` - `Code Generation` - `Use Run-Time Library` as `Multi Thread(/MT)`
   - Set `Linker` - `Debugging` - `Generate Debug Info` as `None`
1. Build the project and obtain `libzstd.dll`

## Linux - x64, armhf, arm64

Linux .so files are built with default optimization.

1. Build with standard Makefile
   ```sh
   make -j(N)
   ```
1. Strip `lib/libzstd.(ver).so`
   ```sh
   strip lib/libzstd.(ver).so
   ```
1. Make sure the binary does not have unnecessary dependency
   ```sh
   ldd lib/libzstd.(ver).so
   ```

Patch `zstd` cli Makefile to prevent it from linking to unnecessary dependency.
- Set `HAVE_ZLIB`, `HAVE_LZ4`, `HAVE_LZMA` to 0.

## macOS - x64, arm64

macOS .dylib files are built with default optimization.

1. Build with standard Makefile.
   ```ssh
   make -j(N)
   ```
1. Strip `lib/libzstd.(ver).dylib`
   ```sh
   strip -S -x lib/libzstd.(ver).dylib
   ```
1. Make sure the binary does not have unnecessary dependency
   ```sh
   otool -L `lib/libzstd.(ver).dylib`
   ```

Patch `zstd` cli Makefile to prevent it from linking to unnecessary dependency.
- Set `HAVE_ZLIB`, `HAVE_LZ4`, `HAVE_LZMA` to 0.
