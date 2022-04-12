# zstd Native Library Compile

This document explains how the embedded native binaries are compiled.

## Source

zstd source can be obtained from [GitHub](https://github.com/facebook/zstd/releases).

## Windows - x86, x64, arm64

Windows .dll files are compiled with size optimization.

1. Open `build\VS2017\lz4.sln` with MSVC 2017 or later
1. Select `liblz4-dll` project
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
1. Build the project and obtain `liblz4.dll`

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
