cmake_minimum_required(VERSION 3.10)
project(clrdev9)

add_library(${PROJECT_NAME} SHARED
	coreclrhost.h
	DEV9.h
	DEV9.cpp
	PSE.h
	PSE.cpp
	)

set_target_properties(${PROJECT_NAME} PROPERTIES COMPILE_FLAGS "-m32" LINK_FLAGS "-m32")
target_link_libraries(${PROJECT_NAME}
			m
			rt
			dl
			pthread
			)
