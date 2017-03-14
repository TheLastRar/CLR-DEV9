#include "PSE.h"

using namespace std;

const string pseDomainName = "PSE_Mono";

MonoDomain* pseDomain;

static char libraryName[256];

PluginLog PSELog;

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

//void CheckAppDomainsFunc(MonoDomain* domain, bool* isfound)
//{
//	if (strcmp(mono_domain_get_friendly_name(domain), pseDomainName) == 0)
//	{
//		*isfound = true;
//		pseDomain = domain;
//		PSELog.Write("Mono Already Init\n");
//	}
//}

MonoDomain* InitMonoSafer(string monousrlibPath, string monoetcPath)
{
	//bool isInit = false;
	//mono_domain_foreach((MonoDomainFunc)CheckAppDomainsFunc, &isInit);

	//if only this worked
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

		PSELog.Write("jit init\n");
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
	return mono_domain_create();
}


//__attribute__((constructor)) int DLLOpen()
//{
//	return 0;
//}
