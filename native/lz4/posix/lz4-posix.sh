#!/bin/bash
# Compile lz4 on Linux/macOS

# Usage:
#   ./lz4-posix.sh ~/build/native/lz4-1.9.4

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

DEST_LIB="liblz4.${DEST_EXT}"
DEST_EXE="lz4"

# Create dest directory
rm -rf "${DEST_DIR}"
mkdir -p "${DEST_DIR}"

# Compile lz4
rm -rf "${BUILD_DIR}"
mkdir -p "${BUILD_DIR}"
pushd "${BUILD_DIR}" > /dev/null

cmake ../build/cmake -G "Unix Makefiles" \
    "-DCPACK_SOURCE_ZIP=OFF" \
    "-DCPACK_SOURCE_7Z=OFF" \
    "-DCPACK_BINARY_NSIS=OFF"
cmake --build . --config Release --parallel "${CORES}"

cp "${DEST_LIB}" "${DEST_DIR}/${DEST_LIB}"
cp "${DEST_EXE}" "${DEST_DIR}/${DEST_EXE}"
popd > /dev/null

# Strip a binary
pushd "${DEST_DIR}" > /dev/null
pwd
ls -lh ${DEST_LIB} ${DEST_EXE}
${STRIP} ${DEST_LIB}
${STRIP} ${DEST_EXE}
ls -lh ${DEST_LIB} ${DEST_EXE}
popd > /dev/null

# Check dependency of a binary
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} ${DEST_LIB} ${DEST_EXE}
popd > /dev/null

