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
void* runtimeCLR = NULL;
unsigned int pseDomainID;

coreclr_create_delegate_ptr createDelegate;

PluginLog PSELog;

//Private
const string pseDomainName = "PSE_Mono";

coreclr_initialize_ptr initializeCoreCLR;
coreclr_execute_assembly_ptr executeAssembly;
coreclr_shutdown_ptr shutdownCoreCLR;
coreclr_shutdown_2_ptr shutdownCoreCLR2;

void* coreClrLib;

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
	LoadCoreCLR("/home/air/.config/PCSX2/inis_1.4.0/CLR_DEV9_CORE.dll", "");
	CloseCoreCLR();
}

//Taken from https://github.com/dotnet/coreclr/blob/master/src/coreclr/hosts/unixcoreruncommon/coreruncommon.cpp
void AddFilesFromDirectoryToTpaList(string directory, string& tpaList)
{
	const char* const tpaExtensions[] = {
		".ni.dll",      // Probe for .ni.dll first so that it's preferred if ni and il coexist in the same dir
		".dll",
		".ni.exe",
		".exe",
	};

	DIR* dir = opendir(directory.c_str());
	if (dir == nullptr)
	{
		return;
	}

	std::set<std::string> addedAssemblies;

	// Walk the directory for each extension separately so that we first get files with .ni.dll extension,
	// then files with .dll extension, etc.
	for (size_t extIndex = 0; extIndex < sizeof(tpaExtensions) / sizeof(tpaExtensions[0]); extIndex++)
	{
		const char* ext = tpaExtensions[extIndex];
		size_t extLength = strlen(ext);

		struct dirent* entry;

		// For all entries in the directory
		while ((entry = readdir(dir)) != nullptr)
		{
			// We are interested in files only
			switch (entry->d_type)
			{
			case DT_REG:
				break;

				// Handle symlinks and file systems that do not support d_type
			case DT_LNK:
			case DT_UNKNOWN:
			{
				string fullFilename;

				fullFilename.append(directory);
				fullFilename.append("/");
				fullFilename.append(entry->d_name);

				struct stat sb;
				if (stat(fullFilename.c_str(), &sb) == -1)
				{
					continue;
				}

				if (!S_ISREG(sb.st_mode))
				{
					continue;
				}
			}
			break;

			default:
				continue;
			}

			string filename(entry->d_name);

			// Check if the extension matches the one we are looking for
			int extPos = filename.length() - extLength;
			if ((extPos <= 0) || (filename.compare(extPos, extLength, ext) != 0))
			{
				continue;
			}

			string filenameWithoutExt(filename.substr(0, extPos));

			// Make sure if we have an assembly with multiple extensions present,
			// we insert only one version of it.
			if (addedAssemblies.find(filenameWithoutExt) == addedAssemblies.end())
			{
				addedAssemblies.insert(filenameWithoutExt);

				tpaList.append(directory);
				tpaList.append("/");
				tpaList.append(filename);
				tpaList.append(":");
			}
		}

		// Rewind the directory stream to be able to iterate over it for the next extension
		rewinddir(dir);
	}

	closedir(dir);
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

void LoadCoreCLR(string pluginPath, string coreClrFolder)
{
	PSELog.WriteLn("Init CLR Runtime");
	//PSELog.Write("PP: %s\n", pluginPath.c_str());
	//PSELog.Write("CF: %s\n", coreClrFolder.c_str());

	if (runtimeCLR != NULL)
	{
		return;
	}

	LoadInitialFD();

	PSELog.WriteLn("Set Dirs");

	if (coreClrFolder.length() == 0)
	{
		//coreClrFolder = "/home/air/git/ReadyBin.Release";
		coreClrFolder = "/home/air/git/ReadyBin.Debug";
	}

	string coreClrPath = coreClrFolder + "/" + "libcoreclr.so";

	string pluginFolder = pluginPath.substr(0, pluginPath.find_last_of("/")) + "/";

	//TPA list
	string tpaList;
	//Add own Assembly?
	//Add native search directory paths (for corefx)?
	//Add CLR files
	AddFilesFromDirectoryToTpaList(coreClrFolder, tpaList);

	//NativeDllPath
	string nativeDllFolder = "";
	//Add native search directory paths (for corefx)?
	//Add CLR files
	nativeDllFolder.append(coreClrFolder);
	//Other paths?

	//Start loading
	coreClrLib = dlopen(coreClrPath.c_str(), RTLD_NOW | RTLD_LOCAL);
	if (coreClrLib == NULL)
	{
		PSELog.WriteLn("Init CLR Failed At dlopen: %s", dlerror());
		return /*NULL*/;
	}

	//Get req functions
	initializeCoreCLR = (coreclr_initialize_ptr)dlsym(coreClrLib, "coreclr_initialize");
	executeAssembly = (coreclr_execute_assembly_ptr)dlsym(coreClrLib, "coreclr_execute_assembly");
	shutdownCoreCLR2 = (coreclr_shutdown_2_ptr)dlsym(coreClrLib, "coreclr_shutdown_2");
	shutdownCoreCLR = (coreclr_shutdown_ptr)dlsym(coreClrLib, "coreclr_shutdown");
	createDelegate = (coreclr_create_delegate_ptr)dlsym(coreClrLib, "coreclr_create_delegate");

	if ((initializeCoreCLR == NULL) | (executeAssembly == NULL) | (shutdownCoreCLR == NULL) | (createDelegate == NULL))
	{
		PSELog.WriteLn("Init CLR Failed At dlsym");
		dlclose(coreClrLib);
		return /*NULL*/;
	}

	//propertyKeys
	const char *propertyKeys[] = {
		"TRUSTED_PLATFORM_ASSEMBLIES",	//framework assemblies
		"APP_PATHS",					//app managed lib path
		"APP_NI_PATHS",					//app ngen lib path
		"NATIVE_DLL_SEARCH_DIRECTORIES",//native dll path (for PInvoke)
		"System.GC.Server",
		"System.Globalization.Invariant",
	};
	const char *propertyValues[] = {
		// TRUSTED_PLATFORM_ASSEMBLIES
		tpaList.c_str(),
		// APP_PATHS
		pluginFolder.c_str(),
		// APP_NI_PATHS
		pluginFolder.c_str(),
		//pluginFolder.c_str(),
		// NATIVE_DLL_SEARCH_DIRECTORIES
		nativeDllFolder.c_str(),
		// System.GC.Server //Should get from Env Values
		"false",
		// System.Globalization.Invariant //Should get from Env Values
		"false",
	};

	char pcsx2Path[PATH_MAX];

	if (readlink("/proc/self/exe", pcsx2Path, PATH_MAX) == -1)
	{
		PSELog.Write("Init CLR Failed At readlink\n");
		dlclose(coreClrLib);
		return /*NULL*/;
	}

	PSELog.WriteLn("PCSX2 Path is %s", pcsx2Path);

	//Actully Init the CLR
	int st = initializeCoreCLR(
		pcsx2Path,
		pseDomainName.c_str(),
		sizeof(propertyKeys) / sizeof(propertyKeys[0]),
		propertyKeys,
		propertyValues,
		&runtimeCLR,
		&pseDomainID);

	if (st < 0)
	{
		PSELog.WriteLn("Init CLR Failed At coreclr_initialize - Status: 0x%08x", st);
		runtimeCLR = NULL;
		dlclose(coreClrLib);
		return /*NULL*/;
	}

	PSELog.WriteLn("Init CLR Done");
}

void CloseCoreCLR()
{
	if (runtimeCLR != NULL)
	{
		//int eCode;
		//shutdownCoreCLR2(runtimeCLR, pseDomainID, &eCode);
		shutdownCoreCLR(runtimeCLR, pseDomainID);
		runtimeCLR = NULL;
		dlclose(coreClrLib);

		LoadExtraFD();
		//CloseCLRFD();
	}
}

//__attribute__((constructor)) int DLLOpen()
//{
//	return 0;
//}
