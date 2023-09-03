#!/bin/bash
# Compile pigz for Windows on MSYS2

# Usage:
#   ./pigz-msys2.sh -a i686 /d/build/native/pigz-2.8
#   ./pigz-msys2.sh -a x86_64 /d/build/native/pigz-2.8
#   ./pigz-msys2.sh -a aarch64 -t /c/llvm-mingw /d/build/native/pigz-2.8

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
    echo "Src [${SRCDIR}] is not a directory!" >&2
    exit 1
fi

# Set path and command vars
# BASE_ABS_PATH: Absolute path of this script, e.g. /home/user/bin/foo.sh
# BASE_DIR: Absolute path of the parent dir of this script, e.g. /home/user/bin
BASE_ABS_PATH=$(readlink -f "$0")
BASE_DIR=$(dirname "${BASE_ABS_PATH}")
DEST_DIR=${BASE_DIR}/build-${ARCH}
CORES=$(grep -c ^processor /proc/cpuinfo)
MAKEFILE_MOD=Makefile.mod

# Set library paths
DEST_EXE="pigz.exe"
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
mkdir -p "${DEST_DIR}"

# Let custom toolchain is called first in PATH
if ! [[ -z "${TOOLCHAIN_DIR}" ]]; then
    export PATH=${TOOLCHAIN_DIR}/bin:${PATH}
fi

# Patch pigz Makefile
# - Change compile target architecture
# - Link static libpthread and zlib instead of dynamic
# - Specify previously built zlib headers and libz.a path
pushd "${SRCDIR}" > /dev/null
cp Makefile $MAKEFILE_MOD
sed -i "s/CC=gcc/CC=${TARGET_TRIPLE}-gcc/g" $MAKEFILE_MOD
sed -i "s/LIBS=\\-lm \\-lpthread \\-lz/LIBS=\\-lm \\-l:libpthread.a \\-l:libz.a/g" $MAKEFILE_MOD
sed -i "s,LDFLAGS=,LDFLAGS=\\-L${DEST_DIR},g" $MAKEFILE_MOD
sed -i "s,CFLAGS=,CFLAGS=\\-I${DEST_DIR}/include ,g" $MAKEFILE_MOD
popd > /dev/null

# Compile pigz
pushd "${SRCDIR}" > /dev/null
make -f $MAKEFILE_MOD clean
make -f $MAKEFILE_MOD "-j${CORES}"
cp "${DEST_EXE}" "${DEST_DIR}"
popd > /dev/null

# Strip binaries
pushd "${DEST_DIR}" > /dev/null
ls -lh *.exe
${STRIP} "${DEST_EXE}"
ls -lh *.exe
popd > /dev/null

# Print dependency of binraies
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} "${DEST_EXE}"
popd > /dev/null
