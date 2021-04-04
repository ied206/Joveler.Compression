# ChangeLog

## v4.x


### v4.1.0

Released in 2021-04-??

- Official support for Windows ARM64.
- Upgrade lz4 binaries to 1.9.3.

### v4.0.0

Released in 2020-05-26

- (BREAKING CHANGE) Native libraries are now placed following [NuGet convention-based working directory](https://docs.microsoft.com/en-US/nuget/create-packages/creating-a-package#create-the-nuspec-file) on .NET Standard build.

## v3.x

### v3.1.2

Released in 2020-02-04

- Fixed Joveler.Compression.LZ4 MSBuild script issue.

### v3.1.1

Released in 2019-11-01

- Improved RHEL/CentOS compatibility.

### v3.1.0

Released in 2019-10-20

- Initial release.
- Added macOS support.
- Applied improved native library loader ([Joveler.DynLoader](https://github.com/ied206/Joveler.DynLoader)).
