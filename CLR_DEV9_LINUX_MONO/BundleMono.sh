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

UnpackPackage()
{
    ar x $1 "data.tar.xz"
    tar -xf "data.tar.xz"
    rm "data.tar.xz"
}

UnpackAllPackage() 
{ 
	for var in "$@"
	do
		UnpackPackage "$var"
	done
}

MissingCheck()
{
    if [ "$1" = "" ]; then
        echo "Unpack Mono i386 Failed"
        return 1
    else
        return 0
    fi
}

MissingAnyCheck()
{
    local ret=0
	for var in "$@"
	do
		MissingCheck "$var"
        ret+=$?
	done
    return $ret
}

PLUGIN_MONO="$1/mono_i386"

if [ -d $PLUGIN_MONO ]; then
	# exit 0
	rm "$PLUGIN_MONO" -r
fi

mkdir $PLUGIN_MONO

# Get Mono 32bit
cd $PLUGIN_MONO

# Mono Native Files
apt-get download libmonosgen-2.0-1:i386 libmonosgen-2.0-dev:i386 mono-runtime-common:i386 libgdiplus:i386
# Find Files
LIBMONOSGEN=$(find . -maxdepth 1 -type f -name libmonosgen-2.0-1*)
LIBMONODEV=$(find . -maxdepth 1 -type f -name libmonosgen-2.0-dev*)
LIBMGDIPLUS=$(find . -maxdepth 1 -type f -name libgdiplus*)
LIBMONOCOMMON=$(find . -maxdepth 1 -type f -name mono-runtime-common*)

# Mono Managed Files Needed By Plugin
# Currently Using on .Net 4.5
# System (mscorlib)
# System.Core
# System.Drawing
# System.Management
# System.Runtime.Serialization
# System.Windows.Forms
# System.Xml
apt-get download libmono-corlib4.5-cil libmono-system4.0-cil libmono-system-core4.0-cil libmono-system-xml4.0-cil libmono-system-drawing4.0-cil libmono-system-management4.0-cil libmono-system-runtime-serialization4.0-cil libmono-system-windows-forms4.0-cil
# Find Files
CORLIB=$(find . -maxdepth 1 -type f -name libmono-corlib4*)
SYSTEM=$(find . -maxdepth 1 -type f -name libmono-system4*)
SYSTEMCORE=$(find . -maxdepth 1 -type f -name libmono-system-core4*)

SYSTEMDRAW=$(find . -maxdepth 1 -type f -name libmono-system-drawing4*)
SYSTEMMANG=$(find . -maxdepth 1 -type f -name libmono-system-management4*)
SYSTEMSERI=$(find . -maxdepth 1 -type f -name libmono-system-runtime-serialization4*)
SYSTEMFORMS=$(find . -maxdepth 1 -type f -name libmono-system-windows-forms4*)
SYSTEMXML=$(find . -maxdepth 1 -type f -name libmono-system-xml4*)

# Files Required By Above files
# System.Configuration
# System.ServiceModel.Internals
# Mono.Posix
# Accessibility.dll
# Related i18n files
apt-get download libmono-system-configuration4.0 libmono-system-servicemodel-internals0.0-cil libmono-accessibility4.0-cil libmono-posix4.0-ci libmono-i18n4.0-cil libmono-i18n-west4.0-cil
# Find Files
SYSTEMCONF=$(find . -maxdepth 1 -type f -name libmono-system-configuration4*)
SYSTEMSM_INT=$(find . -maxdepth 1 -type f -name libmono-system-servicemodel-internals0*)
MONO_POSIX=$(find . -maxdepth 1 -type f -name libmono-posix4*)

ACCESSIBILITY=$(find . -maxdepth 1 -type f -name libmono-accessibility4*)
I18NCORE=$(find . -maxdepth 1 -type f -name libmono-i18n4*)
I18NWEST=$(find . -maxdepth 1 -type f -name libmono-i18n-west4*)

MissingAnyCheck $LIBMONOSGEN $LIBMONODEV $LIBMGDIPLUS $LIBMONOCOMMON
MISSING=$?
MissingAnyCheck $CORLIB $SYSTEM $SYSTEMCORE $SYSTEMDRAW $SYSTEMMANG $SYSTEMSERI $SYSTEMFORMS $SYSTEMXML
MISSING+=$?
MissingAnyCheck $SYSTEMCONF $SYSTEMSM_INT $MONO_POSIX $ACCESSIBILITY $I18NCORE $I18NWEST
MISSING+=$?

if [ "$MISSING" -ne 0 ]; then
	echo "Unpack Mono i386 Failed"
    RemoveAllPackage $LIBMONOSGEN $LIBMONODEV $LIBMGDIPLUS $LIBMONOCOMMON
    RemoveAllPackage $CORLIB $SYSTEM $SYSTEMCORE $SYSTEMDRAW $SYSTEMMANG $SYSTEMSERI $SYSTEMFORMS $SYSTEMXML
    RemoveAllPackage $SYSTEMCONF $SYSTEMSM_INT $MONO_POSIX $ACCESSIBILITY $I18NCORE $I18NWEST
	exit 1
fi

# Unpack
UnpackAllPackage $LIBMONOSGEN $LIBMONODEV $LIBMGDIPLUS $LIBMONOCOMMON
UnpackAllPackage $CORLIB $SYSTEM $SYSTEMCORE $SYSTEMDRAW $SYSTEMMANG $SYSTEMSERI $SYSTEMFORMS $SYSTEMXML
UnpackAllPackage $SYSTEMCONF $SYSTEMSM_INT $MONO_POSIX $ACCESSIBILITY $I18NCORE $I18NWEST

# Cleanup
RemoveAllPackage $LIBMONOSGEN $LIBMONODEV $LIBMGDIPLUS $LIBMONOCOMMON
RemoveAllPackage $CORLIB $SYSTEM $SYSTEMCORE $SYSTEMDRAW $SYSTEMMANG $SYSTEMSERI $SYSTEMFORMS $SYSTEMXML
RemoveAllPackage $SYSTEMCONF $SYSTEMSM_INT $MONO_POSIX $ACCESSIBILITY $I18NCORE $I18NWEST

rm "usr/share" -r
