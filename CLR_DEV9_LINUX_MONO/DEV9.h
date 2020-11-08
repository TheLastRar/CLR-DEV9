#pragma once

#include "PSE.h"

#include <iostream>
#include <fstream>

extern char _binary_CLR_DEV9_dll_start;
extern char _binary_CLR_DEV9_dll_end;

//Is a char* because strings will init
//after constuctors are called
const char* config = "\
<configuration> \n\
	<dllmap dll=\"wpcap\" target=\"libpcap.so.0.8\" /> \n\
	<dllmap dll=\"libgtk-x11-2.0\" target=\"libgtk-3.so\" /> \n\
<configuration/> \n\
";

typedef uint32_t(*ThunkInit)(MonoException** ex);
ThunkInit managedInit;

typedef uint32_t(*ThunkOpen)(void* pDsp, MonoException** ex);
ThunkOpen managedOpen;

typedef void(*ThunkVoid)(MonoException** ex);
ThunkVoid managedClose;
ThunkVoid managedShutdown;

typedef void(*ThunkSetDir)(const char*, MonoException** ex);
ThunkSetDir managedSetSetDir;
ThunkSetDir managedSetLogDir;

//set dirs

typedef uint8_t(*ThunkRead8)(uint32_t addr, MonoException** ex);
ThunkRead8 managedRead8;
typedef uint16_t(*ThunkRead16)(uint32_t addr, MonoException** ex);
ThunkRead16 managedRead16;
typedef uint32_t(*ThunkRead32)(uint32_t addr, MonoException** ex);
ThunkRead32 managedRead32;

typedef void(*ThunkWrite8)(uint32_t addr, uint8_t value, MonoException** ex);
ThunkWrite8 managedWrite8;
typedef void(*ThunkWrite16)(uint32_t addr, uint16_t value, MonoException** ex);
ThunkWrite16 managedWrite16;
typedef void(*ThunkWrite32)(uint32_t addr, uint32_t value, MonoException** ex);
ThunkWrite32 managedWrite32;

typedef void(*ThunkDMA8)(uint8_t *memPointer, int32_t size, MonoException** ex);
ThunkDMA8 managedReadDMA8;
ThunkDMA8 managedWriteDMA8;

typedef void(*ThunkAsync)(uint32_t cycles, MonoException** ex);
ThunkAsync managedAsync;

typedef void(*ThunkIrqCallback)(MonoObject *DEV9callback, MonoException** ex);
ThunkIrqCallback managedIrqCallback;

typedef MonoObject*(*ThunkIrqHandler)(MonoException** ex);
ThunkIrqHandler managedIrqHandler;

ThunkInit managedFormTest;

ThunkVoid managedConfig;