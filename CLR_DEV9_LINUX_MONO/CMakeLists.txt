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

set_target_properties(${PROJECT_NAME} PROPERTIES COMPILE_FLAGS "-m32" LINK_FLAGS "-m32 -Wl,-rpath,'$'ORIGIN/mono_i386/usr/lib")

find_library(LIB_MONO NAMES monosgen-2.0 PATHS "${CMAKE_CURRENT_SOURCE_DIR}/mono_i386/usr/lib" NO_DEFAULT_PATH)

target_link_libraries(${PROJECT_NAME}
			${LIB_MONO}
			m
			rt
			dl
			pthread
			)
