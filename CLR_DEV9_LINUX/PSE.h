#pragma once

//#include <stdlib.h>
#include <stdio.h>
#include <stdarg.h> 
#include <stdint.h>
#include <string.h>

#include <string>

#include "coreclrhost.h"

#define EXPORT_C_(type) extern "C" __attribute__((stdcall, externally_visible, visibility("default"))) type
#define CALLBACK __attribute__((stdcall))

//Runtime
extern void* runtimeCLR;
extern unsigned int pseDomainID;

//set by specific plugin
extern const std::string pluginName;
extern const uint32_t pluginType;
extern const uint8_t pluginVerMajor;
extern const uint8_t pluginVerMinor;
extern const uint8_t pluginVerPatch;
extern std::string PCSX2HomeDir;

//helper methods
extern coreclr_create_delegate_ptr createDelegate;

//temp logging code
struct PluginLog
{
	void Write(const char *fmt, ...)
	{
		va_list list;

		va_start(list, fmt);
		vfprintf(stderr, fmt, list);
		va_end(list);
	}

	void WriteLn(const char *fmt, ...)
	{
		va_list list;

		va_start(list, fmt);
		vfprintf(stderr, fmt, list);
		va_end(list);

		fprintf(stderr, "\n");
	}
};
extern PluginLog PSELog;

void LoadCoreCLR(std::string pluginPath, std::string coreClrFolder);
void CloseCoreCLR();
