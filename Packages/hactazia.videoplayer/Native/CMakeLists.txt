cmake_minimum_required(VERSION 3.16)
project(hactazia_videoplayer VERSION 1.0.0)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Determine platform and architecture
if(WIN32)
    set(UNITY_OS "Windows")
    if(CMAKE_GENERATOR_PLATFORM STREQUAL "x64" OR CMAKE_SIZEOF_VOID_P EQUAL 8)
        set(UNITY_ARCH "x86_64")
    else()
        set(UNITY_ARCH "x86")
    endif()
elseif(APPLE)
    set(UNITY_OS "macOS")
    if(CMAKE_OSX_ARCHITECTURES MATCHES "arm64")
        set(UNITY_ARCH "arm64")
    else()
        set(UNITY_ARCH "x86_64")
    endif()
else()
    set(UNITY_OS "Linux")
    if(CMAKE_SIZEOF_VOID_P EQUAL 8)
        set(UNITY_ARCH "x86_64")
    else()
        set(UNITY_ARCH "x86")
    endif()
endif()

# Set output directory directly to Unity Plugins structure
set(UNITY_PLUGINS_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../Plugins/${UNITY_OS}/${UNITY_ARCH}")

# Set output directories for all configurations to avoid Debug/Release subdirectories
foreach(CONFIG ${CMAKE_CONFIGURATION_TYPES})
    string(TOUPPER ${CONFIG} CONFIG_UPPER)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${CONFIG_UPPER} ${UNITY_PLUGINS_DIR})
    set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${CONFIG_UPPER} ${UNITY_PLUGINS_DIR})
    set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_${CONFIG_UPPER} ${UNITY_PLUGINS_DIR})
endforeach()

# Also set the base directories (for single-config generators)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${UNITY_PLUGINS_DIR})
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${UNITY_PLUGINS_DIR})
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${UNITY_PLUGINS_DIR})

# Platform-specific settings
if(WIN32)
    set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
endif()

# Find FFmpeg using vcpkg or pkg-config
if(WIN32 AND DEFINED CMAKE_TOOLCHAIN_FILE AND CMAKE_TOOLCHAIN_FILE MATCHES "vcpkg")
    message(STATUS "Using vcpkg to find FFmpeg")
    
    # Find FFmpeg components via vcpkg
    find_package(PkgConfig QUIET)
    
    # Try to find FFmpeg via pkg-config first (vcpkg provides .pc files)
    if(PkgConfig_FOUND)
        pkg_check_modules(LIBAV QUIET IMPORTED_TARGET
            libavcodec
            libavformat
            libavutil
            libswscale
            libswresample
        )
    endif()
    
    # Fallback to manual search if pkg-config fails
    if(NOT LIBAV_FOUND)
        find_path(AVCODEC_INCLUDE_DIR libavcodec/avcodec.h)
        find_library(AVCODEC_LIBRARY avcodec)
        find_path(AVFORMAT_INCLUDE_DIR libavformat/avformat.h)
        find_library(AVFORMAT_LIBRARY avformat)
        find_path(AVUTIL_INCLUDE_DIR libavutil/avutil.h)
        find_library(AVUTIL_LIBRARY avutil)
        find_path(SWSCALE_INCLUDE_DIR libswscale/swscale.h)
        find_library(SWSCALE_LIBRARY swscale)
        find_path(SWRESAMPLE_INCLUDE_DIR libswresample/swresample.h)
        find_library(SWRESAMPLE_LIBRARY swresample)
        
        if(AVCODEC_LIBRARY AND AVFORMAT_LIBRARY AND AVUTIL_LIBRARY AND SWSCALE_LIBRARY AND SWRESAMPLE_LIBRARY)
            set(FFMPEG_FOUND TRUE)
        endif()
    endif()
    
else()
    # Use pkg-config for non-Windows platforms or non-vcpkg builds
    find_package(PkgConfig REQUIRED)
    pkg_check_modules(LIBAV REQUIRED IMPORTED_TARGET
        libavcodec
        libavformat
        libavutil
        libswscale
        libswresample
    )
endif()

# Source files - VideoPlayer implementation
set(SOURCES
    src/VideoPlayer.cpp
)

set(HEADERS
    include/VideoPlayer.h
)

# Create shared library
add_library(${PROJECT_NAME} SHARED ${SOURCES} ${HEADERS})

# Include directories
target_include_directories(${PROJECT_NAME} PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

# Link FFmpeg libraries
if(LIBAV_FOUND)
    message(STATUS "Using pkg-config FFmpeg libraries")
    target_link_libraries(${PROJECT_NAME} PRIVATE PkgConfig::LIBAV)
elseif(FFMPEG_FOUND)
    message(STATUS "Using manually found FFmpeg libraries")
    target_include_directories(${PROJECT_NAME} PRIVATE
        ${AVCODEC_INCLUDE_DIR}
        ${AVFORMAT_INCLUDE_DIR}
        ${AVUTIL_INCLUDE_DIR}
        ${SWSCALE_INCLUDE_DIR}
        ${SWRESAMPLE_INCLUDE_DIR}
    )
    
    target_link_libraries(${PROJECT_NAME} PRIVATE
        ${AVCODEC_LIBRARY}
        ${AVFORMAT_LIBRARY}
        ${AVUTIL_LIBRARY}
        ${SWSCALE_LIBRARY}
        ${SWRESAMPLE_LIBRARY}
    )
else()
    message(FATAL_ERROR "FFmpeg libraries not found!")
endif()

# Platform-specific linking
if(WIN32)
    target_link_libraries(${PROJECT_NAME} PRIVATE ws2_32 winmm secur32 bcrypt)
elseif(APPLE)
    find_library(CORE_FOUNDATION CoreFoundation)
    find_library(VIDEO_DECODE_ACCELERATION VideoDecodeAcceleration)
    find_library(CORE_MEDIA CoreMedia)
    find_library(CORE_VIDEO CoreVideo)
    target_link_libraries(${PROJECT_NAME} PRIVATE 
        ${CORE_FOUNDATION} 
        ${VIDEO_DECODE_ACCELERATION}
        ${CORE_MEDIA}
        ${CORE_VIDEO}
    )
elseif(UNIX)
    target_link_libraries(${PROJECT_NAME} PRIVATE pthread dl m)
endif()

# Compiler-specific options
if(MSVC)
    target_compile_definitions(${PROJECT_NAME} PRIVATE
        _CRT_SECURE_NO_WARNINGS
        WIN32_LEAN_AND_MEAN
        VIDEOPLAYER_EXPORTS
    )
    target_compile_options(${PROJECT_NAME} PRIVATE /W3)
else()
    target_compile_options(${PROJECT_NAME} PRIVATE -Wall -Wextra -O2)
    target_compile_definitions(${PROJECT_NAME} PRIVATE VIDEOPLAYER_EXPORTS)
endif()

# Set output name
set_target_properties(${PROJECT_NAME} PROPERTIES
    OUTPUT_NAME "hactazia_videoplayer"
    PREFIX ""
)

# Install rules
install(TARGETS ${PROJECT_NAME}
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
)

install(FILES ${HEADERS}
    DESTINATION include
)

# Ensure Unity Plugins directory exists
add_custom_command(TARGET ${PROJECT_NAME} PRE_BUILD
    COMMAND ${CMAKE_COMMAND} -E make_directory ${UNITY_PLUGINS_DIR}
)

# Copy FFmpeg dependencies if on Windows
if(WIN32 AND DEFINED CMAKE_TOOLCHAIN_FILE AND CMAKE_TOOLCHAIN_FILE MATCHES "vcpkg")
    # Get vcpkg installed directory
    get_filename_component(VCPKG_ROOT "${CMAKE_TOOLCHAIN_FILE}" DIRECTORY)
    get_filename_component(VCPKG_ROOT "${VCPKG_ROOT}" DIRECTORY)
    get_filename_component(VCPKG_ROOT "${VCPKG_ROOT}" DIRECTORY)
    
    set(VCPKG_BIN_DIR "${VCPKG_ROOT}/installed/${VCPKG_TARGET_TRIPLET}/bin")
    
    # Copy FFmpeg DLLs if they exist
    add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${VCPKG_BIN_DIR}/avcodec-61.dll"
            "${VCPKG_BIN_DIR}/avformat-61.dll" 
            "${VCPKG_BIN_DIR}/avutil-59.dll"
            "${VCPKG_BIN_DIR}/swscale-8.dll"
            "${VCPKG_BIN_DIR}/swresample-5.dll"
            ${UNITY_PLUGINS_DIR}
        COMMENT "Copying FFmpeg DLLs to Unity Plugins directory"
    )
endif()

# Debug configuration
if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    target_compile_definitions(${PROJECT_NAME} PRIVATE DEBUG _DEBUG)
    if(MSVC)
        target_compile_options(${PROJECT_NAME} PRIVATE /Od /Zi)
        target_link_options(${PROJECT_NAME} PRIVATE /DEBUG)
    else()
        target_compile_options(${PROJECT_NAME} PRIVATE -g -O0)
    endif()
endif()

# Print configuration
message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "C++ standard: ${CMAKE_CXX_STANDARD}")
message(STATUS "Unity Platform: ${UNITY_OS}/${UNITY_ARCH}")
message(STATUS "Output directory: ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}")

if(AVCODEC_LIBRARY)
    message(STATUS "FFmpeg libraries found:")
    message(STATUS "  AVCODEC: ${AVCODEC_LIBRARY}")
    message(STATUS "  AVFORMAT: ${AVFORMAT_LIBRARY}")
    message(STATUS "  AVUTIL: ${AVUTIL_LIBRARY}")
    message(STATUS "  SWSCALE: ${SWSCALE_LIBRARY}")
    message(STATUS "  SWRESAMPLE: ${SWRESAMPLE_LIBRARY}")
else()
    message(STATUS "Using pkg-config for FFmpeg libraries")
endif()
