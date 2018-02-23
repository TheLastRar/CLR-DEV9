#include "PSE.h"

#include <dirent.h>
#include <dlfcn.h>
#include <limits.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>

#include <set>
#include <vector>

using namespace std;

//Public
MonoImage *pluginImage = NULL;
MonoDomain* pluginDomain = NULL; //Plugin Specific Domain

//coreclr_create_delegate_ptr createDelegate;
//Helper Methods
ThunkGetDelegate CyclesCallbackFromFunctionPointer;
ThunkGetFuncPtr FunctionPointerFromIRQHandler;

PluginLog PSELog;

//Private
const string pseDomainName = "PSE_Mono";

MonoDomain* pseDomain = NULL; //Base Domain
//PluginDomain
MonoAssembly* pluginAssembly = NULL; //Plugin
//PluginImage

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

EXPORT_C_(void)
TestInit()
{
	LoadCoreCLR("/home/air/.config/PCSX2/inis_1.4.0/CLR_DEV9_CORE.dll", "", "");
	CloseCoreCLR();
}

//HACKFIX (The PipeCleaner)
//PCSX2 will hang if extra stdout/stderr handles
//opened by CoreCLR are left open after shutdown
string p1;
string p2;
vector<int32_t> p1FDs;
vector<int32_t> p2FDs;
vector<int32_t> p1ClrFDs;
vector<int32_t> p2ClrFDs;

string GetFDname(int32_t fd)
{
	char buf[256];

	int32_t fd_flags = fcntl(fd, F_GETFD);
	if (fd_flags == -1) return "";

	int32_t fl_flags = fcntl(fd, F_GETFL);
	if (fl_flags == -1) return "";

	char path[256];
	sprintf(path, "/proc/self/fd/%d", fd);

	memset(&buf[0], 0, 256);
	ssize_t s = readlink(path, &buf[0], 256);
	if (s == -1)
	{
		return "";
	}
	return string(buf);
}

vector<int32_t> FindAllFDForFile(string path)
{
	int32_t numHandles = getdtablesize();

	vector<int32_t> FDs;

	for (int32_t i = 0; i < numHandles; i++)
	{
		int32_t fd_flags = fcntl(i, F_GETFD);
		if (fd_flags == -1) continue;

		string ret = GetFDname(i);
		if (path.compare(ret) == 0)
		{
			FDs.push_back(i);
		}
	}

	return FDs;
}

void LoadInitialFD()
{
	p1 = GetFDname(1);
	p2 = GetFDname(2);
	p1FDs = FindAllFDForFile(p1);
	p2FDs = FindAllFDForFile(p2);
}

void LoadExtraFD()
{
	vector<int32_t> newP1FDs = FindAllFDForFile(p1);
	vector<int32_t> newP2FDs = FindAllFDForFile(p2);

	if (newP1FDs.size() > p1FDs.size())
	{
		PSELog.Write("%s has %d extra open handle(s)\n", p1.c_str(), newP1FDs.size() - p1FDs.size());
		vector<int32_t> excessFDs;
		for (size_t x = 0; x < newP1FDs.size(); x++)
		{
			bool old = false;
			for (size_t y = 0; y < p1FDs.size(); y++)
			{
				if (newP1FDs[x] == p1FDs[y])
					old = true;
			}
			if (old == false)
				excessFDs.push_back(newP1FDs[x]);
		}
		p1ClrFDs = excessFDs;
		//for (size_t x = 0; x < excessFDs.size(); x++)
		//{
		//	PSELog.Write("Closing %d\n", excessFDs[x]);
		//	close(excessFDs[x]);
		//}
	}

	if (newP2FDs.size() > p2FDs.size())
	{
		PSELog.Write("%s has %d extra open handle(s)\n", p2.c_str(), newP2FDs.size() - p2FDs.size());
		vector<int32_t> excessFDs;
		for (size_t x = 0; x < newP2FDs.size(); x++)
		{
			bool old = false;
			for (size_t y = 0; y < p2FDs.size(); y++)
			{
				if (newP2FDs[x] == p2FDs[y])
					old = true;
			}
			if (old == false)
				excessFDs.push_back(newP2FDs[x]);
		}
		p2ClrFDs = excessFDs;
		//for (size_t x = 0; x < excessFDs.size(); x++)
		//{
		//	PSELog.Write("Closing %d\n", excessFDs[x]);
		//	close(excessFDs[x]);
		//}
	}
}

void CloseCLRFD()
{
	for (size_t x = 0; x < p1ClrFDs.size(); x++)
	{
		int32_t fd_flags = fcntl(p1ClrFDs[x], F_GETFD);
		if (fd_flags == -1) continue;
		PSELog.Write("%d is still open, closing\n", p1ClrFDs[x]);
		close(p1ClrFDs[x]);
	}
	for (size_t x = 0; x < p2ClrFDs.size(); x++)
	{
		int32_t fd_flags = fcntl(p2ClrFDs[x], F_GETFD);
		if (fd_flags == -1) continue;
		PSELog.Write("%d is still open, closing\n", p2ClrFDs[x]);
		close(p2ClrFDs[x]);
	}
}
//end HACKFIX (The PipeCleaner)

void LoadCoreCLR(string pluginPath, string monoUsrLibFolder, string monoEtcFolder)
{
	PSELog.WriteLn("Init Mono Runtime");
	//PSELog.Write("PP: %s\n", pluginPath.c_str());
	//PSELog.Write("CF: %s\n", coreClrFolder.c_str());

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

		LoadInitialFD();

		PSELog.WriteLn("Set Dirs");

		if (monoUsrLibFolder.length() == 0)
		{
			monoUsrLibFolder = "/usr/lib/";
		}
		if (monoEtcFolder.length() == 0)
		{
			monoEtcFolder = "/etc/";
		}

		mono_set_dirs(monoUsrLibFolder.c_str(), monoEtcFolder.c_str());
		mono_config_parse(NULL);
		mono_set_signal_chaining(true);

		PSELog.Write("Set Debug (if only)\n");
		mono_debug_init(MONO_DEBUG_FORMAT_MONO);

		PSELog.Write("JIT Init\n");
		pseDomain = mono_jit_init(pseDomainName.c_str());
		if (pseDomain == NULL)
		{
			PSELog.Write("Init Mono Failed At jit_init\n");
			return;
		}
		else
		{
			PSELog.WriteLn(mono_domain_get_friendly_name(pseDomain));
		}
	}
	else
	{
		PSELog.WriteLn("Mono Already Running");
		PSELog.WriteLn(mono_domain_get_friendly_name(pseDomain));
	}

	pluginDomain = mono_domain_create();

	//Now load the plugin
	string pluginFolder = pluginPath.substr(0, pluginPath.find_last_of("/")) + "/";

	PSELog.WriteLn("Load Assembly");
	pluginAssembly = mono_domain_assembly_open(pluginDomain, pluginPath.c_str());

	if (!pluginAssembly)
	{
		PSELog.WriteLn("Init Mono Failed At PluginAssembly");
		CloseCoreCLR();
		pluginDomain = NULL;
		return;
	}

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
		CloseCoreCLR();
		pluginDomain = NULL;
		pluginImage = NULL;
		return;
	}

	PSELog.WriteLn("Get PSE Main()");
	MonoMethod *main = mono_class_get_method_from_name(pseClass_mono, "Main", 0);

	PSELog.WriteLn("Run PSE Main()");

	char* argv[1];
	argv[0] = (char *)pluginPath.c_str();

	int32_t ret = mono_runtime_run_main(main, 1, argv, NULL);

	if (ret != 0)
	{
		CloseCoreCLR();
		return;
	}

	PSELog.WriteLn("Get helpers");

	MonoMethod *getDelegate = mono_class_get_method_from_name(pseClass_mono, "CyclesCallbackFromFunctionPointer", 1);
	CyclesCallbackFromFunctionPointer = (ThunkGetDelegate)mono_method_get_unmanaged_thunk(getDelegate);

	MonoMethod *getFucnPtr = mono_class_get_method_from_name(pseClass_mono, "FunctionPointerFromIRQHandler", 1);
	FunctionPointerFromIRQHandler = (void*(*)(MonoObject* func, MonoException** ex))mono_method_get_unmanaged_thunk(getFucnPtr);

	//PSELog.WriteLn("Get Signal Functions");

	////MonoAssemblyName* runtimeName = mono_assembly_name_new("Mono, Version=0.0.0.0, Culture=null, PublicKeyToken=null");
	//MonoAssemblyName* runtimeName = mono_assembly_name_new("Mono");
	//MonoImageOpenStatus status;
	//runtimeAssembly = mono_assembly_load(runtimeName, NULL, &status);

	//char pcsx2Path[PATH_MAX];

	//if (readlink("/proc/self/exe", pcsx2Path, PATH_MAX) == -1)
	//{
	//	PSELog.Write("Init CLR Failed At readlink\n");
	//	dlclose(coreClrLib);
	//	return /*NULL*/;
	//}

	//PSELog.WriteLn("PCSX2 Path is %s", pcsx2Path);
	PSELog.WriteLn("Init CLR Done");
}

void CloseCoreCLR()
{
	if (pluginImage != NULL)
	{
		mono_image_close(pluginImage);
		pluginImage = NULL;
	}
	if (pluginAssembly != NULL)
	{
		mono_assembly_close(pluginAssembly);
		pluginAssembly = NULL;
	}
	if (pluginDomain != NULL)
	{
		//mono_domain_unload(pluginDomain);
		//mono_domain_free(pluginDomain,false);
		pluginDomain = NULL;
	}

	LoadExtraFD();
	//CloseCLRFD();
}

//__attribute__((constructor)) int DLLOpen()
//{
//	return 0;
//}
