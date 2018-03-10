#include <pthread.h>
#include <cstdio>
#include <iostream>
#include <dlfcn.h>

#include <fcntl.h>   // open
#include <unistd.h>  // read, write, close
#include <cstdio>    // BUFSIZ

void* main_ext(char* path)
{
	void* hld = dlopen(path, 3);
	if (hld == NULL)
	{
		printf("OPEN FAILED\n");
		throw;
	}
	void(*PS2Esetset)(const char *);
	int32_t(*PS2Einit)();

	int32_t(*PS2Etype)();
	int32_t(*PS2Ever)(int32_t);
	//void(*TestInit)();
	//void(*DEV9config)();

	//PS2Esetset = (void(*)(const char *))dlsym(hld, "DEV9setSettingsDir");
	//PS2Einit = (int32_t(*)())dlsym(hld, "DEV9init");

	//DEV9config = (void(*)())dlsym(hld, "DEV9configure");
	
	PS2Etype = (int32_t(*)())dlsym(hld, "PS2EgetLibType");
	PS2Ever = (int32_t(*)(int32_t))dlsym(hld, "PS2EgetLibVersion2");
	//TestInit = (void(*)())dlsym(hld, "TestInit");
	//TestInit();
	//PS2Esetset("/home/air/.config/PCSX2/inis_1.4.0/");
	//PS2Einit();
	//DEV9config();
	int type = PS2Etype();
	int ver = PS2Ever(type);

	//dlclose(hld);
	return hld;
}

int main()
{

	//main_int();
	char* p1 = "/home/air/projects/CLR_DEV9_LINUX_MONO/bin/x86/Debug/libCLR_DEV9_LINUX_MONO.so";
	char* p2 = "/home/air/projects/CLR_DEV9_LINUX_MONO/bin/x86/Debug/libCLR_DEV9_LINUX_MONO_copy.so";

	//Copy
	char buf[BUFSIZ];
	size_t size;

	int source = open(p1, O_RDONLY, 0);
	int dest = open(p2, O_WRONLY | O_CREAT | O_TRUNC, 0644);

	while ((size = read(source, buf, BUFSIZ)) > 0) {
		write(dest, buf, size);
	}

	close(source);
	close(dest);
	//

	void* ptr = main_ext(p1);
	dlclose(main_ext(p2));
	dlclose(main_ext(p1));
	dlclose(ptr);

	dlclose(main_ext(p1));
    return 0;
}