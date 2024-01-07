#!/bin/bash
# Compile zlib on Linux/macOS

# Usage:
#   ./zlib-posix.sh ~/build/native/zlib-1.3

# Check script arguments
if [[ "$#" -ne 1 ]]; then
    echo "Usage: $0 <FILE_SRCDIR>" >&2
    exit 1
fi
if ! [[ -d "$1" ]]; then
    echo "[$1] is not a directory!" >&2
    exit 1
fi
SRCDIR=$1

# Query environment info
OS=$(uname -s) # Linux, Darwin, MINGW64_NT-10.0-19042, MSYS_NT-10.0-18363, ...

# Set path and command vars
# BASE_ABS_PATH: Absolute path of this script, e.g. /home/user/bin/foo.sh
# BASE_DIR: Absolute path of the parent dir of this script, e.g. /home/user/bin
if [ "${OS}" = Linux ]; then
    BASE_ABS_PATH=$(readlink -f "$0")
    CORES=$(grep -c ^processor /proc/cpuinfo)
    DEST_DYNAMIC_LIB="libz.so"
    STRIP="strip"
    CHECKDEP="ldd"
elif [ "${OS}" = Darwin ]; then
    BASE_ABS_PATH="$(cd $(dirname "$0");pwd)/$(basename "$0")"
    CORES=$(sysctl -n hw.logicalcpu)
    DEST_DYNAMIC_LIB="libz.dylib"
    STRIP="strip -x"
    CHECKDEP="otool -L"
else
    echo "[${OS}] is not a supported platform!" >&2
    exit 1
fi
BASE_DIR=$(dirname "${BASE_ABS_PATH}")
DEST_DIR="${BASE_DIR}/build"
DEST_STATIC_LIB="libz.a"
DEST_INCL_ZLIBH="zlib.h"
DEST_INCL_ZCONFH="zconf.h"

# Create dest directory
rm -rf "${DEST_DIR}"
mkdir -p "${DEST_DIR}"
mkdir -p "${DEST_DIR}/include"

# Compile zlib
BUILD_MODES=( "static" "dynamic" )
pushd "${SRCDIR}" > /dev/null
for BUILD_MODE in "${BUILD_MODES[@]}"; do
    CONFIGURE_ARGS=""
    if [ "$BUILD_MODE" = "static" ]; then
        CONFIGURE_ARGS="--static"
    elif [ "$BUILD_MODE" = "dynamic" ]; then
        CONFIGURE_ARGS=""
    fi

    make clean
    ./configure
    make "-j${CORES}"

    if [ "$BUILD_MODE" = "static" ]; then
        cp "${DEST_STATIC_LIB}" "${DEST_DIR}"
        cp "${DEST_INCL_ZLIBH}" "${DEST_DIR}/include"
        cp "${DEST_INCL_ZCONFH}" "${DEST_DIR}/include"
    elif [ "$BUILD_MODE" = "dynamic" ]; then
        cp "${DEST_DYNAMIC_LIB}" "${DEST_DIR}"
    fi    
done 
popd > /dev/null

# Strip a binary
pushd "${DEST_DIR}" > /dev/null
ls -lh "${DEST_DYNAMIC_LIB}"
${STRIP} "${DEST_DYNAMIC_LIB}"
ls -lh "${DEST_DYNAMIC_LIB}"
popd > /dev/null

# Check dependency of a binary
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} "${DEST_DYNAMIC_LIB}"
popd > /dev/null

