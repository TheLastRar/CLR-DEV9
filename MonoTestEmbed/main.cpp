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

	return hld;
}

int main()
{
	char* p1 = "../../../../MonoTestEmbedLib/bin/x86/Debug/libMonoTestEmbedLib.so";

	void(*MonoTest)();

	printf("Open\n");
	void* ptr1 = main_ext(p1);

	MonoTest = (void(*)())dlsym(ptr1, "Test");
	MonoTest();

	printf("Close\n");
	dlclose(ptr1);

	return 0;
}