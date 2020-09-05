cmake_minimum_required(VERSION 3.10)
project(clrdev9mono)
include_directories("/usr/include/mono-2.0/") # Mono

add_library(${PROJECT_NAME} SHARED
	obj/CLR_DEV9.o # Core lib.
	DEV9.h
	DEV9.cpp
	PSE.h
	PSE.cpp
	)

execute_process(COMMAND ./build.sh get-arch RESULT_VARIABLE CMD_ARCH)
if(CMD_ARCH MATCHES "x64")
    find_library(LIB_MONO NAMES monosgen-2.0)
else()
    add_compile_options(-m32)
    add_link_options(-m32)
    execute_process(COMMAND ./build.sh get-apt RESULT_VARIABLE CMD_APT)
    if(NOT CMD_APT MATCHES "")
        add_link_options(-Wl,-rpath,'$'ORIGIN/mono_i386/usr/lib)
        find_library(LIB_MONO NAMES monosgen-2.0 PATHS
            "${CMAKE_CURRENT_SOURCE_DIR}/mono_i386/usr/lib" NO_DEFAULT_PATH)
    else()
        find_library(LIB_MONO NAMES monosgen-2.0)
    endif()
endif()

target_link_libraries(${PROJECT_NAME}
			${LIB_MONO}
			m
			rt
			dl
			pthread
			)
