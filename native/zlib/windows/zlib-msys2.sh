#!/bin/bash
# Compile zlib for Windows on MSYS2

# Usage:
#   ./zlib-msys2.sh -a i686 /d/build/native/zlib-1.3
#   ./zlib-msys2.sh -a x86_64 /d/build/native/zlib-1.3
#   ./zlib-msys2.sh -a aarch64 -t /c/llvm-mingw /d/build/native/zlib-1.3

function print_help() {
    echo "Usage: $0 <-a i686|x86_64|aarch64> [-t TOOLCHAIN_DIR] <FILE_SRCDIR>" >&2
}

# Check script arguments
while getopts "a:t:h" opt; do
    case $opt in
        a) # architecture
            ARCH=$OPTARG
            ;;
        t) # toolchain, required for aarch64
            TOOLCHAIN_DIR=$OPTARG
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
SRCDIR="$@"
if ! [[ -d "${SRCDIR}" ]]; then
    print_help
    echo "Source [${SRCDIR}] is not a directory!" >&2
    exit 1
fi

# Set path and command vars
# BASE_ABS_PATH: Absolute path of this script, e.g. /home/user/bin/foo.sh
# BASE_DIR: Absolute path of the parent dir of this script, e.g. /home/user/bin
BASE_ABS_PATH=$(readlink -f "$0")
BASE_DIR=$(dirname "${BASE_ABS_PATH}")
DEST_DIR=${BASE_DIR}/build-${ARCH}
CORES=$(grep -c ^processor /proc/cpuinfo)

# Set library paths
DEST_STATIC_LIB="libz.a"
DEST_DYNAMIC_LIB="zlib1.dll"
DEST_INCL_ZLIBH="zlib.h"
DEST_INCL_ZCONFH="zconf.h"
STRIP="strip"
CHECKDEP="ldd"

# Set target triple
if [ "${ARCH}" = i686 ]; then
    TARGET_TRIPLE="i686-w64-mingw32"
elif [ "${ARCH}" = x86_64 ]; then
    TARGET_TRIPLE="x86_64-w64-mingw32"
elif [ "${ARCH}" = aarch64 ]; then
    TARGET_TRIPLE="aarch64-w64-mingw32"
    # Let custom toolchain is called first in PATH
    if [[ -z "${TOOLCHAIN_DIR}" ]]; then
        echo "Please provide llvm-mingw as [TOOLCHAIN_DIR] for aarch64 build." >&2
        exit 1
    fi
else
    print_help
    HOST_ARCH=$(uname -m)
    echo "[${ARCH}] is not a supported architecture, Ex) use '-a ${HOST_ARCH}'" >&2
    exit 1
fi

# Create dest directory
rm -rf "${DEST_DIR}"
mkdir -p "${DEST_DIR}"
mkdir -p "${DEST_DIR}/include"

# Let custom toolchain is called first in PATH
if ! [[ -z "${TOOLCHAIN_DIR}" ]]; then
    export PATH=${TOOLCHAIN_DIR}/bin:${PATH}
fi

# Compile zlib
pushd "${SRCDIR}" > /dev/null
make -f "win32/Makefile.gcc" clean
make -f "win32/Makefile.gcc" "-j${CORES}" "PREFIX=${TARGET_TRIPLE}-"
cp "${DEST_STATIC_LIB}" "${DEST_DIR}"
cp "${DEST_DYNAMIC_LIB}" "${DEST_DIR}"
cp "${DEST_INCL_ZLIBH}" "${DEST_DIR}/include"
cp "${DEST_INCL_ZCONFH}" "${DEST_DIR}/include"
popd > /dev/null

# Strip binaries
pushd "${DEST_DIR}" > /dev/null
ls -lh *.dll
${STRIP} "${DEST_DYNAMIC_LIB}"
ls -lh *.dll
popd > /dev/null

# Print dependency of binraies
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} "${DEST_DYNAMIC_LIB}"
popd > /dev/null
