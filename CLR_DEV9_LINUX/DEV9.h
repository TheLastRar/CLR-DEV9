#pragma once

#include "PSE.h"

#include <iostream>
#include <fstream>

typedef uint32_t(*ThunkInit)();
ThunkInit managedInit;

typedef uint32_t(*ThunkOpen)(void* pDsp);
ThunkOpen managedOpen;

typedef void(*ThunkVoid)();
ThunkVoid managedClose;
ThunkVoid managedShutdown;

typedef void(*ThunkSetDir)(char*);
ThunkSetDir managedSetSetDir;
ThunkSetDir managedSetLogDir;

//set dirs

typedef uint8_t(*ThunkRead8)(uint32_t addr);
ThunkRead8 managedRead8;
typedef uint16_t(*ThunkRead16)(uint32_t addr);
ThunkRead16 managedRead16;
typedef uint32_t(*ThunkRead32)(uint32_t addr);
ThunkRead32 managedRead32;

typedef void(*ThunkWrite8)(uint32_t addr, uint8_t value);
ThunkWrite8 managedWrite8;
typedef void(*ThunkWrite16)(uint32_t addr, uint16_t value);
ThunkWrite16 managedWrite16;
typedef void(*ThunkWrite32)(uint32_t addr, uint32_t value);
ThunkWrite32 managedWrite32;

typedef void(*ThunkDMA8)(uint8_t *memPointer, int32_t size);
ThunkDMA8 managedReadDMA8;
ThunkDMA8 managedWriteDMA8;

typedef void(*ThunkAsync)(uint32_t cycles);
ThunkAsync managedAsync;

typedef void(*ThunkIrqCallback)(void*);
ThunkIrqCallback managedIrqCallback;

typedef void*(*ThunkIrqHandler)();
ThunkIrqHandler managedIrqHandler;

ThunkVoid managedConfig;