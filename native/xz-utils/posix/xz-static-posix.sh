#!/bin/bash
# Compile liblzma on Linux/macOS

# Usage:
#   ./xz-static-posix.sh ~/build/native/xz-5.4.3

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
    DEST_EXE="xz"
    STRIP="strip"
    CHECKDEP="ldd"
elif [ "${OS}" = Darwin ]; then
    BASE_ABS_PATH="$(cd $(dirname "$0");pwd)/$(basename "$0")"
    CORES=$(sysctl -n hw.logicalcpu)
    DEST_EXE="xz"
    STRIP="strip -x"
    CHECKDEP="otool -L"
else
    echo "${OS} is not a supported platform!" >&2
    exit 1
fi
BASE_DIR=$(dirname "${BASE_ABS_PATH}")
DEST_DIR="${BASE_DIR}/build"

# Create dest directory
rm -rf "${DEST_DIR}"
mkdir -p "${DEST_DIR}"

# Compile liblzma
pushd "${SRCDIR}" > /dev/null
make clean
/configure \
    --disable-debug \
    --disable-shared \
    --disable-dependency-tracking \
    --disable-nls \
    --disable-scripts
make "-j${CORES}"
cp "src/xz/${DEST_EXE}" "${DEST_DIR}/${DEST_EXE}"
popd > /dev/null

# Strip a binary
pushd "${DEST_DIR}" > /dev/null
ls -lh "${DEST_EXE}"
${STRIP} "${DEST_EXE}"
ls -lh  "${DEST_EXE}"
popd > /dev/null

# Check dependency of a binary
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} "${DEST_EXE}"
popd > /dev/null

