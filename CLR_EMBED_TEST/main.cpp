#include <pthread.h>
#include <cstdio>
#include <iostream>
#include <dlfcn.h>

int main_ext()
{
	void* hld = dlopen("/home/air/projects/CLR_DEV9_LINUX/bin/x86/Debug/libCLR_DEV9_LINUX.so", 3);
	if (hld == NULL)
	{
		printf("OPEN FAILED\n");
	}
	void(*PS2Esetset)(const char *);
	int32_t(*PS2Einit)();

	void(*TestInit)();
	void(*DEV9config)();

	PS2Esetset = (void(*)(const char *))dlsym(hld, "DEV9setSettingsDir");
	PS2Einit = (int32_t(*)())dlsym(hld, "DEV9init");

	DEV9config = (void(*)())dlsym(hld, "DEV9configure");

	TestInit = (void(*)())dlsym(hld, "TestInit");
	TestInit();
	//PS2Esetset("/home/air/.config/PCSX2/inis_1.4.0/");
	//PS2Einit();
	//DEV9config();

	dlclose(hld);
	return 0;
}

int main()
{
	//main_int();
	main_ext();
	main_ext();

    return 0;
}