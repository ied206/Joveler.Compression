#!/bin/bash
# Compile static pigz on Linux/macOS

# Usage:
#   ./pigz-posix.sh ~/build/native/pigz-2.8

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
    STRIP="strip"
    CHECKDEP="ldd"
    SED_ARGS="-i"
    SED_ZLIB="\\-l:libz.a"
elif [ "${OS}" = Darwin ]; then
    BASE_ABS_PATH="$(cd $(dirname "$0");pwd)/$(basename "$0")"
    CORES=$(sysctl -n hw.logicalcpu)
    STRIP="strip -x"
    CHECKDEP="otool -L"
    SED_ARGS="-i .bak -e"
    SED_ZLIB="\\-lzstatic"
else
    echo "[${OS}] is not a supported platform!" >&2
    exit 1
fi
BASE_DIR=$(dirname "${BASE_ABS_PATH}")
DEST_DIR="${BASE_DIR}/build"
DEST_EXE="pigz"
MAKEFILE_MOD=Makefile.mod

# Dest directory must have been created by zlib-posix.sh
if ! [[ -d "${DEST_DIR}" ]]; then
    echo "Please run [zlib-posix.sh] first." >&2
    exit 1
fi

# Patch pigz Makefile
# - Link static zlib instead of dynamic
# - Specify previously built zlib headers and libz.a path
pushd "${SRCDIR}" > /dev/null
if [ "${OS}" = Darwin ]; then
    cp "${DEST_DIR}/libz.a" "${DEST_DIR}/libzstatic.a" 
fi
cp Makefile $MAKEFILE_MOD
sed ${SED_ARGS} "s/LIBS=\\-lm \\-lpthread \\-lz/LIBS=\\-lm \\-lpthread ${SED_ZLIB}/g" $MAKEFILE_MOD
sed ${SED_ARGS} "s,LDFLAGS=,LDFLAGS=\\-L${DEST_DIR},g" $MAKEFILE_MOD
sed ${SED_ARGS} "s,CFLAGS=,CFLAGS=\\-I${DEST_DIR}/include ,g" $MAKEFILE_MOD
popd > /dev/null

# Compile pigz
pushd "${SRCDIR}" > /dev/null
make -f $MAKEFILE_MOD clean
make -f $MAKEFILE_MOD "-j${CORES}"
cp "${DEST_EXE}" "${DEST_DIR}"
popd > /dev/null

# Strip a binary
pushd "${DEST_DIR}" > /dev/null
ls -lh "${DEST_EXE}"
${STRIP} "${DEST_EXE}"
ls -lh "${DEST_EXE}"
popd > /dev/null

# Check dependency of a binary
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} "${DEST_EXE}"
popd > /dev/null

