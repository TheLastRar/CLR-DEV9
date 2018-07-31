#include "DEV9.h"

using namespace std;

const string pluginName = "CLR_DEV9_lx_wrapper";
const uint32_t pluginType = 0x10;
const uint8_t pluginVerMajor = 0;
const uint8_t pluginVerMinor = 0;
const uint8_t pluginVerPatch = 0;

//string pluginPath;
////string pluginLibPath;
//string coreclrPath;

string configFileName = "CLR_DEV9_lx.ini";
string pluginFileName = "CLR_DEV9_CORE.dll";
string pluginFileNameNoExt = "CLR_DEV9_CORE";
string pluginPSEType = "PSE.CLR_PSE_DEV9";

string configDir;
string logDir;
string PCSX2HomeDir;

//MonoDomain *pluginDomain = NULL;
//MonoAssembly *pluginAssembly = NULL;
//MonoImage *pluginImage = NULL;
//MonoClass *pluginClassDEV9 = NULL;

int32_t LoadAssembly()
{
	PSELog.WriteLn("Init CLR");

	if (runtimeCLR != NULL)
	{
		return 0;
	}

	//LoadCoreCLR(pluginPath, coreclrPath);
	LoadCoreCLR(configDir + "CLR_DEV9_CORE.dll", "");

	if (runtimeCLR == NULL)
	{
		//PSELog.WriteLn("Init CLR Failed");
		return -1;
	}

	PSELog.WriteLn("Get Methods");
	//Load Methods

	//init
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9init", (void**)&managedInit);
	//open
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9open", (void**)&managedOpen);
	//close
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9close", (void**)&managedClose);
	//shutdown
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9shutdown", (void**)&managedShutdown);
	//set log + config
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9setSettingsDir", (void**)&managedSetSetDir);
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9setLogDir", (void**)&managedSetLogDir);
	//read
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9read8", (void**)&managedRead8);
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9read16", (void**)&managedRead16);
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9read32", (void**)&managedRead32);
	//write
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9write8", (void**)&managedWrite8);
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9write16", (void**)&managedWrite16);
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9write32", (void**)&managedWrite32);
	//DMA8
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9readDMA8Mem", (void**)&managedReadDMA8);
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9writeDMA8Mem", (void**)&managedWriteDMA8);
	//async
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9async", (void**)&managedAsync);
	//irq
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9irqCallback", (void**)&managedIrqCallback);
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9irqHandler", (void**)&managedIrqHandler);
	//config
	createDelegate(runtimeCLR, pseDomainID, pluginFileNameNoExt.c_str(), pluginPSEType.c_str(), "DEV9configure", (void**)&managedConfig);

	//Debug prints
	PSELog.WriteLn("Address of DEV9init is %p", managedInit);
	//PSELog.WriteLn("Address of DEV9open is %p", managedOpen);
	//PSELog.WriteLn("Address of DEV9close is %p", managedClose);
	//PSELog.WriteLn("Address of DEV9shutdown is %p", managedShutdown);
	//PSELog.WriteLn("Address of DEV9setSettingsDir is %p", managedSetSetDir);
	//PSELog.WriteLn("Address of DEV9setLogDir is %p", managedSetLogDir);
	//PSELog.WriteLn("Address of DEV9read8 is %p", managedRead8);
	//PSELog.WriteLn("Address of DEV9read16 is %p", managedRead16);
	//PSELog.WriteLn("Address of DEV9read32 is %p", managedRead32);
	//PSELog.WriteLn("Address of DEV9write8 is %p", managedWrite8);
	//PSELog.WriteLn("Address of DEV9write16 is %p", managedWrite16);
	//PSELog.WriteLn("Address of DEV9write32 is %p", managedWrite32);
	//PSELog.WriteLn("Address of DEV9readDMA8Mem is %p", managedReadDMA8);
	//PSELog.WriteLn("Address of DEV9writeDMA8Mem is %p", managedWriteDMA8);
	//PSELog.WriteLn("Address of DEV9async is %p", managedAsync);
	//PSELog.WriteLn("Address of DEV9irqCallback is %p", managedIrqCallback);
	//PSELog.WriteLn("Address of DEV9irqHandler is %p", managedIrqHandler);
	//PSELog.WriteLn("Address of DEV9configure is %p", managedConfig);

	return 0;
}

EXPORT_C_(int32_t)
DEV9init(void)
{
	PSELog.WriteLn("Init Plugin");
	int32_t ret = LoadAssembly();

	if (ret == 0)
	{
		PSELog.WriteLn("Loaded Plugin");

		managedSetSetDir((char*)configDir.c_str());
		managedSetLogDir((char*)logDir.c_str());

		int32_t ret = managedInit();

		//if (ex)
		//{
		//	PSELog.WriteLn("InnitError");
		//	return -1;
		//}

		return ret;
	}
	return -1;
}

EXPORT_C_(int32_t)
DEV9open(void* pDsp)
{
	PSELog.WriteLn("Open Plugin");
	int32_t ret = managedOpen(pDsp);

	//if (ex)
	//{
	//	PSELog.WriteLn("OpenError");
	//	return -1;
	//}

	return ret;
}

EXPORT_C_(void)
DEV9close()
{
	managedClose();

	//if (ex)
	//{
	//	PSELog.WriteLn("CloseError");
	//	throw;
	//}
}

EXPORT_C_(void)
DEV9shutdown()
{
	managedShutdown();

	//Cleanup refrences
	CloseCoreCLR();

	//if (ex)
	//	throw;
}

//TODO
EXPORT_C_(void)
DEV9setSettingsDir(const char* dir)
{
	PSELog.Write("SetSetting\n");

	configDir = dir;
	PCSX2HomeDir = configDir.substr(0, configDir.substr(0, configDir.length()-1).find_last_of("/"));
	//string configPath = configDir + configFileName;

	//ifstream reader;
	//reader.open(configPath, ios::in);

	//if (reader.is_open())
	//{
	//	getline(reader, pluginPath);
	//	//if (!reader.eof())
	//	//{
	//	//	getline(reader, pluginLibPath);
	//	//}
	//	if (!reader.eof())
	//	{
	//		getline(reader, coreclrPath);
	//	}
	//	reader.close();
	//}
	//else
	//{
	//	pluginPath.append(configDir);
	//	pluginPath.append(pluginFileName);

	//	ofstream writer;
	//	writer.open(configPath, ios::out | ios::trunc);
	//	writer.write(pluginPath.c_str(), pluginPath.length());
	//	writer.close();
	//}

	//PSELog.WriteLn(pluginPath.c_str());
	//PSELog.WriteLn(coreclrPath.c_str());
	//read txt file in config dir with;
	//plugin path
	//mono lib path
	//mono config path

	//create default file if none exists
}

EXPORT_C_(void)
DEV9setLogDir(const char* dir)
{
	PSELog.Write("SetLog\n");
	logDir = dir;
}

EXPORT_C_(uint8_t)
DEV9read8(uint32_t addr)
{
	uint8_t ret = managedRead8(addr);

	//if (ex)
	//	throw;

	return ret;
}

EXPORT_C_(uint16_t)
DEV9read16(uint32_t addr)
{
	uint16_t ret = managedRead16(addr);

	//if (ex)
	//	throw;

	return ret;
}

EXPORT_C_(uint32_t)
DEV9read32(uint32_t addr)
{
	uint32_t ret = managedRead32(addr);

	//if (ex)
	//	throw;

	return  ret;
}

EXPORT_C_(void)
DEV9write8(uint32_t addr, uint8_t value)
{
	managedWrite8(addr, value);

	//if (ex)
	//	throw;
}

EXPORT_C_(void)
DEV9write16(uint32_t addr, uint16_t value)
{
	managedWrite16(addr, value);

	//if (ex)
	//	throw;
}

EXPORT_C_(void)
DEV9write32(uint32_t addr, uint32_t value)
{
	managedWrite32(addr, value);

	//if (ex)
	//	throw;
}

EXPORT_C_(void)
DEV9readDMA8Mem(uint8_t* memPointer, int32_t size)
{
	managedReadDMA8(memPointer, size);

	//if (ex)
	//	throw;
}

EXPORT_C_(void)
DEV9writeDMA8Mem(uint8_t* memPointer, int32_t size)
{
	managedWriteDMA8(memPointer, size);

	//if (ex)
	//	throw;
}

EXPORT_C_(void)
DEV9async(uint32_t cycles)
{
	managedAsync(cycles);

	//if (ex)
	//	throw;
}

EXPORT_C_(void)
DEV9irqCallback(void* DEV9callback)
{
	//mono_thread_attach(mono_get_root_domain());
	//PSELog.WriteLn("SetCallback");
	//MonoException* ex;

	//PSELog.WriteLn("Call Helper");
	//MonoObject *ret = CyclesCallbackFromFunctionPointer((void*)DEV9callback, &ex);

	//if (ex)
	//	throw;

	PSELog.WriteLn("SetIRQ");
	managedIrqCallback(DEV9callback);

	//if (ex)
	//	throw;
}

EXPORT_C_(void*)
DEV9irqHandler()
{
	//mono_thread_attach(mono_get_root_domain());
	//PSELog.WriteLn("GetHandler");
	//MonoException* ex;

	//PSELog.WriteLn("GetIRQ");
	//MonoObject *ret = managedIrqHandler(&ex);

	//if (ex)
	//	throw;

	//PSELog.WriteLn("Call Helper");
	//void *retPtr = FunctionPointerFromIRQHandler(ret, &ex);

	//if (ex)
	//	throw;

	PSELog.WriteLn("GetIRQ");
	void* retPtr = managedIrqHandler();

	return retPtr;
}

//freeze

//Test is done before SetConfig
//So we don't know where our
//plugin or mono is
//meaning I can't invoke
//the managed test function
//EXPORT_C_(int32_t)
//DEV9test()
//{
//	PSELog.Write("Test\n");
//  return 0;
//}


EXPORT_C_(void)
DEV9configure()
{
	//bool preInit = false;
	//if (runtimeCLR == NULL)
	//{
	//	preInit = true;
	//	int32_t ret = LoadAssembly();
	//	
	//	if (ret != 0)
	//		throw;
	//}

	//managedSetLogDir((char*)logDir.c_str());
	//managedSetSetDir((char*)configDir.c_str());
	//managedConfig();

	//if (preInit)
	//{
	//	//Cleanup refrences
	//	CloseCoreCLR();
	//}
}
