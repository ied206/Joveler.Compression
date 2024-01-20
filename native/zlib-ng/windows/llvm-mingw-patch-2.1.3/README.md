# Patches required to use clang on zlib-ng 2.1.3

## Disable `-Wno-pedantic-ms-format` on llvm-mingw

Applying `CMakeList.patch` fixes this issue.

zlib-ng `CMakeList.txt` think any mingw toolchain would support `-Wno-pedantic-ms-format` parameter. But llvm-mingw does not. `CMakeList.patch` adds an additional check to add such argument only if the mingw toolchain is actually GCC.

```sh
cd zlib-ng-2.1.3
patch -p1 < $REPO/native/windows/llvm-mingw-patch-2.1.3/CMakeList.diff
```

## Include `<arm_neon.h>` instead of `<arm64_neon.h>` in ARM64 build

zlib-ng relies on compiler intrinsic support for its SIMD implementations.

MinGW-w64 and MSVC has `<arm64_neon.h>` for ARMv8 NEON structures, but llvm-mingw does not have such header.

So as a temporary measure, patch it to include `<arm_neon.h>` instead.

```sh
cd zlib-ng-2.1.3
patch -p1 < $REPO/native/windows/llvm-mingw-patch-2.1.3/NeonHeader.diff
```
