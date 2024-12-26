#!/bin/bash
# Compile zstd on Linux/macOS

# Usage:
#   ./libzstd-posix.sh ~/build/native/zstd-1.5.5

function print_help() {
    echo "Usage: $0 [-a armhf|aarch64] <SRC_DIR>" >&2
    echo "" >&2
    echo "-a: Specify architecture for cross-compiling (Linux only, Optional)" >&2
}

# Check script arguments
CROSS_ARCH=""
while getopts "a:h" opt; do
    case $opt in
        a) # pre-defined Architecture for cross-compile
            CROSS_ARCH=$OPTARG
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
# Parse <SRC_DIR>
shift $(( OPTIND - 1 ))
SRC_DIR="$@"
if ! [[ -d "${SRC_DIR}" ]]; then
    print_help
    echo "Source [${SRC_DIR}] is not a directory!" >&2
    exit 1
fi
BUILD_DIR="${SRC_DIR}/build-cmake"

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

DEST_LIB="libzstd.${DEST_EXT}"
DEST_EXE="zstd"

# Check cross-compile architecture (for Linux)
if [ "${CROSS_ARCH}" = i686 ]; then
    :
elif [ "${CROSS_ARCH}" = x86_64 ]; then
    :
elif [ "${CROSS_ARCH}" = armhf ]; then
    :
elif [ "${CROSS_ARCH}" = aarch64 ]; then
    :
elif [ "${CROSS_ARCH}" != "" ]; then
    echo "[${ARCH}] is not a pre-defined architecture" >&2
    exit 1
fi

if [ "${CROSS_ARCH}" != "" ]; then
    DEST_DIR="${DEST_DIR}-${CROSS_ARCH}"
fi

# Create dest directory
rm -rf "${DEST_DIR}"
mkdir -p "${DEST_DIR}"

# Compile zstd
rm -rf "${BUILD_DIR}"
mkdir -p "${BUILD_DIR}"
pushd "${BUILD_DIR}" > /dev/null

CONFIGURE_ARGS=""
if [ "${CROSS_ARCH}" != "" ]; then
    CONFIGURE_ARGS="${CONFIGURE_ARGS} -DCMAKE_TOOLCHAIN_FILE=${BASE_DIR}/linux-gcc-cross.cmake -DCMAKE_SYSTEM_PROCESSOR=${CROSS_ARCH}"
fi 

cmake ../build/cmake -G "Unix Makefiles" \
    "-DZSTD_BUILD_DYNAMIC=ON" \
    "-DZSTD_BUILD_STATIC=ON" \
    "-DZSTD_BUILD_PROGRAM=ON" \
    "-DZSTD_PROGRAMS_LINK_SHARED=OFF" \
    "-DZSTD_ZLIB_SUPPORT=OFF" \
    "-DZSTD_LZMA_SUPPORT=OFF" \
    "-DZSTD_LZ4_SUPPORT=OFF" \
    "-DZSTD_BUILD_TESTS=OFF" \
    "-DCMAKE_BUILD_TYPE=Release" \
    ${CONFIGURE_ARGS}
# TODO: --config not required because of `-DCMAKE_BUILD_TYPE`?
cmake --build . --config Release --parallel "${CORES}"

cp "lib/${DEST_LIB}" "${DEST_DIR}/${DEST_LIB}"
cp "programs/${DEST_EXE}" "${DEST_DIR}/${DEST_EXE}"
popd > /dev/null

# Strip a binary
pushd "${DEST_DIR}" > /dev/null
pwd
ls -lh "${DEST_LIB}" "${DEST_EXE}"
${STRIP} "${DEST_LIB}"
${STRIP} "${DEST_EXE}"
ls -lh "${DEST_LIB}" "${DEST_EXE}"
popd > /dev/null

# Check dependency of a binary
pushd "${DEST_DIR}" > /dev/null
file "${DEST_LIB}" "${DEST_EXE}"
${CHECKDEP} "${DEST_LIB}" "${DEST_EXE}"
popd > /dev/null
