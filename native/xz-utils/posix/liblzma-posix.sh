#!/bin/bash
# Compile liblzma on Linux/macOS

# Usage:
#   ./liblzma-posix.sh ~/build/native/xz-5.4.3

function print_help() {
    echo "Usage: $0 [-a armhf|aarch64] [-T TARGET_TRIPLE] <SRC_DIR>" >&2
    echo "" >&2
    echo "-a: Specify architecture for cross-compiling (Linux only, Optional)" >&2
    echo "-T: Specify target triple for cross-compiling (Optional)" >&2
}

# Check script arguments
CROSS_ARCH=""
CROSS_TRIPLE=""
while getopts "a:T:h" opt; do
    case $opt in
        a) # pre-defined Architecture for cross-compile
            CROSS_ARCH=$OPTARG
            ;;
        T) # any Target Triple for cross-compile
            CROSS_TRIPLE=$OPTARG
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
# Parse <FILE_SRC_DIR>
shift $(( OPTIND - 1 ))
SRC_DIR="$@"
if ! [[ -d "${SRC_DIR}" ]]; then
    print_help
    echo "Source [${SRC_DIR}] is not a directory!" >&2
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

# Set target triple (for Linux)
TARGET_TRIPLE=""
if [ "${CROSS_ARCH}" = i686 ]; then
    TARGET_TRIPLE="i686-linux-gnu"
elif [ "${CROSS_ARCH}" = x86_64 ]; then
    TARGET_TRIPLE="x86_64-linux-gnu"
elif [ "${CROSS_ARCH}" = armhf ]; then
    TARGET_TRIPLE="arm-linux-gnueabihf"
elif [ "${CROSS_ARCH}" = aarch64 ]; then
    TARGET_TRIPLE="aarch64-linux-gnu"
elif [ "${CROSS_ARCH}" != "" ]; then
    echo "[${ARCH}] is not a pre-defined architecture" >&2
    exit 1
fi

if [ "${CROSS_ARCH}" != "" ]; then
    DEST_DIR="${DEST_DIR}-${CROSS_ARCH}"
elif [ "${CROSS_TRIPLE}" != "" ]; then
    TARGET_TRIPLE="${CROSS_TRIPLE}"
    DEST_DIR="${DEST_DIR}-${CROSS_TRIPLE}"
fi

if [ "${TARGET_TRIPLE}" != "" ]; then
    echo "(Cross compile) Target triple set to [${TARGET_TRIPLE}]"
fi 

# Create dest directory
mkdir -p "${DEST_DIR}"

# Compile liblzma, xz
BUILD_MODES=( "exe" "lib" )
pushd "${SRC_DIR}" > /dev/null
for BUILD_MODE in "${BUILD_MODES[@]}"; do
    CONFIGURE_ARGS=""
    if [ "$BUILD_MODE" = "lib" ]; then
        CONFIGURE_ARGS="${CONFIGURE_ARGS} --enable-shared --disable-xz"
    elif [ "$BUILD_MODE" = "exe" ]; then
        CONFIGURE_ARGS="${CONFIGURE_ARGS} --disable-shared CFLAGS=-Os"
    fi

    if [ "${TARGET_TRIPLE}" != "" ]; then
        CONFIGURE_ARGS="${CONFIGURE_ARGS} --host=${TARGET_TRIPLE}"
    fi 
    
    make clean
    ./configure \
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

