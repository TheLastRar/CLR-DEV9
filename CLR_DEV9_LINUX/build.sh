#!/bin/bash

if [ ! -d "obj" ]; then
    mkdir obj
fi
cd obj

buildArch=x86

if [ ! -d $buildArch ]; then
    mkdir $buildArch
fi
cd $buildArch

buildType=Debug

if [ $# -gt 0 ] && [ $1 == "-release" ]; then
    buildType=Release
else
    buildType=Debug
fi

if [ ! -d $buildType ]; then
    mkdir $buildType
fi
cd $buildType

cmake -DCMAKE_BUILD_TYPE=$buildType ../../..
make

cd ../../..

if [ ! -d "bin" ]; then
    mkdir bin
fi
if [ ! -d bin/$buildArch ]; then
    mkdir bin/$buildArch
fi
if [ ! -d bin/$buildArch/$buildType ]; then
    mkdir bin/$buildArch/$buildType
fi
cd $buildType

cp obj/$buildArch/$buildType/libclrdev9.so bin/$buildArch/$buildType/libclrdev9.so
