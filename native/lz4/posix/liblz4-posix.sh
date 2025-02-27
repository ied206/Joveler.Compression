#!/bin/bash
# Compile lz4 on Linux/macOS

# Usage:
#   ./lz4-posix.sh ~/build/native/lz4-1.9.4

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
if [[ "${OS}" == Linux ]]; then
    BASE_ABS_PATH=$(readlink -f "$0")
    CORES=$(grep -c ^processor /proc/cpuinfo)
    DEST_EXT="so"
    STRIP="strip"
    CHECKDEP="ldd"
elif [[ "${OS}" == Darwin ]]; then
    export MACOSX_DEPLOYMENT_TARGET=11
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

# Check cross-compile architecture (for Linux)
if [[ "${CROSS_ARCH}" == i686 ]]; then
    :
elif [[ "${CROSS_ARCH}" == x86_64 ]]; then
    :
elif [[ "${CROSS_ARCH}" == armhf ]]; then
    :
elif [[ "${CROSS_ARCH}" == aarch64 ]]; then
    :
elif [[ "${CROSS_ARCH}" != "" ]]; then
    echo "[${ARCH}] is not a pre-defined architecture" >&2
    exit 1
fi

# Cross-compile
CMAKE_OPT_PARAMS=""
if [[ "${CROSS_ARCH}" != "" ]]; then
    echo "Setup cross-compile for [${CROSS_ARCH}]"
    DEST_DIR="${DEST_DIR}-${CROSS_ARCH}"

    if [[ "${OS}" == Linux ]]; then
        CMAKE_OPT_PARAMS="${CMAKE_OPT_PARAMS} -DCMAKE_TOOLCHAIN_FILE=${BASE_DIR}/linux-gcc-cross.cmake -DCMAKE_SYSTEM_PROCESSOR=${CROSS_ARCH}"
    elif [[ "${OS}" == Darwin ]]; then
        CMAKE_OPT_PARAMS="${CMAKE_OPT_PARAMS} -DCMAKE_OSX_ARCHITECTURES=${CROSS_ARCH}"
    fi
fi

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
    "-DCPACK_BINARY_NSIS=OFF" \
    "-DCMAKE_BUILD_TYPE=MinSizeRel" \
    ${CMAKE_OPT_PARAMS}
# Benchmark: MSVC -Os build is much faster than Clang -O3 build.
# It seems CMAKE_BUILD_TYPE must be denoted in configure time, not a build time.
cmake --build . --config MinSizeRel --parallel "${CORES}"

cp "${DEST_LIB}" "${DEST_DIR}/${DEST_LIB}"
cp "${DEST_EXE}" "${DEST_DIR}/${DEST_EXE}"
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
