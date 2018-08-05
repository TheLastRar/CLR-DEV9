#include <dlfcn.h>
#include <limits.h>
#include <unistd.h>
//#include <stdlib.h>
#include <stdio.h>
#include <stdarg.h> 
#include <stdint.h>
#include <string.h>

#include <string>
#include <fstream>

#include <mono/jit/jit.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/reflection.h>

#define EXPORT_C_(type) extern "C" __attribute__((stdcall, externally_visible, visibility("default"))) type

using namespace std;

char* pluginData;

MonoDomain* pluginDomain = NULL;
MonoImage* pluginImage;

typedef void(*ThunkVoid)(MonoException** ex);
ThunkVoid managedFormTest;

char* aPath1 = "../../../MonoTest.dll";

void CloseMono()
{
	if (pluginDomain != NULL)
	{
		//if (pluginNamePtr != NULL)
		//{
		//	mono_free(pluginNamePtr);
		//	pluginNamePtr = NULL;
		//}

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

void LoadMono(char* pluginData, size_t pluginLength, const char* configData, string monoUsrLibFolder, string monoEtcFolder)
{
	printf("Init Mono Runtime\n");

	if (pluginDomain != NULL)
	{
		//Check pluginImage
		return;
	}

	//Check if mono is already active
	if (mono_get_root_domain() == NULL)
	{
		//Inc Reference
		dlopen("libmono-2.0.so", RTLD_NOW | RTLD_LOCAL);

		//LoadInitialFD();

		//PSELog.WriteLn("Set Dirs");

		if (monoUsrLibFolder.length() == 0)
		{
			monoUsrLibFolder = "/usr/lib/";
		}
		if (monoEtcFolder.length() == 0)
		{
			monoEtcFolder = "/etc/";
		}

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
			printf("Init Mono Failed At jit_init\n");
			return;
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
			printf("Init CLR Failed At readlink\n");
			CloseMono();
			return;
		}

		pcsx2Path[len] = 0;
		//PSELog.WriteLn("PCSX2 Path is %s", pcsx2Path);

		char* argv[1];
		argv[0] = pcsx2Path;//(char *)pluginPath.c_str();

		int32_t ret = mono_runtime_set_main_args(1, argv);
		mono_domain_set_config(rootDomain, ".", "");

		if (ret != 0)
		{
			CloseMono();
			return;
		}
	}
	else
	{
		//PSELog.WriteLn("Mono Already Running");
		//PSELog.WriteLn(mono_domain_get_friendly_name(pseDomain));
		mono_thread_attach(mono_get_root_domain());
	}

	pluginDomain = mono_domain_create_appdomain("AssemblyDomain", NULL);
	mono_domain_set_config(pluginDomain, ".", "");

	if (!mono_domain_set(pluginDomain, false))
	{
		printf("Set Domain Failed\n");
		throw;
	}

	mono_config_parse_memory(configData);

	//PSELog.WriteLn("Load Image");
	MonoImageOpenStatus status;
	pluginImage = mono_image_open_from_data_full(pluginData, pluginLength, true, &status, false);

	if (!pluginImage | (status != MONO_IMAGE_OK))
	{
		printf("Init Mono Failed At PluginImage");
		CloseMono();
	}

	//PSELog.WriteLn("Load Assembly");

	MonoAssembly* pluginAssembly = mono_assembly_load_from_full(pluginImage, "", &status, false);
	if (!pluginAssembly | (status != MONO_IMAGE_OK))
	{
		printf("Init Mono Failed At PluginAssembly\n");
		CloseMono();
		return;
	}

	//PSELog.WriteLn("Get PSE classes");

	MonoClass *pseClass;
	//MonoClass *pseClass_mono;

	pseClass = mono_class_from_name(pluginImage, "MonoTest", "TestForms");

	if (!pseClass)
	{
		printf("Failed to load CLR_PSE classes\n");
		CloseMono();
		return;
	}

	printf("Get Test Methods\n");

	MonoMethod* meth;

	meth = mono_class_get_method_from_name(pseClass, "Test", 0);
	managedFormTest = (ThunkVoid)mono_method_get_unmanaged_thunk(meth);

	if (!mono_domain_set(mono_get_root_domain(), false))
	{
		printf("Set Domain Failed\n");
		CloseMono();
		return;
	}

	printf("Init CLR Done\n");
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

__attribute__((constructor))
void initialize_plugin()
{
	size_t s = 0;
	pluginData = LoadData(aPath1, &s);

	LoadMono(pluginData, s, "", "", "");
}

__attribute__((destructor))
void destroy_plugin()
{
	mono_thread_attach(mono_get_root_domain());
	mono_domain_set(pluginDomain, false);
	mono_thread_attach(mono_domain_get());

	CloseMono();

	delete[] pluginData;
}

EXPORT_C_(void)
Test(void)
{
	mono_thread_attach(mono_get_root_domain());
	mono_domain_set(pluginDomain, false);
	mono_thread_attach(mono_domain_get());

	MonoException* ex = NULL;
	managedFormTest(&ex);
}

