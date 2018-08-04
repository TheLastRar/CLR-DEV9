#!/bin/bash

if [ ! -d "bin" ]; then
    mkdir bin
fi
cd bin

buildType=Debug

if [ $# -gt 0 ] && [ $1 == "-release" ]; then
    if [ ! -d "Release" ]; then
        mkdir Release
    fi
    buildType=Release
    cd Release
else
    if [ ! -d "Debug" ]; then
        mkdir Debug
    fi
    cd Debug
    buildType=Debug
fi

cmake -DCMAKE_BUILD_TYPE=$buildType ../..
make
