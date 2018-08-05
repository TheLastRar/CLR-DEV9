#Building CLR_DEV9 on Linux (CoreCLR)

The following instructions assume that CLR-DEV9, CoreCLR & CoreFX repos have been cloned into a folder called git

##Plugin Files

There are VSCode tasks that automate building the plugins, however if you don't want to install VSCode you can refer to instructions for building without VSCode.

###Build Requirements

Install g++, g++-multilib, make, cmake and [dotnet sdk 2.1 or newer](https://www.microsoft.com/net/download/dotnet-core/2.1)

###Building with VSCode

Install VSCode

In VSCode, install the C# extension by Microsoft

Open the folder git/CLR-DEV9 in VSCode

Run the following build tasks
1. `Build CLR_DEV9_CORE`
2. `Build CLR_DEV9_LINUX`

###Building without VSCode

In the folder git/CLR-DEV9, run the following commands
1. `dotnet restore`
2. `dotnet msbuild CLR_DEV9.sln /t:CLR_DEV9_CORE /p:Configuration="DEBUG" /p:Platform="Any CPU" /property:GenerateFullPaths=true`

in the folder git/CLR-DEV9/CLR_DEV9_LINUX, run `build.sh`

###Installing the plugin

Copy CLR_DEV9_CORE.dll from CLR_DEV9/bin/<Release|Debug>/netcoreapp2.1/ into PCSX2's inis folder (Typically `~/.config/PCSX2/inis/`)

Copy libclrdev9.so from CLR_DEV9_LINUX/bin/x86/<Release|Debug>/ into PCSX2's plugin folder (You can find this in PCSX2 by going to the plugin selection screen) 

##CoreCLR & CoreFX

At the time of writing, dotnet core lacks a x86 package, so you will have to build one yourself

###Build Requirements

Install all the dependencies listed here https://github.com/dotnet/coreclr/blob/master/Documentation/building/linux-instructions.md#toolchain-setup
Install all the dependencies listed here https://github.com/dotnet/corefx/blob/master/Documentation/building/unix-instructions.md#prerequisites-native-build
(You should also install 32bit versions of the dependencies listed in the managed build as these are required by the runtime, key requirements are libunwind and libssl1.0)

Install debootstrap qemu-user-static binfmt-support

###(CoreCLR)[https://github.com/dotnet/coreclr]

Create a rootfs for CoreCLR
`sudo ./cross/build-rootfs.sh x86`

Build CoreCLR
`./build.sh cross x86 skipnuget debug skiptests ignorewarnings cmakeargs "-DSKIP_LLDBPLUGIN=true"`

###(CoreFX))[https://github.com/dotnet/corefx]

Create a rootfs for CoreFX
`sudo ./cross/build-rootfs.sh x86`

Build CoreFX Native
./build-native.sh -debug -buildArch=x86 -- cross

Build CoreFX Managed
./build-managed.sh -BuildTests=false

###Combine CoreCLR & CoreFX

Create a folder called `coreclr` in the parent of PCSX2's inis directory (Typically `~/.config/PCSX2/`)

copy the contents of `git/coreclr/bin/Product/Linux.x86.<Debug/Release>/` into the folder you just created.
copy all .so files in `git/corefx/bin/Product/Linux.x86.<Debug/Release>/native/` into the folder you just created.
copy all .dll files in `git/corefx/bin/runtime/netcoreapp-Linux-<Debug/Release>-x64/` into the folder you just created.

The plugin should now be ready to use.
