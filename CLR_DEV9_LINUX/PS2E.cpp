#include "PSE.h"

using namespace std;

//Public
ThunkGetDelegate CyclesCallbackFromFunctionPointer;
ThunkGetFuncPtr FunctionPointerFromIRQHandler;

PluginLog PSELog;

//Private
const string pseDomainName = "PSE_Mono";

MonoDomain* pseDomain;

static char libraryName[256];

EXPORT_C_(const char*)
PS2EgetLibName(void)
{
	snprintf(libraryName, 255, pluginName.c_str());
	return libraryName;
}

EXPORT_C_(uint32_t)
PS2EgetLibType(void)
{
	return pluginType;
}

EXPORT_C_(uint32_t)
PS2EgetLibVersion2(uint32_t type)
{
	uint8_t apiVersion = 3; //hardcoded for dev9
	return (pluginVerPatch << 24 | apiVersion << 16 | pluginVerMajor << 8 | pluginVerMinor);
}

MonoDomain* LoadMonoSafer(string monousrlibPath, string monoetcPath)
{
	//bool isInit = false;
	//mono_domain_foreach((MonoDomainFunc)CheckAppDomainsFunc, &isInit);

	//if only this worked
	PSELog.Write("Init Mono Runtime\n");
	pseDomain = mono_get_root_domain();

	if (pseDomain == NULL)
	{
		PSELog.Write("Set Dirs\n");

		if (monousrlibPath.length() == 0)
		{
			monousrlibPath = "/usr/lib/";
		}
		if (monoetcPath.length() == 0)
		{
			monoetcPath = "/etc/";
		}

		mono_set_dirs(monousrlibPath.c_str(), monoetcPath.c_str());
		mono_config_parse(NULL);

		PSELog.Write("Set Debug (if only)\n");

		mono_debug_init(MONO_DEBUG_FORMAT_MONO);

		PSELog.Write("JIT Init\n");
		pseDomain = mono_jit_init(pseDomainName.c_str());
		if (pseDomain == NULL)
		{
			PSELog.Write("Init Mono Failed At jit_init\n");
			return NULL;
		}
		else
		{
			PSELog.WriteLn(mono_domain_get_friendly_name(pseDomain));
		}
	}
	PSELog.Write("Done\n");
	return mono_domain_create();
}

MonoImage* LoadPluginPSE(MonoAssembly *pluginAssembly, string pluginPath)
{
	if (!pluginAssembly)
		return NULL;

	MonoImage *pluginImage;

	PSELog.WriteLn("Get plugin image");
	pluginImage = mono_assembly_get_image(pluginAssembly);

	PSELog.WriteLn("Get PSE classes");

	MonoClass *pseClass;
	MonoClass *pseClass_mono;

	pseClass = mono_class_from_name(pluginImage, "PSE", "CLR_PSE");
	pseClass_mono = mono_class_from_name(pluginImage, "PSE", "CLR_PSE_Mono");

	if (!pseClass | !pseClass_mono)
	{
		PSELog.WriteLn("Failed to load CLR_PSE classes");
		mono_image_close(pluginImage);
		return NULL;
	}

	PSELog.WriteLn("Get PSE Main()");
	MonoMethod *main = mono_class_get_method_from_name(pseClass_mono, "Main", 0);

	PSELog.WriteLn("Run PSE Main()");

	char* argv[1];
	argv[0] = (char *)pluginPath.c_str();

	int32_t ret = mono_runtime_run_main(main, 1, argv, NULL);

	if (ret != 0)
	{
		mono_image_close(pluginImage);
		return NULL;
	}

	PSELog.WriteLn("Get helpers");
	MonoMethod *getDelegate = mono_class_get_method_from_name(pseClass_mono, "CyclesCallbackFromFunctionPointer", 1);
	CyclesCallbackFromFunctionPointer = (ThunkGetDelegate)mono_method_get_unmanaged_thunk(getDelegate);

	MonoMethod *getFucnPtr = mono_class_get_method_from_name(pseClass_mono, "FunctionPointerFromIRQHandler", 1);
	FunctionPointerFromIRQHandler = (void*(*)(MonoObject* func, MonoException** ex))mono_method_get_unmanaged_thunk(getFucnPtr);

	PSELog.WriteLn("Done");
	return pluginImage;
}

//__attribute__((constructor)) int DLLOpen()
//{
//	return 0;
//}
