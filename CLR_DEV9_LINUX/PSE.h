#pragma once

#include <stdlib.h>
#include <stdio.h>
#include <stdarg.h> 
#include <stdint.h>
#include <string.h>

#include <string>

#include <mono/jit/jit.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/reflection.h>

#define EXPORT_C_(type) extern "C" __attribute__((stdcall, externally_visible, visibility("default"))) type
#define CALLBACK __attribute__((stdcall))

//set by specific plugin
extern const std::string pluginName;
extern const uint32_t pluginType;
extern const uint8_t pluginVerMajor;
extern const uint8_t pluginVerMinor;
extern const uint8_t pluginVerPatch;

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

MonoDomain *InitMonoSafer(std::string monousrlibPath, std::string monoetcPath);
