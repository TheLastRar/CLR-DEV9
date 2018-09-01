#!/bin/bash

RemovePackage() 
{ 
	if [ "$1" != "" ]; then
		rm $1
	fi
}

RemoveAllPackage() 
{ 
	for var in "$@"
	do
		RemovePackage "$var"
	done
}

PLUGIN_MONO="$1/mono_i386"

if [ -d $PLUGIN_MONO ]; then
	# exit 0
	rm "$PLUGIN_MONO" -r
fi

mkdir $PLUGIN_MONO

# Get Mono 32bit
cd $PLUGIN_MONO

# Mono
apt-get download libmonosgen-2.0-1:i386 mono-runtime-common:i386 libgdiplus:i386

# Find Files
LIBMONOSGEN=$(find . -maxdepth 1 -type f -name libmonosgen-2.0-1*)
LIBMGDIPLUS=$(find . -maxdepth 1 -type f -name libgdiplus*)
LIBMONOCOMMON=$(find . -maxdepth 1 -type f -name mono-runtime-common*)

if [ "$LIBMONOSGEN" = "" ]; then
	echo "Unpack Mono i386 Failed"
	RemoveAllPackage $LIBMONOSGEN $LIBMGDIPLUS $LIBMONOCOMMON
	exit 1
fi
if [ "$LIBMONOCOMMON" = "" ]; then
	echo "Unpack Mono i386 Failed"
	RemoveAllPackage $LIBMONOSGEN $LIBMGDIPLUS $LIBMONOCOMMON
	exit 1
fi
if [ "$LIBMGDIPLUS" = "" ]; then
	echo "Unpack Mono i386 Failed"
	RemoveAllPackage $LIBMONOSGEN $LIBMGDIPLUS $LIBMONOCOMMON
	exit 1
fi

# Unpack
ar x $LIBMONOCOMMON "data.tar.xz"
tar -xf "data.tar.xz"
rm "data.tar.xz"
ar x $LIBMONOSGEN "data.tar.xz"
tar -xf "data.tar.xz"
rm "data.tar.xz"
ar x $LIBMGDIPLUS "data.tar.xz"
tar -xf "data.tar.xz"
rm "data.tar.xz"

# Cleanup
RemoveAllPackage $LIBMONOSGEN $LIBMGDIPLUS $LIBMONOCOMMON

rm "etc" -r
rm "usr/share" -r
