set(CMAKE_SYSTEM_NAME Linux)

set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)

if(NOT CMAKE_SYSTEM_PROCESSOR)
  message("Please set CMAKE_SYSTEM_PROCESSOR for cross compile.")
  message("- List of supported architectures: [i686, x86_64, arm64, armhf]")
  message("- Trying to build for host: [${CMAKE_HOST_SYSTEM_PROCESSOR}]")
  set(CMAKE_SYSTEM_PROCESSOR ${CMAKE_HOST_SYSTEM_PROCESSOR})
endif()
string( TOLOWER "${CMAKE_HOST_SYSTEM_PROCESSOR}" CMAKE_HOST_SYSTEM_PROCESSOR)

if(CMAKE_SYSTEM_PROCESSOR STREQUAL "x86_64" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "amd64" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "x64")
    set(_TARGET_TRIPLE x86_64-linux-gnu)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "i686" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "x86")
    set(_TARGET_TRIPLE i686-linux-gnu)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "aarch64" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "arm64")
    set(_TARGET_TRIPLE aarch64-linux-gnu)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "armhf" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "arm" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "armv7")
    set(_TARGET_TRIPLE arm-linux-gnueabihf)
endif()

find_program(CMAKE_C_COMPILER NAMES ${_TARGET_TRIPLE}-gcc)
if(NOT CMAKE_C_COMPILER)
    message(FATAL_ERROR "C toolchain for [${_TARGET_TRIPLE}] is not installed!")
endif()

# g++ support is optional
find_program(CMAKE_CXX_COMPILER NAMES ${_TARGET_TRIPLE}-g++)

set(CMAKE_TRY_COMPILE_PLATFORM_VARIABLES
  CMAKE_SYSTEM_NAME
  CMAKE_SYSTEM_PROCESSOR
)

