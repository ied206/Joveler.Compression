
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

$LibName = "liblz4.dll"
$LZ4ExeName = "lz4.exe"

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
    # Configure lz4 (with -Os)
    # -------------------------------------------------------------------------
    Write-Output ""
    Write-Host "[*] Configure lz4" -ForegroundColor Yellow
    Push-Location $BuildDir
    cmake ..\build\cmake -G "MinGW Makefiles" "-DCMAKE_MAKE_PROGRAM=${ToolchainDir}/bin/mingw32-make" `
        "-DCMAKE_TOOLCHAIN_FILE=${BaseDir}/llvm-mingw.cmake" `
        "-DCMAKE_SYSTEM_PROCESSOR=${TargetArch}" `
        "-DLLVM_MINGW=${ToolchainDir}" `
        "-DCPACK_SOURCE_ZIP=OFF" `
        "-DCPACK_SOURCE_7Z=OFF" `
        "-DCPACK_BINARY_NSIS=OFF" ` 
        "-DCMAKE_BUILD_TYPE=MinSizeRel" 
    # Benchmark: MSVC -Os build is much faster than Clang -O3 build.
    # It seems CMAKE_BUILD_TYPE must be denoted in configure time, not a build time.
    Pop-Location

    # -------------------------------------------------------------------------
    # Build lz4
    # -------------------------------------------------------------------------
    Write-Output ""
    Write-Host "[*] Build lz4" -ForegroundColor Yellow
    Push-Location $BuildDir
    cmake --build . --config MinSizeRel --parallel "${Cores}"
    Pop-Location

    # -------------------------------------------------------------------------
    # Retrieve binaries
    # -------------------------------------------------------------------------
    Copy-Item "${BuildDir}\${LibName}" "${DestDir}\${LibName}"
    Copy-Item "${BuildDir}\${LZ4ExeName}" "${DestDir}\${LZ4ExeName}"

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

