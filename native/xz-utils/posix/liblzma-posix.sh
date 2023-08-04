#!/bin/bash
# Compile liblzma on Linux/macOS

# Usage:
#   ./liblzma-posix.sh ~/build/native/xz-5.4.3

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
    DEST_LIB="liblzma.so"
    DEST_EXE="xz"
    STRIP="strip"
    CHECKDEP="ldd"
elif [ "${OS}" = Darwin ]; then
    BASE_ABS_PATH="$(cd $(dirname "$0");pwd)/$(basename "$0")"
    CORES=$(sysctl -n hw.logicalcpu)
    DEST_LIB="liblzma.dylib"
    DEST_EXE="xz"
    STRIP="strip -x"
    CHECKDEP="otool -L"
else
    echo "[${OS}] is not a supported platform!" >&2
    exit 1
fi
BASE_DIR=$(dirname "${BASE_ABS_PATH}")
DEST_DIR="${BASE_DIR}/build"

# Create dest directory
mkdir -p "${DEST_DIR}"

# Compile liblzma, xz
BUILD_MODES=( "exe" "lib" )
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
        cp "src/liblzma/.libs/${DEST_LIB}" "${DEST_DIR}/${DEST_LIB}"
    elif [ "$BUILD_MODE" = "exe" ]; then
        cp "src/xz/${DEST_EXE}" "${DEST_DIR}/${DEST_EXE}"
    fi    
done 
popd > /dev/null

# Strip a binary
pushd "${DEST_DIR}" > /dev/null
ls -lh "${DEST_LIB}" "${DEST_EXE}"
${STRIP} "${DEST_LIB}" "${DEST_EXE}"
ls -lh "${DEST_LIB}" "${DEST_EXE}"
popd > /dev/null

# Check dependency of a binary
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} "${DEST_LIB}" "${DEST_EXE}"
popd > /dev/null

