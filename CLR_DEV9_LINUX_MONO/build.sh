#!/bin/bash

./BundleMono.sh ./

buildType=Debug
if [ $# -gt 0 ] && [ $1 == "-release" ]; then
    buildType=Release
else
    buildType=Debug
fi
buildArch=x86


if [ ! -f "../CLR_DEV9/bin/$buildType/CLR_DEV9.dll" ]; then
    echo CLR_DEV9.dll Not Found
    exit 1
fi

cp ../CLR_DEV9/bin/$buildType/CLR_DEV9.dll CLR_DEV9.dll

if [ ! -d "obj" ]; then
    mkdir obj
fi
cd obj

objcopy --input binary --output elf32-i386 --binary-architecture i386 ../CLR_DEV9.dll CLR_DEV9.o

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

cp obj/$buildArch/$buildType/libclrdev9mono.so bin/$buildArch/$buildType/libclrdev9mono.so

if [ -f bin/$buildArch/$buildType/libclrdev9mono.so ]; then
    tar -zcvf bin/$buildArch/$buildType/libclrdev9mono.$buildType.tar.gz mono_i386 -C bin/$buildArch/$buildType/ libclrdev9mono.so
fi
