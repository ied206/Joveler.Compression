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
SRCDIR=$1

# Query environment info
OS=$(uname -s) # Linux, Darwin, MINGW64_NT-10.0-19042, MSYS_NT-10.0-18363, ...

# Set path and command vars
# BASE_ABS_PATH: Absolute path of this script, e.g. /home/user/bin/foo.sh
# BASE_DIR: Absolute path of the parent dir of this script, e.g. /home/user/bin
if [ "${OS}" = Linux ]; then
    BASE_ABS_PATH=$(readlink -f "$0")
    CORES=$(grep -c ^processor /proc/cpuinfo)
    DEST_LIB="libz.so"
    STRIP="strip"
    CHECKDEP="ldd"
elif [ "${OS}" = Darwin ]; then
    BASE_ABS_PATH="$(cd $(dirname "$0");pwd)/$(basename "$0")"
    CORES=$(sysctl -n hw.logicalcpu)
    DEST_LIB="libz.dylib"
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

# Compile zlib
pushd "${SRCDIR}" > /dev/null
make clean
./configure
make "-j${CORES}"

cp "${DEST_LIB}" "${DEST_DIR}/${DEST_LIB}"
popd > /dev/null

# Strip a binary
pushd "${DEST_DIR}" > /dev/null
ls -lh "${DEST_LIB}"
${STRIP} "${DEST_LIB}"
ls -lh "${DEST_LIB}"
popd > /dev/null

# Check dependency of a binary
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} "${DEST_LIB}"
popd > /dev/null

