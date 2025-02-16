# ChangeLog

## v5.x

### v5.0.2

- Update NuGet README.

### v5.0.1

Released on 2025-02-10

- Fix broken .NET Framework MSBuild script for copying native DLLs.

### v5.0.0

Released on 2025-01-31

- (EXPERIMENTAL) Provides parallel lz4 compression.
- (BREAKING CHANGE) Now targets .NET 8.0/.NET Framework 4.6.2/.NET Standard 2.0, to use TPL Dataflow library.
- Upgrades packaged lz4 binaries to 1.10.0.
- Adds support for nullable reference type information.

## v4.x

### v4.1.0

Released on 2021-04-05

- Official support for Windows ARM64.
- Upgrade lz4 binaries to 1.9.3.

### v4.0.0

Released on 2020-05-26

- (BREAKING CHANGE) Native libraries are now placed following [NuGet convention-based working directory](https://docs.microsoft.com/en-US/nuget/create-packages/creating-a-package#create-the-nuspec-file) on .NET Standard build.

## v3.x

### v3.1.2

Released on 2020-02-04

- Fixed Joveler.Compression.LZ4 MSBuild script issue.

### v3.1.1

Released on 2019-11-01

- Improved RHEL/CentOS compatibility.

### v3.1.0

Released on 2019-10-20

- Initial release.
- Added macOS support.
- Applied improved native library loader ([Joveler.DynLoader](https://github.com/ied206/Joveler.DynLoader)).
