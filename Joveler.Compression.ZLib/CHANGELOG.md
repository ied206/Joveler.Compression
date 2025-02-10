# ChangeLog

## v6.x

### v6.0.1

Released in 2025-02-10

- Fix broken .NET Framework MSBuild script for copying native DLLs.

### v6.0.0

Released in 2025-01-31

- (EXPERIMENTAL) Provides parallel zlib compression.
- (BREAKING CHANGE) Now targets .NET 8.0/.NET Framework 4.6.2/.NET Standard 2.0, to use TPL Dataflow library.
- Provides APIs for combining two checksums.
- Upgrades packaged zlib-ng binaries to 2.2.3.

## v5.x

### v5.0.0

Released on 2023-09-10

- (BREAKING CHANGE) The library now ships `zlib1.dll` (zlib-ng compat ABI) instead of `zlibwapi.dll` (zlib stdcall ABI) for faster performance.
    - NuGet packages will contain zlib-ng fork binaries instead of upstream zlib ones.
    - Please refer to USAGE.md for proper initialization.
- Supports multiple zlib ABIs.
    - All `zlib (cdecl)` (default), `zlib (stdcall)`, `zlib-ng (cdecl)` ABIs are supported.
- (BREAKING CHANGE) `DeflateStream`, `ZLibStream`, `GZipStream` and helper classes are now sealed for better performance.
    - ABI will not break unless you have created a derived class of zlib streams.
- Add `ZLibInit.TryGlobalCleanup()`, which tries to silently unload native zlib instance even though if it has not been loaded.
- Retargets .NET Framework 4.6.

## v4.x

### v4.2.0

Released on 2022-12-25

- Recompile Windows zlib binaries with proper compile options.
- Discontinue .NET Framework 4.5.1, 4.6, 4.6.1 due to end of official Microsoft support.

### v4.1.0

Released on 2021-04-05

- Official support for Windows ARM64.
- Fix a bug of `ResetFunctions()` not clearing deflate/inflate delegates on Windows.

### v4.0.0

Released on 2020-05-26

- (BREAKING CHANGE) Native libraries are now placed following [NuGet convention-based working directory](https://docs.microsoft.com/en-US/nuget/create-packages/creating-a-package#create-the-nuspec-file) on .NET Standard build.

## v3.x

### v3.1.1

Released on 2019-11-01

- Improved RHEL/CentOS compatibility.

### v3.1.0

Released on 2019-10-20

- Added macOS support.
- Applied improved native library loader ([Joveler.DynLoader](https://github.com/ied206/Joveler.DynLoader)).

### v3.0.0

Released on 2019-10-02

- (BREAKING CHANGES) Redesigned public APIs.
- Supports the advanced configuration of `DeflateStream` and its family.

## v2.x

Starting from v2.x, Joveler.Compression.ZLib supported Windows and Linux.

### v2.2.0

Released on 2019-08-18

- Adds native support for `Span<T>` APIs.
- Dropped .Net Standard 1.3 support.
- Supports dll with additional dependencies.
- `Compressor` classes were removed.

### v2.1.1, v2.1.2

Released on 2018-10-30

- Supports armhf, arm64 on Linux.

### v2.1.0

Released on 2018-10-01

- Target .NET Framework 4.5.1, .NET Standard 1.3 and 2.0.

### v2.0.0

Released on 2018-10-01

- Support .NET Standard and Linux.

## v1.x ()

v1.x of the `Joveler.Compression.ZLib` was released under the name of [ZLibWrapper](https://github.com/ied206/ZLibWrapper), which targeted the .NET Framework only.

### v1.3.1

Released on 2018-09-06

- Uses 64K as a default buffer size.

### v1.3.0

Released on 2018-05-23

- The codebase and public API was refactored.

### v1.2.0

Released on 2018-01-21

- Added static wrapper methods that accept `Stream` to `Crc32Checksum` and `Adler32Checksum`.

### v1.1.0

Released on 2017-10-05

- Multi target .NET Framework 4.0 and .NET Framework 4.5.
- Change the namespace to `Joveler.ZLibWrapper` from `ZLibWrapper`.
- Improved copy of embedded precompiled binaries.

### v1.0.0

Released on 2017-10-04

- Initial release.
- Supported compress and decompress classes:
    - DeflateStream, ZLibStream, GZipStream
    - DeflateCompressor, ZLibCompressor, GZipCompressor
- Supported checksum classes:
    - Crc32Stream, Adler32Stream
    - Crc32Checksum, Adler32Checksum
