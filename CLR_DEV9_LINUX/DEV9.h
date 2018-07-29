#pragma once

#include "PSE.h"

#include <iostream>
#include <fstream>

typedef uint32_t(*ThunkInit)();
ThunkInit managedInit = nullptr;

typedef uint32_t(*ThunkOpen)(void* pDsp);
ThunkOpen managedOpen = nullptr;

typedef void(*ThunkVoid)();
ThunkVoid managedClose = nullptr;
ThunkVoid managedShutdown = nullptr;

typedef void(*ThunkSetDir)(char*);
ThunkSetDir managedSetSetDir = nullptr;
ThunkSetDir managedSetLogDir = nullptr;

//set dirs

typedef uint8_t(*ThunkRead8)(uint32_t addr);
ThunkRead8 managedRead8 = nullptr;
typedef uint16_t(*ThunkRead16)(uint32_t addr);
ThunkRead16 managedRead16 = nullptr;
typedef uint32_t(*ThunkRead32)(uint32_t addr);
ThunkRead32 managedRead32 = nullptr;

typedef void(*ThunkWrite8)(uint32_t addr, uint8_t value);
ThunkWrite8 managedWrite8 = nullptr;
typedef void(*ThunkWrite16)(uint32_t addr, uint16_t value);
ThunkWrite16 managedWrite16 = nullptr;
typedef void(*ThunkWrite32)(uint32_t addr, uint32_t value);
ThunkWrite32 managedWrite32 = nullptr;

typedef void(*ThunkDMA8)(uint8_t *memPointer, int32_t size);
ThunkDMA8 managedReadDMA8 = nullptr;
ThunkDMA8 managedWriteDMA8 = nullptr;

typedef void(*ThunkAsync)(uint32_t cycles);
ThunkAsync managedAsync = nullptr;

typedef void(*ThunkIrqCallback)(void*);
ThunkIrqCallback managedIrqCallback = nullptr;

typedef void*(*ThunkIrqHandler)();
ThunkIrqHandler managedIrqHandler = nullptr;

ThunkVoid managedConfig = nullptr;