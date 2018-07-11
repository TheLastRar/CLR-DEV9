#include <cstdio>
#include <mono/jit/jit.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/reflection.h>
#include <dlfcn.h>
#include <limits.h>
#include <unistd.h>

#include <fcntl.h>   // open
#include <unistd.h>  // read, write, close
#include <cstdio>    // BUFSIZ

#include <fstream>

//MonoDomain* pluginDomain;
//MonoImage* pluginImage;
//MonoAssembly* pluginAssembly;

typedef void(*ThunkVoid)(MonoException** ex);
//ThunkVoid managedFormTest;

//char* aPath1 = "../../../MonoTest.dll";
char* aPath1 = "/home/air/projects/CLR_DEV9_LINUX_MONO/CLR_DEV9.dll";
char* aPath2 = "../../../MonoTest_Copy.dll";

void StartMono()
{
	if (mono_get_root_domain() == NULL)
	{
		//Inc Reference
		dlopen("libmono-2.0.so", RTLD_NOW | RTLD_LOCAL);

		//LoadInitialFD();

		//PSELog.WriteLn("Set Dirs");

		//if (monoUsrLibFolder.length() == 0)
		//{
		//	monoUsrLibFolder = "/usr/lib/";
		//}
		//if (monoEtcFolder.length() == 0)
		//{
		//	monoEtcFolder = "/etc/";
		//}

		//mono_set_dirs(monoUsrLibFolder.c_str(), monoEtcFolder.c_str());
		mono_config_parse(NULL);
		mono_set_signal_chaining(true);

#if !NDEBUG
		//PSELog.Write("Set Debug\n");
		mono_debug_init(MONO_DEBUG_FORMAT_MONO);
#endif

		//PSELog.Write("JIT Init\n");
		MonoDomain* rootDomain = mono_jit_init(/*pseDomainName*/"PSE_Mono");

		if (rootDomain == NULL)
		{
			throw;
		}
		else
		{
			//PSELog.WriteLn(mono_domain_get_friendly_name(pseDomain));
		}

		//PSELog.WriteLn("Set Main Args()");

		char pcsx2Path[PATH_MAX];
		size_t len = readlink("/proc/self/exe", pcsx2Path, PATH_MAX - 1);
		if (len < 0)
		{
			throw;
		}

		pcsx2Path[len] = 0;
		//PSELog.WriteLn("PCSX2 Path is %s", pcsx2Path);

		char* argv[1];
		argv[0] = pcsx2Path;//(char *)pluginPath.c_str();

		int32_t ret = mono_runtime_set_main_args(1, argv);
		mono_domain_set_config(rootDomain, ".", "");

		if (ret != 0)
		{
			throw;
		}
	}
	else
	{
		//PSELog.WriteLn("Mono Already Running");
		//PSELog.WriteLn(mono_domain_get_friendly_name(pseDomain));
		mono_thread_attach(mono_get_root_domain());
	}
}

void DupTest()
{
	//Copy
	char buf[BUFSIZ];
	size_t size;

	int source = open(aPath1, O_RDONLY, 0);
	int dest = open(aPath2, O_WRONLY | O_CREAT | O_TRUNC, 0644);

	while ((size = read(source, buf, BUFSIZ)) > 0) {
		write(dest, buf, size);
	}

	close(source);
	close(dest);
}
char* LoadData(char* aPath, size_t* size)
{
	std::ifstream shaderFile;
	shaderFile.open(aPath, std::ios::binary | std::ios::ate);
	if (!shaderFile.is_open())
	{
		throw;
	}

	*size = shaderFile.tellg();
	shaderFile.seekg(0, std::ios::beg);
	char* bin = new char[*size];
	shaderFile.read(bin, *size);

	return bin;
}

MonoDomain* LoadDomain()
{
	MonoDomain* retDomain = mono_domain_create_appdomain("AssemblyDomain", NULL);
	mono_domain_set_config(retDomain, ".", "");

	if (!mono_domain_set(retDomain, false))
	{
		printf("Set Domain Failed\n");
		throw;
	}

	//mono_config_parse_memory(configData);

	return retDomain;
}

MonoImage* LoadImage(char* pluginData, size_t pluginLength)
{
	//PSELog.WriteLn("Load Image");
	MonoImageOpenStatus status;
	MonoImage* retImage = mono_image_open_from_data_full(pluginData, pluginLength, true, &status, false);

	if (!retImage | (status != MONO_IMAGE_OK))
	{
		throw;
	}

	return retImage;
}

void LoadMethod(MonoDomain* pluginDomain, MonoImage* pluginImage)
{
	if (!mono_domain_set(pluginDomain, false))
	{
		printf("Set Domain Failed\n");
		throw;
	}

	printf("Load Assembly\n");
	
	MonoImageOpenStatus status = (MonoImageOpenStatus)9000;
	
	MonoAssembly* pluginAssembly = mono_assembly_load_from_full(pluginImage, "", &status, false);

	//if (!pluginAssembly | (status != MONO_IMAGE_OK))
	//{
	//	printf("Init Mono Failed At PluginAssembly");
	//	throw;
	//}

	//printf("Get PSE classes\n");

	//MonoClass *pseClass_mono;

	//pseClass_mono = mono_class_from_name(pluginImage, "MonoTest", "TestForms");

	//if (!pseClass_mono)
	//{
	//	printf("Failed to load CLR_PSE classes\n");
	//	throw;
	//}

	//printf("Get PSE Methods\n");

	//MonoMethod* meth;

	//meth = mono_class_get_method_from_name(pseClass_mono, "Test", 0);
	//ThunkVoid managedFormTest = (ThunkVoid)mono_method_get_unmanaged_thunk(meth);

	if (!mono_domain_set(mono_get_root_domain(), false))
	{
		printf("Set Domain Failed");
		throw;
	}
}

void Close(MonoDomain* pluginDomain, MonoImage* pluginImage)
{
	mono_domain_set(pluginDomain, false);
	if (pluginDomain != NULL)
	{
		mono_domain_set(mono_get_root_domain(), false);
		mono_domain_unload(pluginDomain);
		pluginDomain = NULL;
		//Also unloads the assembly
	}
	if (pluginImage != NULL)
	{
		mono_image_close(pluginImage);
		pluginImage = NULL;
	}
	mono_domain_set(mono_get_root_domain(), false);
}

int main()
{
	printf("Hello from MonoTestEmbed!\n");

	DupTest();

	size_t s = 0;

	char* pluginData = LoadData(aPath1, &s);

	StartMono();
	MonoDomain* pluginDomain1 = LoadDomain();
	MonoImage* pluginImage1 = LoadImage(pluginData, s);
	LoadMethod(pluginDomain1, pluginImage1);

	mono_domain_set(pluginDomain1, false);

	StartMono();
	MonoDomain* pluginDomain2 = LoadDomain();
	MonoImage* pluginImage2 = LoadImage(pluginData, s);
	LoadMethod(pluginDomain2, pluginImage2);

	mono_domain_set(pluginDomain2, false);

	//MonoException* ex = NULL;
	//managedFormTest(&ex);

	//mono_domain_set(mono_get_root_domain(), false);

	//if (ex)
	//{
	//	mono_print_unhandled_exception((MonoObject*)ex);
	//	throw;
	//}

	Close(pluginDomain2, pluginImage2);

	Close(pluginDomain1, pluginImage1);

	delete[] pluginData;

	return 0;
}