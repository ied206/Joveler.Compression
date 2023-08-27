# ChangeLog

## v4.x

### v4.2.3

Released in 2023-08-27

- Update xz-utils to 5.4.4.

### v4.2.2

Released in 2023-08-06

- Update xz-utils to 5.4.3.

### v4.2.1

Released in 2023-02-16

- Fix .NET Framework build script path issue.

### v4.2.0

Released in 2023-02-16

- Supports xz-utils 5.4.1.
- Target .NET Framework 4.6 instead of [deprecated 4.5.1](https://devblogs.microsoft.com/dotnet/net-framework-4-5-2-4-6-4-6-1-will-reach-end-of-support-on-april-26-2022/).

### v4.1.0

Released in 2021-04-05

- Official support for Windows ARM64.

Released in 2020-05-26

- (BREAKING CHANGE) Native libraries are now placed following [NuGet convention-based working directory](https://docs.microsoft.com/en-US/nuget/create-packages/creating-a-package#create-the-nuspec-file) on .NET Standard build.
- Updated liblzma to 5.2.5.

## v3.x

**NOTE**: The major version was bumped to v3.x from v1.x to match with Joveler.Compression.ZLib.

### v3.1.1

Released in 2019-11-01

- Improved RHEL/CentOS compatibility.

### v3.1.0

Released in 2019-10-20

- Added macOS support.
- Applied improved native library loader ([Joveler.DynLoader](https://github.com/ied206/Joveler.DynLoader)).

### v3.0.0

Released in 2019-10-02

- (BREAKING CHANGE) Redesigned public APIs.
- Exposes CRC32 and CRC64 API of XZ-Utils.
- Supports advanced configuration of XZStream

## v1.x

### v1.2.0

Released in 2019-08-18

- Adds native support for `Span<T>` APIs.
- Dropped .Net Standard 1.3 support.
- Supports dll with additional dependencies.

### v1.1.1, v1.1.2

Released in 2018-10-30

- Supports armhf, arm64 on Linux.

### v1.1.0

Released in 2018-10-18

- Targets .NET Framework 4.5.1, .NET Standard 1.3 and 2.0.

### v1.0.0

Released in 2018-10-01

- Initial release.
