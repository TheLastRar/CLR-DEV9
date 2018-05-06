#include "PSE.h"

//#include <dirent.h>
#include <dlfcn.h>
#include <limits.h>
#include <unistd.h>
//#include <fcntl.h>
//#include <sys/types.h>
//#include <sys/stat.h>

using namespace std;

//Public
MonoImage *pluginImage = NULL;

//coreclr_create_delegate_ptr createDelegate;
//Helper Methods
ThunkGetDelegate CyclesCallbackFromFunctionPointer;
ThunkGetFuncPtr FunctionPointerFromIRQHandler;

PluginLog PSELog;

//Private
//Is a char* because strings will init
//after constuctors are called
const char* pseDomainName = "PSE_Mono";

MonoDomain* pseDomain = NULL; //Base Domain
MonoDomain* pluginDomain = NULL; //Plugin Specific Domain
MonoAssembly* pluginAssembly = NULL; //Plugin
//PluginImage

typedef MonoString*(*ThunkGetLibName)(MonoException** ex);
ThunkGetLibName managedGetLibName;

typedef uint32_t(*ThunkGetLibType)(MonoException** ex);
ThunkGetLibType managedGetLibType;

typedef uint32_t(*ThunkGetLibVersion2)(uint32_t type, MonoException** ex);
ThunkGetLibVersion2 managedGetLibVersion2;

static char* pluginNamePtr = NULL;

EXPORT_C_(const char*)
PS2EgetLibName(void)
{
	mono_thread_attach(mono_get_root_domain());
	mono_domain_set(pluginDomain, false);
	mono_thread_attach(mono_domain_get());

	MonoException* ex;
	MonoString* ret = managedGetLibName(&ex);

	mono_domain_set(mono_get_root_domain(), false);

	if (pluginNamePtr != NULL)
	{
		mono_free(pluginNamePtr);
		pluginNamePtr = NULL;
	}

	if (ex)
	{
		mono_print_unhandled_exception((MonoObject*)ex);
		throw;
	}

	pluginNamePtr = mono_string_to_utf8(ret);

	return pluginNamePtr;
}

EXPORT_C_(uint32_t)
PS2EgetLibType(void)
{
	mono_thread_attach(mono_get_root_domain());
	mono_domain_set(pluginDomain, false);
	mono_thread_attach(mono_domain_get());

	MonoException* ex;
	uint32_t ret = managedGetLibType(&ex);

	mono_domain_set(mono_get_root_domain(), false);

	if (ex)
	{
		mono_print_unhandled_exception((MonoObject*)ex);
		throw;
	}

	return ret;
}

EXPORT_C_(uint32_t)
PS2EgetLibVersion2(uint32_t type)
{
	mono_domain_set(pluginDomain, false);

	MonoException* ex;
	uint32_t ret = managedGetLibVersion2(type, &ex);

	mono_domain_set(mono_get_root_domain(), false);

	if (ex)
	{
		mono_print_unhandled_exception((MonoObject*)ex);
		throw;
	}

	return ret;
}

void LoadCoreCLR(char* pluginData, size_t pluginLength, const char* configData, string monoUsrLibFolder, string monoEtcFolder)
{
	PSELog.WriteLn("Init Mono Runtime");

	if (pluginDomain != NULL)
	{
		//Check pluginImage
		return;
	}

	//Check if mono is already active
	pseDomain = mono_get_root_domain();
		
	if (pseDomain == NULL)
	{
		//Inc Reference
		dlopen("libmono-2.0.so", RTLD_NOW | RTLD_LOCAL);

		//LoadInitialFD();

		PSELog.WriteLn("Set Dirs");

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
		PSELog.Write("Set Debug\n");
		mono_debug_init(MONO_DEBUG_FORMAT_MONO);
#endif

		PSELog.Write("JIT Init\n");
		pseDomain = mono_jit_init(pseDomainName);
		if (pseDomain == NULL)
		{
			PSELog.Write("Init Mono Failed At jit_init\n");
			return;
		}
		else
		{
			PSELog.WriteLn(mono_domain_get_friendly_name(pseDomain));
		}

		PSELog.WriteLn("Set Main Args()");

		char pcsx2Path[PATH_MAX];
		size_t len = readlink("/proc/self/exe", pcsx2Path, PATH_MAX - 1);
		if (len < 0)
		{
			PSELog.Write("Init CLR Failed At readlink\n");
			CloseCoreCLR();
			return;
		}

		pcsx2Path[len] = 0;
		//PSELog.WriteLn("PCSX2 Path is %s", pcsx2Path);

		char* argv[1];
		argv[0] = pcsx2Path;//(char *)pluginPath.c_str();

		int32_t ret = mono_runtime_set_main_args(1, argv);
		mono_domain_set_config(pseDomain, ".", "");

		if (ret != 0)
		{
			CloseCoreCLR();
			return;
		}
	}
	else
	{
		PSELog.WriteLn("Mono Already Running");
		PSELog.WriteLn(mono_domain_get_friendly_name(pseDomain));
		mono_thread_attach(mono_get_root_domain());
	}

	pluginDomain = mono_domain_create_appdomain("AssemblyDomain" ,NULL);
	mono_domain_set_config(pluginDomain, ".", "");

	if (!mono_domain_set(pluginDomain, false))
	{
		PSELog.WriteLn("Set Domain Failed");
		CloseCoreCLR();
		return;
	}

	PSELog.WriteLn("Load Image");
	MonoImageOpenStatus status;
	pluginImage = mono_image_open_from_data_full(pluginData, pluginLength, true, &status, false);

	if (!pluginImage | (status != MONO_IMAGE_OK))
	{
		PSELog.WriteLn("Init Mono Failed At PluginImage");
		CloseCoreCLR();
		return;
	}

	PSELog.WriteLn("Load Assembly");
	pluginAssembly = mono_assembly_load_from_full(pluginImage, "", &status, false);

	if (!pluginAssembly | (status != MONO_IMAGE_OK))
	{
		PSELog.WriteLn("Init Mono Failed At PluginAssembly");
		CloseCoreCLR();
		return;
	}

	mono_config_parse_memory(configData);

	PSELog.WriteLn("Get PSE classes");

	MonoClass *pseClass;
	MonoClass *pseClass_mono;

	pseClass = mono_class_from_name(pluginImage, "PSE", "CLR_PSE");
	pseClass_mono = mono_class_from_name(pluginImage, "PSE", "CLR_PSE_Mono");

	if (!pseClass | !pseClass_mono)
	{
		PSELog.WriteLn("Failed to load CLR_PSE classes");
		CloseCoreCLR();
		return;
	}

	PSELog.WriteLn("Get PSE Methods");

	MonoMethod* meth;

	meth = mono_class_get_method_from_name(pseClass, "PS2EgetLibName", 0);
	managedGetLibName = (ThunkGetLibName)mono_method_get_unmanaged_thunk(meth);

	meth = mono_class_get_method_from_name(pseClass, "PS2EgetLibType", 0);
	managedGetLibType = (ThunkGetLibType)mono_method_get_unmanaged_thunk(meth);
	
	meth = mono_class_get_method_from_name(pseClass, "PS2EgetLibVersion2", 1);
	managedGetLibVersion2 = (ThunkGetLibVersion2)mono_method_get_unmanaged_thunk(meth);

	PSELog.WriteLn("Get helpers");

	meth = mono_class_get_method_from_name(pseClass_mono, "CyclesCallbackFromFunctionPointer", 1);
	CyclesCallbackFromFunctionPointer = (ThunkGetDelegate)mono_method_get_unmanaged_thunk(meth);

	meth = mono_class_get_method_from_name(pseClass_mono, "FunctionPointerFromIRQHandler", 1);
	FunctionPointerFromIRQHandler = (ThunkGetFuncPtr)mono_method_get_unmanaged_thunk(meth);

	if (!mono_domain_set(pseDomain, false))
	{
		PSELog.WriteLn("Set Domain Failed");
		CloseCoreCLR();
		return;
	}

	PSELog.WriteLn("Init CLR Done");
}

void CloseCoreCLR()
{
	mono_domain_set(pluginDomain, false);
	if (pluginNamePtr != NULL)
	{
		mono_free(pluginNamePtr);
		pluginNamePtr = NULL;
	}
	if (pluginDomain != NULL)
	{
		mono_domain_set(pseDomain, false);
		PSELog.WriteLn("%p", pluginDomain);
		mono_domain_unload(pluginDomain);
		pluginDomain = NULL;
		//Also unloads the assembly
		pluginAssembly = NULL;
	}
	if (pluginImage != NULL)
	{
		//mono_image_close(pluginImage);
		pluginImage = NULL;
	}
	mono_domain_set(pseDomain, false);
	//LoadExtraFD();
	//CloseCLRFD();
}

