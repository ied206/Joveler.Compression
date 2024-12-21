#!/bin/bash
# Compile zlib on Linux/macOS

# Usage:
#   [*] Native Build
#   ./zlib-ng-posix.sh ~/build/native/zlib-ng-2.2.2
#   [*] Cross Build (on arm64, requires a necessary toolchain to be installed)
#   ./zlib-ng-posix.sh -a armhf ~/build/native/zlib-ng-2.2.2

function print_help() {
    echo "Usage: $0 [-a armhf|aarch64] <FILE_SRCDIR>" >&2
    echo "" >&2
    echo "-a: Specify architecture for cross-compiling (Optional)" >&2
}

# Check script arguments
CROSS_ARCH=""
while getopts "a:t:h" opt; do
    case $opt in
        a) # architecture for cross-compile
            CROSS_ARCH=$OPTARG
            ;;
        h)
            print_help
            exit 1
            ;;
        :)
            print_help
            exit 1
            ;;
    esac
done
# Parse <FILE_SRCDIR>
shift $(( OPTIND - 1 ))
SRC_DIR="$@"
if ! [[ -d "${SRC_DIR}" ]]; then
    print_help
    echo "Source [${SRC_DIR}] is not a directory!" >&2
    exit 1
fi
BUILD_DIR="${SRC_DIR}/build"

# Required dependencies: cmake
# Debian/Ubuntu: sudo apt-get install cmake
which cmake > /dev/null
if [[ $? -ne 0 ]]; then
    echo "Please install cmake!" >&2
    echo "Run \"sudo apt-get install cmake\"." >&2
    exit 1
fi

# Query environment info
OS=$(uname -s) # Linux, Darwin, MINGW64_NT-10.0-19042, MSYS_NT-10.0-18363, ...

# Set path and command vars
# BASE_ABS_PATH: Absolute path of this script, e.g. /home/user/bin/foo.sh
# BASE_DIR: Absolute path of the parent dir of this script, e.g. /home/user/bin
if [ "${OS}" = Linux ]; then
    BASE_ABS_PATH=$(readlink -f "$0")
    CORES=$(grep -c ^processor /proc/cpuinfo)
    DEST_EXT="so"
    STRIP="strip"
    CHECKDEP="ldd"
elif [ "${OS}" = Darwin ]; then
    BASE_ABS_PATH="$(cd $(dirname "$0");pwd)/$(basename "$0")"
    CORES=$(sysctl -n hw.logicalcpu)
    DEST_EXT="dylib"
    STRIP="strip -x"
    CHECKDEP="otool -L"
else
    echo "[${OS}] is not a supported platform!" >&2
    exit 1
fi
BASE_DIR=$(dirname "${BASE_ABS_PATH}")
DEST_DIR="${BASE_DIR}/build"

# Cross-compile
CMAKE_OPT_PARAMS=""
if [ "${CROSS_ARCH}" != "" ]; then
    echo "Setup cross-compile for [${CROSS_ARCH}]"
    CMAKE_OPT_PARAMS="${CMAKE_OPT_PARAMS} -DCMAKE_TOOLCHAIN_FILE=${SRC_DIR}/cmake/toolchain-${CROSS_ARCH}.cmake"
fi

# Create dest directory
rm -rf "${DEST_DIR}"
mkdir -p "${DEST_DIR}"

# Compile zlib-ng
BUILD_MODES=( "newapi" "compat" )
for BUILD_MODE in "${BUILD_MODES[@]}"; do
    if [ "$BUILD_MODE" = "newapi" ]; then
        ZLIB_COMPAT_VALUE="OFF"
        DEST_LIB="libz-ng.${DEST_EXT}"
    elif [ "$BUILD_MODE" = "compat" ]; then
        ZLIB_COMPAT_VALUE="ON"
        DEST_LIB="libz.${DEST_EXT}"
    fi
    
    rm -rf "${BUILD_DIR}"
    mkdir -p "${BUILD_DIR}"
    pushd "${BUILD_DIR}" > /dev/null

    cmake .. -G "Unix Makefiles" \
        "-DZLIB_COMPAT=${ZLIB_COMPAT_VALUE}" \
        "-DWITH_GTEST=OFF" \
        "${CMAKE_OPT_PARAMS}"
    cmake --build . --config Release --parallel "${CORES}"

    cp "${DEST_LIB}" "${DEST_DIR}/${DEST_LIB}"
    popd > /dev/null
done 

# Strip a binary
pushd "${DEST_DIR}" > /dev/null
pwd
ls -lh *.${DEST_EXT}
${STRIP} *.${DEST_EXT}
ls -lh *.${DEST_EXT}
popd > /dev/null

# Check dependency of a binary
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} *.${DEST_EXT}
popd > /dev/null

