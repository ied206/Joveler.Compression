
# -----------------------------------------------------------------------------
# Script parameters & banner
# -----------------------------------------------------------------------------
param (
    [Parameter(Mandatory=$true)]
    [string]$src = "",
    [Parameter(Mandatory=$true)]
    [string]$toolchain = "",
    #[Parameter(Mandatory=$true)]
    #[string]$arch = "x86_64",
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

$GzipExeName = "minigzip.exe"
$DeflateExeName = "minideflate.exe"

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
# Available publish modes
enum BuildConfig
{
    ZLibCompat
    NoZLibCompat
}

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

    # -----------------------------------------------------------------------------
    # Build per zlib compat on/off
    # -----------------------------------------------------------------------------
    foreach ($zlibCompat in @($true, $false)) {
        if ($zlibCompat) {
            $ParamCompat = "ON"
            $LibName = "zlib1.dll"
            $DirPostfix = "compat"
        } else {
            $ParamCompat = "OFF"
            $LibName = "zlib-ng2.dll"
            $DirPostfix = "ng"
        }

        # -------------------------------------------------------------------------
        # Prepare directories
        # -------------------------------------------------------------------------
        $BuildDir = "${SrcDir}\build-${DirPostfix}-${TargetArch}"
        $DestDir = "${BaseDir}\build-${DirPostfix}-${TargetArch}"
        Remove-Item "${BuildDir}" -Recurse -ErrorAction SilentlyContinue
        Remove-Item "${DestDir}" -Recurse -ErrorAction SilentlyContinue
        New-Item "${BuildDir}" -ItemType Directory -ErrorAction SilentlyContinue
        New-Item "${DestDir}" -ItemType Directory -ErrorAction SilentlyContinue

        # -------------------------------------------------------------------------
        # Configure zlib-ng
        # -------------------------------------------------------------------------
        Write-Output ""
        Write-Host "[*] Configure zlib-ng" -ForegroundColor Yellow
        Push-Location $BuildDir
        cmake .. -G "MinGW Makefiles" "-DCMAKE_MAKE_PROGRAM=${ToolchainDir}/bin/mingw32-make" `
            "-DCMAKE_TOOLCHAIN_FILE=${BaseDir}/llvm-mingw.cmake" `
            "-DCMAKE_SYSTEM_PROCESSOR=${TargetArch}" `
            "-DLLVM_MINGW=${ToolchainDir}" `
            "-DZLIB_COMPAT=${ParamCompat}" `
            "-DWITH_GTEST=OFF"
        Pop-Location

        # -------------------------------------------------------------------------
        # Build zlib-ng
        # -------------------------------------------------------------------------
        Write-Output ""
        Write-Host "[*] Build zlib-ng" -ForegroundColor Yellow
        Push-Location $BuildDir
        cmake --build . --config Release --parallel "${Cores}"
        Pop-Location

        # -------------------------------------------------------------------------
        # Retrieve binaries
        # -------------------------------------------------------------------------
        Copy-Item "${BuildDir}\${LibName}" "${DestDir}\${LibName}"
        Copy-Item "${BuildDir}\${GzipExeName}" "${DestDir}\${GzipExeName}"
        Copy-Item "${BuildDir}\${DeflateExeName}" "${DestDir}\${DeflateExeName}"

        # -------------------------------------------------------------------------
        # Strip binaries (Just in case)
        # -------------------------------------------------------------------------
        # Strip binaries
        Push-Location $DestDir
        Write-Host "[*] Strip binaries" -ForegroundColor Yellow
        Get-ChildItem -Filter *.dll
        Get-ChildItem -Filter *.exe
        & ${StripExe} "${LibName}"
        & ${StripExe} "${GzipExeName}"
        & ${StripExe} "${DeflateExeName}"
        Get-ChildItem -Filter *.dll
        Get-ChildItem -Filter *.exe
        Pop-Location

        # Print dependency of binaries
        if ($HasRadare2) {
            Push-Location $DestDir
            Write-Host "[*] Linked libraries of [${LibName}]" -ForegroundColor Yellow
            & "${Rabin2Exe}" -Al "${LibName}"
            Write-Host "[*] Linked libraries of [${GzipExeName}]" -ForegroundColor Yellow
            & "${Rabin2Exe}" -Al "${GzipExeName}"
            Write-Host "[*] Linked libraries of [${DeflateExeName}]" -ForegroundColor Yellow
            & "${Rabin2Exe}" -Al "${DeflateExeName}"
            Pop-Location
        }
        else {
            Write-Host "Install radare2 or pass radare2 directory to check dependencies." -ForegroundColor Yellow
        }
    }
}

