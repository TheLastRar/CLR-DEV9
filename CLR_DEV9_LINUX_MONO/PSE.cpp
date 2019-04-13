#include "PSE.h"

//#include <dirent.h>
#include <dlfcn.h>
#include <limits.h>
#include <unistd.h>
//#include <fcntl.h>
//#include <sys/types.h>
//#include <sys/stat.h>

//#define _GNU_SOURCE
#include <link.h>

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

//MonoDomain* pseDomain = NULL; //Base Domain
MonoDomain* pluginDomain = NULL; //Plugin Specific Domain
//PluginImage

typedef MonoString*(*ThunkGetLibName)(MonoException** ex);
ThunkGetLibName managedGetLibName;

typedef uint32_t(*ThunkGetLibType)(MonoException** ex);
ThunkGetLibType managedGetLibType;

typedef uint32_t(*ThunkGetLibVersion2)(uint32_t type, MonoException** ex);
ThunkGetLibVersion2 managedGetLibVersion2;

string pluginNamePtr = "";

//Mono Init config
const char* pluginData;
size_t pluginLength;
const char* configData;
string monoUsrLibFolder = "";
string monoEtcFolder = "";
//Mono Init config

EXPORT_C_(const char*)
PS2EgetLibName(void)
{
	bool preInit = false;
	if (pluginDomain == NULL)
	{
		preInit = true;
		LoadCoreCLR();
	}
	//
	mono_thread_attach(mono_get_root_domain());
	mono_domain_set(pluginDomain, false);
	mono_thread_attach(mono_domain_get());

	MonoException* ex;
	MonoString* ret = managedGetLibName(&ex);

	mono_domain_set(mono_get_root_domain(), false);

	if (ex)
	{
		mono_print_unhandled_exception((MonoObject*)ex);
		throw;
	}

	char* pNameChar = mono_string_to_utf8(ret);
	pluginNamePtr = pNameChar;
	mono_free(pNameChar);

	if (preInit)
	{
		CloseCoreCLR();
	}

	return pluginNamePtr.c_str();
}

EXPORT_C_(uint32_t)
PS2EgetLibType(void)
{
	bool preInit = false;
	if (pluginDomain == NULL)
	{
		preInit = true;
		LoadCoreCLR();
	}
	//
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

	if (preInit)
	{
		CloseCoreCLR();
	}

	return ret;
}

EXPORT_C_(uint32_t)
PS2EgetLibVersion2(uint32_t type)
{
	bool preInit = false;
	if (pluginDomain == NULL)
	{
		preInit = true;
		LoadCoreCLR();
	}

	mono_domain_set(pluginDomain, false);

	MonoException* ex;
	uint32_t ret = managedGetLibVersion2(type, &ex);

	mono_domain_set(mono_get_root_domain(), false);

	if (ex)
	{
		mono_print_unhandled_exception((MonoObject*)ex);
		throw;
	}

	if (preInit)
	{
		CloseCoreCLR();
	}

	return ret;
}

int VisitModule(struct dl_phdr_info* info, size_t size, void* data) 
{
	const char** path = (const char**)data;

	const char* name = info->dlpi_name;

	uintptr_t base_address = info->dlpi_addr;
	uintptr_t total_size = 0;
	for (int i = 0; i < info->dlpi_phnum; i++) {
		total_size += info->dlpi_phdr[i].p_memsz;
	}

	if ((base_address < (uintptr_t)VisitModule) & ((uintptr_t)VisitModule < base_address + total_size))
	{
		//PSELog.WriteLn("Found Self");
		//PSELog.WriteLn(name);
		*path = name;
		return 1;
	}

	return 0;
}

const char* GetModulePath() {
	const char* path = nullptr;
	dl_iterate_phdr(VisitModule, (void*)&path);
	return path;
}

void CoreCLRConfig(char* parPluginData, size_t parPluginLength, const char* parConfigData, string parMonoUsrLibFolder, string parMonoEtcFolder)
{
	pluginData = parPluginData;
	pluginLength = parPluginLength;
	configData = parConfigData;
	if (parMonoUsrLibFolder.length() != 0)
	{
		monoUsrLibFolder = parMonoUsrLibFolder;
	}
	if (parMonoEtcFolder.length() != 0)
	{
		monoEtcFolder = parMonoEtcFolder;
	}
}

void LoadCoreCLR()
{
	PSELog.WriteLn("Init Mono Runtime");

	if (pluginDomain != NULL)
	{
		//Check pluginImage
		return;
	}

	//Local lib dir
	string x86LocalLibPath = GetModulePath();
	x86LocalLibPath = x86LocalLibPath.substr(0, x86LocalLibPath.find_last_of("/")) + "/mono_i386/usr/lib/";

	//Check if mono is already active
	if (mono_get_root_domain() == NULL)
	{
		//Inc Reference
		dlopen("libmonosgen-2.0.so", RTLD_NOW | RTLD_LOCAL);

		//LoadInitialFD();

		//PSELog.WriteLn("Set Dirs");
		if (monoUsrLibFolder.length() == 0)
		{
			monoUsrLibFolder = x86LocalLibPath;//"/usr/lib/";
		}
		if (monoEtcFolder.length() == 0)
		{
			//Do we need local copy of etc?
			monoEtcFolder = "/etc/";
		}

		mono_set_dirs(monoUsrLibFolder.c_str(), monoEtcFolder.c_str());
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
			PSELog.Write("Init Mono Failed At jit_init\n");
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
			PSELog.Write("Init CLR Failed At readlink\n");
			CloseCoreCLR();
			return;
		}

		pcsx2Path[len] = 0;
		//PSELog.WriteLn("PCSX2 Path is %s", pcsx2Path);

		char* argv[1];
		argv[0] = pcsx2Path;

		int32_t ret = mono_runtime_set_main_args(1, argv);
		mono_domain_set_config(rootDomain, ".", "");

		if (ret != 0)
		{
			CloseCoreCLR();
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
		PSELog.WriteLn("Set Domain Failed\n");
		throw;
	}

	mono_config_parse_memory(configData);

	//Remap native libs
	string gdiPath = x86LocalLibPath + "libgdiplus.so";
	string mphPath = x86LocalLibPath + "libMonoPosixHelper.so";

	if (access(gdiPath.c_str(), F_OK) != -1)
	{
		//PSELog.WriteLn("Redirect Native Mono Libs");
		mono_dllmap_insert(nullptr, "gdiplus",     nullptr, gdiPath.c_str(), nullptr);
		mono_dllmap_insert(nullptr, "gdiplus.dll", nullptr, gdiPath.c_str(), nullptr);
		mono_dllmap_insert(nullptr, "gdi32",       nullptr, gdiPath.c_str(), nullptr);
		mono_dllmap_insert(nullptr, "gdi32.dll",   nullptr, gdiPath.c_str(), nullptr);

		mono_dllmap_insert(nullptr, "MonoPosixHelper", nullptr, mphPath.c_str(), nullptr);
	}

	MonoImageOpenStatus status;

	//Load Plugin
	//PSELog.WriteLn("Load Image");
	pluginImage = mono_image_open_from_data_full((char*)pluginData, pluginLength, true, &status, false);

	if (!pluginImage | (status != MONO_IMAGE_OK))
	{
		PSELog.WriteLn("Init Mono Failed At PluginImage");
		CloseCoreCLR();
		return;
	}

	//PSELog.WriteLn("Load Assembly");
	MonoAssembly* pluginAssembly = mono_assembly_load_from_full(pluginImage, "", &status, false);
	if (!pluginAssembly | (status != MONO_IMAGE_OK))
	{
		PSELog.WriteLn("Init Mono Failed At PluginAssembly");
		CloseCoreCLR();
		return;
	}

	//PSELog.WriteLn("Get PSE classes");

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

	//PSELog.WriteLn("Get PSE Methods");

	MonoMethod* meth;

	meth = mono_class_get_method_from_name(pseClass, "PS2EgetLibName", 0);
	managedGetLibName = (ThunkGetLibName)mono_method_get_unmanaged_thunk(meth);

	meth = mono_class_get_method_from_name(pseClass, "PS2EgetLibType", 0);
	managedGetLibType = (ThunkGetLibType)mono_method_get_unmanaged_thunk(meth);

	meth = mono_class_get_method_from_name(pseClass, "PS2EgetLibVersion2", 1);
	managedGetLibVersion2 = (ThunkGetLibVersion2)mono_method_get_unmanaged_thunk(meth);

	//PSELog.WriteLn("Get helpers");

	meth = mono_class_get_method_from_name(pseClass_mono, "CyclesCallbackFromFunctionPointer", 1);
	CyclesCallbackFromFunctionPointer = (ThunkGetDelegate)mono_method_get_unmanaged_thunk(meth);

	meth = mono_class_get_method_from_name(pseClass_mono, "FunctionPointerFromIRQHandler", 1);
	FunctionPointerFromIRQHandler = (ThunkGetFuncPtr)mono_method_get_unmanaged_thunk(meth);

	if (!mono_domain_set(mono_get_root_domain(), false))
	{
		PSELog.WriteLn("Set Domain Failed");
		CloseCoreCLR();
		return;
	}

	//PSELog.WriteLn("Init CLR Done");
}

void CloseCoreCLR()
{
	PSELog.WriteLn("Close Mono Runtime");
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
	//LoadExtraFD();
	//CloseCLRFD();
}

