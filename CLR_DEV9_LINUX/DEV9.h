#pragma once

#include "PSE.h"

#include <iostream>
#include <fstream>

typedef uint32_t(*ThunkInit)(MonoException** ex);
static ThunkInit managedInit;

typedef uint32_t(*ThunkOpen)(void* pDsp, MonoException** ex);
static ThunkOpen managedOpen;

typedef void(*ThunkVoid)(MonoException** ex);
static ThunkVoid managedClose;

static ThunkVoid managedShutdown;

//set dirs

typedef uint8_t(*ThunkRead8)(uint32_t addr, MonoException** ex);
static ThunkRead8 managedRead8;
typedef uint16_t(*ThunkRead16)(uint32_t addr, MonoException** ex);
static ThunkRead16 managedRead16;
typedef uint32_t(*ThunkRead32)(uint32_t addr, MonoException** ex);
static ThunkRead32 managedRead32;

typedef void(*ThunkWrite8)(uint32_t addr, uint8_t value, MonoException** ex);
static ThunkWrite8 managedWrite8;
typedef void(*ThunkWrite16)(uint32_t addr, uint16_t value, MonoException** ex);
static ThunkWrite16 managedWrite16;
typedef void(*ThunkWrite32)(uint32_t addr, uint32_t value, MonoException** ex);
static ThunkWrite32 managedWrite32;

typedef void(*ThunkDMA8)(uint8_t *memPointer, int32_t size, MonoException** ex);
static ThunkDMA8 managedReadDMA8;
static ThunkDMA8 managedWriteDMA8;

typedef void(*ThunkAsync)(uint32_t cycles, MonoException** ex);
static ThunkAsync managedAsync;

typedef void(*ThunkIrqCallback)(MonoObject *DEV9callback, MonoException** ex);
static ThunkIrqCallback managedIrqCallback;

typedef MonoObject*(*ThunkIrqHandler)(MonoException** ex);
static ThunkIrqHandler managedIrqHandler;

static ThunkVoid managedConfig;