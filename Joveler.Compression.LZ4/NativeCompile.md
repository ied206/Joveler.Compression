# LZ4 Native Library Compile

This document explains how the embedded native binaries are compiled.

## Source

LZ4 source can be obtained from [GitHub](https://github.com/lz4/lz4/releases).

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
1. Strip `lib/liblz4.(ver).so`
   ```sh
   strip lib/liblz4.(ver).so
   ```
1. Make sure the binary does not have unnecessary dependency
   ```sh
   ldd lib/liblz4.(ver).so
   ```
 
## macOS - x64

macOS .dylib files are built with default optimization.

1. Build with standard Makefile.
   ```ssh
   make -j(N)
   ```
1. Strip `lib/liblz4.(ver).dylib`
   ```sh
   strip -S -x lib/liblz4.(ver).dylib
   ```
1. Make sure the binary does not have unnecessary dependency
   ```sh
   ottol -L `lib/liblz4.(ver).dylib`
   ```
