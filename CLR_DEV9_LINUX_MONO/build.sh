#!/usr/bin/env bash

# bundle mono for apt-based distros, use system Mono otherwise
if command -v apt-get >/dev/null
then
    ./BundleMono.sh ./
fi

buildType="Debug"
if [[ "${1}" == "-release" ]]
then
    buildType="Release"
fi

# autodetect 32/64-bit based on PCSX2 binary
# default to 32-bit if not in PATH
buildArch="x86"
if command -v PCSX2 >/dev/null && [[ "$(objdump -f $(command -v PCSX2) | grep "file format" | tr -s " " | cut -d " " -f 4)" == "elf64-x86-64" ]]
then
    buildArch="x64"
fi

if [[ "${1}" == "get-arch" ]] ; then echo "${buildArch}" ; exit 0 ; fi
if [[ "${1}" == "get-apt" ]] ; then command -v apt-get ; exit 0 ; fi

if [[ ! -f "../CLR_DEV9/bin/${buildType}/CLR_DEV9.dll" ]]; then
    echo "CLR_DEV9.dll Not Found!  Place the Windows build of the appropriate architecture you want in $(realpath ../CLR_DEV9/bin/${buildType})"
    exit 1
fi

cp -v ../CLR_DEV9/bin/$buildType/CLR_DEV9.dll CLR_DEV9.dll

if [[ ! -d "obj" ]]; then
    mkdir -vp obj
fi

if [[ "${buildArch}" == "x64" ]]
then
    objcopy --input binary --output elf64-x86-64 --binary-architecture i386:x86-64 CLR_DEV9.dll obj/CLR_DEV9.o
else
    objcopy --input binary --output elf32-i386 --binary-architecture i386 CLR_DEV9.dll obj/CLR_DEV9.o
fi

cd obj
if [[ ! -d "${buildArch}" ]]; then
    mkdir -vp "${buildArch}"
fi

cd "${buildArch}"
if [[ ! -d "${buildType}" ]]; then
    mkdir -vp "${buildType}"
fi

cd "${buildType}"
cmake -DCMAKE_BUILD_TYPE="${buildType}" ../../..
make VERBOSE=1

cd ../../..
if [ ! -d "bin/${buildArch}/${buildType}" ]; then
    mkdir -vp "bin/${buildArch}/${buildType}"
fi

cp "obj/${buildArch}/${buildType}/libclrdev9mono.so" "bin/${buildArch}/${buildType}/libclrdev9mono.so"

if [[ -f "bin/${buildArch}/${buildType}/libclrdev9mono.so" ]]; then
    if [[ -d "mono_i386" ]]
    then
        tar -zcvf "bin/${buildArch}/${buildType}/libclrdev9mono.${buildType}.tar.gz" "mono_i386" -C "bin/${buildArch}/${buildType}/" libclrdev9mono.so
    else
        tar -zcvf "bin/${buildArch}/${buildType}/libclrdev9mono.${buildType}.tar.gz" -C "bin/${buildArch}/${buildType}/" libclrdev9mono.so
    fi
fi
