#!/bin/bash
# Compile zlib on Linux/macOS

# Usage:
#   ./zlib-posix.sh ~/build/native/zlib-1.2.13

# Check script arguments
if [[ "$#" -ne 1 ]]; then
    echo "Usage: $0 <FILE_SRCDIR>" >&2
    exit 1
fi
if ! [[ -d "$1" ]]; then
    echo "[$1] is not a directory!" >&2
    exit 1
fi
SRC_DIR=$1
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
        "-DWITH_GTEST=OFF"
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

