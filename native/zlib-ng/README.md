# Build zlib-ng native library

This document explains how the embedded native binaries are compiled.

## Source

zlib-ng source can be obtained from its [GitHub repo](https://github.com/zlib-ng/zlib-ng).

## Windows - x86, x64, arm64

### LLVM-mingw Build

Install cmake, and run `zlib-ng-cmake.ps1` script with proper arguments.

You may need to patch zlib-ng source.

## Linux - x64, armhf, arm64

Install cmake, and run `zlib-ng-posix.sh` with proper arguments.

## macOS - x64, arm64

Install cmake, and run `zlib-ng-posix.sh` with proper arguments.
