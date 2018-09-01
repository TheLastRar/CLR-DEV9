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
target_link_libraries(${PROJECT_NAME}
			monosgen-2.0
			m
			rt
			dl
			pthread
			)
