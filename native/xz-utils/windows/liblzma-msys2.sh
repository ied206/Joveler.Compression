#!/bin/bash
# Compile liblzma for Windows on MSYS2

# Usage:
#   ./liblzma-msys2.sh -a i686 /d/build/native/xz-5.4.0
#   ./liblzma-msys2.sh -a x86_64 /d/build/native/xz-5.4.0
#   ./liblzma-msys2.sh -a aarch64 -t /c/llvm-mingw /d/build/native/xz-5.4.0

# Check script arguments
while getopts "a:t:" opt; do
  case $opt in
    a) # architecture
      ARCH=$OPTARG
      ;;
    t) # toolchain, required for aarch64
      TOOLCHAIN_DIR=$OPTARG
      ;;
    :)
      echo "Usage: $0 <-a i686|x86_64|aarch64> [-t TOOLCHAIN_DIR] <FILE_SRCDIR>" >&2
      exit 1
      ;;
  esac
done
# Parse <FILE_SRCDIR>
shift $(( OPTIND - 1 ))
SRCDIR="$@"
if ! [[ -d "${SRCDIR}" ]]; then
    echo "[${SRCDIR}] is not a directory!" >&2
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
DEST_LIB="liblzma.dll"
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
    HOST_ARCH=$(uname -m)
    echo "[${ARCH}] is not a supported architecture, Ex) use '-a ${HOST_ARCH}'" >&2
    exit 1
fi

# Create dest directory
rm -rf "${DEST_DIR}"
mkdir -p "${DEST_DIR}"

# Let custom toolchain is called first in PATH
if ! [[ -z "${TOOLCHAIN_DIR}" ]]; then
    export PATH=${TOOLCHAIN_DIR}/bin:${PATH}
fi

# Compile liblzma, xz
BUILD_MODES=( "lib" "exe" )
pushd "${SRCDIR}" > /dev/null
for BUILD_MODE in "${BUILD_MODES[@]}"; do
    CONFIGURE_ARGS=""
    if [ "$BUILD_MODE" = "lib" ]; then
        CONFIGURE_ARGS="--enable-shared --disable-xz"
    elif [ "$BUILD_MODE" = "exe" ]; then
        CONFIGURE_ARGS="--disable-shared CFLAGS=-Os"
    fi
    
    make clean
    ./configure --host=${TARGET_TRIPLE} \
        --disable-debug \
        --disable-dependency-tracking \
        --disable-nls \
        --disable-scripts \
        --disable-xzdec \
        --disable-lzmadec \
        --disable-lzmainfo \
        --disable-lzma-links \
        ${CONFIGURE_ARGS}
    make "-j${CORES}"

    if [ "$BUILD_MODE" = "lib" ]; then
        cp "src/liblzma/.libs/liblzma-5.dll" "${DEST_DIR}/${DEST_LIB}"
    elif [ "$BUILD_MODE" = "exe" ]; then
        cp "src/xz/xz.exe" "${DEST_DIR}/${DEST_EXE}"
    fi    
done 
popd > /dev/null

# Strip binaries
pushd "${DEST_DIR}" > /dev/null
ls -lh *.dll *.exe
${STRIP} "${DEST_LIB}" "${DEST_EXE}"
ls -lh *.dll *.exe
popd > /dev/null

# Print dependency of binraies
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} "${DEST_LIB}"
popd > /dev/null
