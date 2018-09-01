## Building CLR_DEV9 on Linux (Mono)

The following instructions assume that CLR-DEV9

## Plugin Files

There are VSCode tasks that automate building the plugins, however if you don't want to install VSCode you can refer to instructions for building without VSCode.

### Build Requirements

Install g++, g++-multilib, make, cmake and mono-complete

### Building with VSCode

Install VSCode

In VSCode, install the C# extension by Microsoft

Open the folder git/CLR-DEV9 in VSCode

Run the following build task
1. Build CLR_DEV9_LINUX_MONO (The command will also build CLR_DEV9)

### Building without VSCode

In the folder git/CLR-DEV9, run the following command
`msbuild CLR_DEV9.sln /t:CLR_DEV9 /p:Configuration="DEBUG" /p:Platform="Any CPU" /p:DefineConstants=SKIP_DLLEXPORT /property:GenerateFullPaths=true`

In the folder git/CLR-DEV9/CLR_DEV9_LINUX_MONO, run `build.sh`

### Installing the plugin

Extract libclrdev9mono.<Release|Debug>.tar.gz from CLR_DEV9_LINUX_MONO/bin/x86/<Release|Debug>/ into PCSX2's plugin folder (You can find this in PCSX2 by going to the plugin selection screen) 

The plugin should now be ready to use.
