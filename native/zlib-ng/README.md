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

### Linux - armhf in arm64

Recent ARMv8 SoCs dropped the ability to run an aarch32 VM on an aarch64 host. We can detour this issue by cross-compiling, then testing on aarch32 userspace on arm64 system.

First, install gcc for armhf to cross-compile native libraries.

```bash
sudo apt install gcc-arm-linux-gnueabihf
```

Then setup aarch32 userspace on arm64 host.

```bash
sudo dpkg --add-architecture armhf
```

Edit `sources.list` to unsert `[arch=arm64,armhf]' between 'deb' and 'http'.

```bash
sudo vim /etc/apt/sources.lists
```

```
[AS-IS]
deb http://ftp.kr.debian.org/debian/ bookworm main
[TO-BE]
deb [arch=arm64,armhf] http://ftp.kr.debian.org/debian/ bookworm main
```

Install C/C++ runtime library for armhf.

```bash
sudo apt update
sudo apt install libc6:armhf libstdc++6:armhf 
```

Now you can run .NET armhf binary on arm64 host.


## macOS - x64, arm64

Install cmake, and run `zlib-ng-posix.sh` with proper arguments.
