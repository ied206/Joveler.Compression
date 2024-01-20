# Patches required to use clang on zlib-ng 2.1.5

## Remove DLL prefix `lib`

CMake by default use `lib` prefix on mingw-produced DLL files. However, zlib already has the word `lib` in its name, and MSVC compiled zlib does not start with `lib` by convention.

Apply `CMakeList.diff` to disable `lib` prefix.

```sh
cd zlib-ng-2.1.5
patch -p1 < $REPO/native/windows/llvm-mingw-patch-2.1.5/CMakeList.diff
```
