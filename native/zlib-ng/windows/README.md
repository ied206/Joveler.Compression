# Compile zlib-ng for Windows

Use CMake and llvm-mingw to compile Windows zlib-ng dlls.

## Required Tools

- [CMake](https://cmake.org/)
- [llvm-mingw](https://github.com/mstorsjo/llvm-mingw)
    - Tested with llvm-mingw 20230614 with LLVM stable 16.0.6.
- (Optinal) [Radare2](https://github.com/radareorg/radare2/releases)
    - Dependency check of the compile script depends on radare2. 
    - Put radare2 in the PATH or specify the exact location as an argument.

## Build Manual

1. Extract the `zlib-ng` source code.
1. Open Powershell.
1. Run `zlib-ng-cmake.ps1`.
    - You must pass a path of `llvm-mingw`.
    - Radare2 is required only if you want to track the dependency of the compiled binaries.
    - The script will build all x86, x64, arm64 binaries and both compat/modern ABIs.
    ```
    [Examples]
    .\zlib-ng-cmake.ps1 -toolchain /c/build/llvm-mingw -src /c/build/native/zlib-ng-2.1.5
    ```
1. Gather binaries from the `build-*` directory.

## Patches required to use clang on zlib-ng

See [README.md](./llvm-mingw-patch-2.1.5/README.md).
