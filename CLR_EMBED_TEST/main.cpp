#include <cstdio>
#include <iostream>
#include <dlfcn.h>

#include <mono/jit/jit.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/debug-helpers.h>

//#include <mono/metadata/environment.h>


const char* libraryDomainName = "/home/air/CLR_DEV9.dll";
const char* libaryReqVersion = "v4.0.30319";

int main()
{
    printf("hello from CLR_EMBED_TEST!\n");

	//mono_set_dirs("/usr/lib/mono", "/etc/mono");
	//printf("Set Dirs\n");
	//mono_config_parse(NULL);
	//printf("Set config\n");
	//Required for mdb's to load for detailed stack traces etc.
	//mono_debug_init(MONO_DEBUG_FORMAT_MONO);
	//MonoDomain *domain;
	printf("Innit Ver\n");
	//domain = mono_jit_init_version(libraryDomainName, libaryReqVersion);
	//domain = mono_jit_init(libraryDomainName);
	printf("Opendll\n");
	void* hld = dlopen("/home/air/projects/CLR_DEV9_LINUX/bin/x86/Debug/libCLR_DEV9_LINUX.so", 3);
	if (hld == NULL)
	{
		printf("OPEN FAILED\n");
	}
	int32_t(*PS2EgetLibName)();
	printf("GetFunction\n");
	PS2EgetLibName = (int32_t(*)())dlsym(hld, "DEV9init");
	PS2EgetLibName();

	dlclose(hld);

	hld = dlopen("/home/air/projects/CLR_DEV9_LINUX/bin/x86/Debug/libCLR_DEV9_LINUX.so", 3);
	PS2EgetLibName = (int32_t(*)())dlsym(hld, "DEV9init");
	PS2EgetLibName();
	//printf(PS2EgetLibName());
	std::cin.ignore(10000, '\n');
    return 0;
}