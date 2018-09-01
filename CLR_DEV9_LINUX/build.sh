#!/bin/bash

buildType=Debug
if [ $# -gt 0 ] && [ $1 == "-release" ]; then
    buildType=Release
else
    buildType=Debug
fi
buildArch=x86

if [ ! -d "obj" ]; then
    mkdir obj
fi
cd obj


if [ ! -d $buildArch ]; then
    mkdir $buildArch
fi
cd $buildArch

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

cp obj/$buildArch/$buildType/libclrdev9.so bin/$buildArch/$buildType/libclrdev9.so
