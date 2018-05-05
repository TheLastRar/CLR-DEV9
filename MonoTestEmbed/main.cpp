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

MonoDomain* pluginDomain;
MonoImage* pluginImage;
MonoAssembly* pluginAssembly;

typedef void(*ThunkVoid)(MonoException** ex);
ThunkVoid managedFormTest;

void Load()
{
	char* aPath = "../../../MonoTest.dll";

	printf("Init Mono Runtime\n");

	//string monoUsrLibFolder = "/usr/lib/";
	//string monoEtcFolder = "/etc/";

	//mono_set_dirs(monoUsrLibFolder.c_str(), monoEtcFolder.c_str());
	mono_config_parse(NULL);
	mono_set_signal_chaining(true);

	printf("Set Debug\n");
	mono_debug_init(MONO_DEBUG_FORMAT_MONO);

	printf("JIT Init\n");
	MonoDomain* pseDomain = mono_jit_init("Root Domain");
	if (pseDomain == NULL)
	{
		printf("Init Mono Failed At jit_init\n");
		throw;
	}
	else
	{
		printf(mono_domain_get_friendly_name(pseDomain));
		printf("\n");
	}

	printf("Set Main Args()\n");

	//char pcsx2Path[PATH_MAX];
	//size_t len = readlink("/proc/self/exe", pcsx2Path, PATH_MAX - 1);
	//if (len < 0)
	//{
	//	printf("Init CLR Failed At readlink\n");
	//	throw;
	//}

	//pcsx2Path[len] = 0;

	char* argv[1];
	argv[0] = aPath;//pcsx2Path;

	int32_t ret = mono_runtime_set_main_args(1, argv);
	mono_domain_set_config(pseDomain, ".", "");

	if (ret != 0)
	{
		throw;
	}

	printf("Create Domain\n");

	pluginDomain = mono_domain_create_appdomain("AssemblyDomain", NULL);
	mono_domain_set_config(pluginDomain, ".", "");

	if (!mono_domain_set(pluginDomain, false))
	{
		printf("Set Domain Failed\n");
		throw;
	}

	printf("Load Image\n");
	MonoImageOpenStatus status;
	//pluginImage = mono_image_open_from_data_full(pluginData, pluginLength, true, &status, false);
	pluginImage = mono_image_open_full(aPath, &status, false);

	if (!pluginImage | (status != MONO_IMAGE_OK))
	{
		printf("Init Mono Failed At PluginImage\n");
		throw;
	}

	printf("Load Assembly\n");
	pluginAssembly = mono_assembly_load_from_full(pluginImage, "", &status, false);

	if (!pluginAssembly | (status != MONO_IMAGE_OK))
	{
		printf("Init Mono Failed At PluginAssembly");
		throw;
	}

	//mono_config_parse_memory(configData);

	printf("Get PSE classes\n");

	MonoClass *pseClass_mono;

	pseClass_mono = mono_class_from_name(pluginImage, "MonoTest", "TestForms");

	if (!pseClass_mono)
	{
		printf("Failed to load CLR_PSE classes\n");
		throw;
	}

	printf("Get PSE Methods\n");

	MonoMethod* meth;

	meth = mono_class_get_method_from_name(pseClass_mono, "Test", 0);
	managedFormTest = (ThunkVoid)mono_method_get_unmanaged_thunk(meth);

	//if (!mono_domain_set(pseDomain, false))
	//{
	//	PSELog.WriteLn("Set Domain Failed");
	//	CloseCoreCLR();
	//	return;
	//}

	printf("Init CLR Done\n");
}

void Close()
{
	//mono_domain_set(pluginDomain, false);
	if (pluginDomain != NULL)
	{
		mono_domain_unload(pluginDomain);
		pluginDomain = NULL;
		//Also unloads the assembly
		pluginAssembly = NULL;
	}
	if (pluginImage != NULL)
	{
		mono_image_close(pluginImage);
		pluginImage = NULL;
	}
	mono_domain_set(mono_get_root_domain(), false);
}

enum TestEnum
{
	Foo,
	Bar,
};

void TestEnum()
{
	MonoClass *mEnum;
	MonoClass *mClass;

	mClass = mono_class_from_name(pluginImage, "MonoTest", "TestEnumCtor");

	MonoMethod *meth;
	meth = mono_class_get_method_from_name(mClass, ".ctor", 1);

	void(*thunkCtor)(MonoObject* obj, int e, MonoException** ex);

	thunkCtor = (void(*)(MonoObject*, int, MonoException**))mono_method_get_unmanaged_thunk(meth);

	MonoObject *mClassInstance = mono_object_new(mono_domain_get(), mClass);
	MonoException* ex = NULL;

	thunkCtor(mClassInstance, Bar, &ex);

	if (ex)
	{
		mono_print_unhandled_exception((MonoObject*)ex);
		throw;
	}

	//Start Calling Methods
	meth = mono_class_get_method_from_name(mClass, "PrintValue", 0);

	void(*thunkPrint)(MonoObject* obj, MonoException** ex);

	thunkPrint = (void(*)(MonoObject*, MonoException**))mono_method_get_unmanaged_thunk(meth);

	thunkPrint(mClassInstance, &ex);

	if (ex)
	{
		mono_print_unhandled_exception((MonoObject*)ex);
		throw;
	}
}

int main()
{
	printf("Hello from MonoTestEmbed!\n");
	Load();

	MonoException* ex = NULL;
	//managedFormTest(&ex);

	TestEnum();

	//mono_domain_set(mono_get_root_domain(), false);

	if (ex)
	{
		mono_print_unhandled_exception((MonoObject*)ex);
		throw;
	}

	Close();

	return 0;
}