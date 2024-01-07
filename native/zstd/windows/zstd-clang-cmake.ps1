
# -----------------------------------------------------------------------------
# Script parameters & banner
# -----------------------------------------------------------------------------
param (
    [Parameter(Mandatory=$true)]
    [string]$src = "",
    [Parameter(Mandatory=$true)]
    [string]$toolchain = "",
    [Parameter(Mandatory=$false)]
    [string]$radare2 = ""
)

# -----------------------------------------------------------------------------
# Set global directory paths & enviroment infomation
# -----------------------------------------------------------------------------
$BaseDir = $PSScriptRoot
$Cores = ${Env:NUMBER_OF_PROCESSORS}

$SrcDir = $src
$ToolchainDir = $toolchain
$Radare2Dir = $radare2
$Rabin2Exe = "${Radare2Dir}\bin\rabin2.exe"

$LibName = "libzstd.dll"
$LZ4ExeName = "zstd.exe"

# -----------------------------------------------------------------------------
# Check if 'rabin2' exists on PATH (CheckDep purpose)
# -----------------------------------------------------------------------------
Function Test-CommandExists {
    Param ($command)
    $oldPreference = $ErrorActionPreference
    $ErrorActionPreference = ‘stop’
    try {
        if (Get-Command $command) { 
            RETURN $true 
        }
    }
    Catch {
        RETURN $false
    }
    Finally {
        $ErrorActionPreference = $oldPreference
    }
}

$HasRadare2 = $false
if (Test-Path -Path "${Rabin2Exe}" -PathType Leaf) {
    $HasRadare2 = $true
}
elseif (Test-CommandExists rabin2) {
    $Rabin2Exe = "rabin2"
    $HasRadare2 = $true
}

# -------------------------------------------------------------------------
# Build Profiles
# -------------------------------------------------------------------------
$buildArches = @(
    "i686"
    "x86_64"
    "aarch64"
)

# -----------------------------------------------------------------------------
# Build per architectures
# -----------------------------------------------------------------------------
foreach ($buildArch in $buildArches) {    
    # -----------------------------------------------------------------------------
    # Set per-arch directory paths & enviroment infomation
    # -----------------------------------------------------------------------------
    $TargetArch = $buildArch
    $StripExe = "${ToolchainDir}\bin\${TargetArch}-w64-mingw32-strip.exe"

    # -------------------------------------------------------------------------
    # Prepare directories
    # -------------------------------------------------------------------------
    $BuildDir = "${SrcDir}\build-${TargetArch}"
    $DestDir = "${BaseDir}\build-${TargetArch}"
    Remove-Item "${BuildDir}" -Recurse -ErrorAction SilentlyContinue
    Remove-Item "${DestDir}" -Recurse -ErrorAction SilentlyContinue
    New-Item "${BuildDir}" -ItemType Directory -ErrorAction SilentlyContinue
    New-Item "${DestDir}" -ItemType Directory -ErrorAction SilentlyContinue

    # -------------------------------------------------------------------------
    # Configure zstd (with -Os)
    # -------------------------------------------------------------------------
    Write-Output ""
    Write-Host "[*] Configure zstd" -ForegroundColor Yellow
    Push-Location $BuildDir
    cmake ..\build\cmake -G "MinGW Makefiles" "-DCMAKE_MAKE_PROGRAM=${ToolchainDir}/bin/mingw32-make" `
        "-DCMAKE_TOOLCHAIN_FILE=${BaseDir}/llvm-mingw.cmake" `
        "-DCMAKE_SYSTEM_PROCESSOR=${TargetArch}" `
        "-DLLVM_MINGW=${ToolchainDir}" `
        "-DZSTD_BUILD_DYNAMIC=ON" `
        "-DZSTD_BUILD_STATIC=ON" `
        "-DZSTD_BUILD_PROGRAM=ON" `
        "-DZSTD_PROGRAMS_LINK_SHARED=OFF" `
        "-DZSTD_ZLIB_SUPPORT=OFF" `
        "-DZSTD_LZMA_SUPPORT=OFF" `
        "-DZSTD_LZ4_SUPPORT=OFF" `
        "-DZSTD_BUILD_TESTS=OFF" `
        "-DCMAKE_BUILD_TYPE=Release"
    # Benchmark: In zstd, -O3 is faster than -Os.
    # -O3 zstd: 12.064s to compression 7.7GB file
    # -Os zstd: 13.053s to compression 7.7GB file
    Pop-Location

    # "-DCMAKE_BUILD_TYPE=MinSizeRel" `
    # -------------------------------------------------------------------------
    # Build zstd
    # -------------------------------------------------------------------------
    Write-Output ""
    Write-Host "[*] Build zstd" -ForegroundColor Yellow
    Push-Location $BuildDir
    # TODO: --config not required because of `-DCMAKE_BUILD_TYPE`?
    cmake --build . --config Release --parallel "${Cores}"
    Pop-Location

    # -------------------------------------------------------------------------
    # Retrieve binaries
    # -------------------------------------------------------------------------
    Copy-Item "${BuildDir}\lib\${LibName}" "${DestDir}\${LibName}"
    Copy-Item "${BuildDir}\programs\${LZ4ExeName}" "${DestDir}\${LZ4ExeName}"

    # -------------------------------------------------------------------------
    # Strip binaries (Just in case)
    # -------------------------------------------------------------------------
    # Strip binaries
    Push-Location $DestDir
    Write-Host "[*] Strip binaries" -ForegroundColor Yellow
    Get-ChildItem -Filter *.dll
    Get-ChildItem -Filter *.exe
    & ${StripExe} "${LibName}"
    & ${StripExe} "${LZ4ExeName}"
    Get-ChildItem -Filter *.dll
    Get-ChildItem -Filter *.exe
    Pop-Location

    # Print dependency of binaries
    if ($HasRadare2) {
        Push-Location $DestDir
        Write-Host "[*] Linked libraries of [${LibName}]" -ForegroundColor Yellow
        & "${Rabin2Exe}" -Al "${LibName}"
        Write-Host "[*] Linked libraries of [${LZ4ExeName}]" -ForegroundColor Yellow
        & "${Rabin2Exe}" -Al "${LZ4ExeName}"
        Pop-Location
    }
    else {
        Write-Host "Install radare2 or pass radare2 directory to check depdencies." -ForegroundColor Yellow
    }
}

