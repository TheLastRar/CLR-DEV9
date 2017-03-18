#include "DEV9.h"

using namespace std;

const string pluginName = "CLR_DEV9_lx_wrapper";
const uint32_t pluginType = 0x10;
const uint8_t pluginVerMajor = 0;
const uint8_t pluginVerMinor = 0;
const uint8_t pluginVerPatch = 0;

string pluginPath;
string monoLibPath;
string monoEtcPath;

string configFileName = "CLR_DEV9_lx.ini";
string pluginFileName = "CLR_DEV9.dll";

string configDir;
string logDir;

MonoDomain *pluginDomain = NULL;
MonoAssembly *pluginAssembly = NULL;
MonoImage *pluginImage = NULL;
MonoClass *pluginClassDEV9 = NULL;

int32_t LoadAssembly()
{
	PSELog.WriteLn("Init Mono");

	if (pluginDomain != NULL)
	{
		return 0;
	}

	pluginDomain = LoadMonoSafer(monoLibPath, monoEtcPath);

	PSELog.WriteLn("Load Assemblies");
	pluginAssembly = mono_domain_assembly_open(pluginDomain, pluginPath.c_str());

	if (!pluginAssembly)
	{
		PSELog.WriteLn("Init Mono Failed At PluginAssembly");
		mono_domain_free(pluginDomain,false);
		pluginDomain = NULL;
		return -1;
	}

	PSELog.WriteLn("Load Plugin Image");
	pluginImage = LoadPluginPSE(pluginAssembly, pluginPath.c_str());

	if (!pluginImage)
	{
		PSELog.WriteLn("Load Plugin Image Failed");
		mono_domain_free(pluginDomain, false);
		pluginDomain = NULL;
		return -1;
	}

	PSELog.WriteLn("Get DEV9 Class");
	pluginClassDEV9 = mono_class_from_name(pluginImage, "PSE", "CLR_PSE_DEV9");

	if (!pluginClassDEV9)
	{
		PSELog.WriteLn("Init Mono Failed At Get CLR_PSE_DEV9");
		mono_image_close(pluginImage);
		pluginImage = NULL;
		mono_domain_free(pluginDomain, false);
		pluginDomain = NULL;
		return -1;
	}

	if (mono_class_init(pluginClassDEV9) == false)
	{
		PSELog.WriteLn("Classes Failed To Init");
		mono_image_close(pluginImage);
		pluginImage = NULL;
		mono_domain_free(pluginDomain, false);
		pluginDomain = NULL;
		return -1;
	}

	PSELog.WriteLn("Get Methods");
	//Load Methods
	MonoMethod *meth;

	//init
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9init", 0);
	managedInit = (ThunkInit)mono_method_get_unmanaged_thunk(meth);
	//open
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9open", 1);
	managedOpen = (ThunkOpen)mono_method_get_unmanaged_thunk(meth);
	//close
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9close", 0);
	managedClose = (ThunkVoid)mono_method_get_unmanaged_thunk(meth);
	//shutdown
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9shutdown", 0);
	managedClose = (ThunkVoid)mono_method_get_unmanaged_thunk(meth);
	//set log + config
	//read
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9read8", 1);
	managedRead8 = (ThunkRead8)mono_method_get_unmanaged_thunk(meth);
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9read16", 1);
	managedRead16 = (ThunkRead16)mono_method_get_unmanaged_thunk(meth);
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9read32", 1);
	managedRead32 = (ThunkRead32)mono_method_get_unmanaged_thunk(meth);
	//write
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9write8", 2);
	managedWrite8 = (ThunkWrite8)mono_method_get_unmanaged_thunk(meth);
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9write16", 2);
	managedWrite16 = (ThunkWrite16)mono_method_get_unmanaged_thunk(meth);
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9write32", 2);
	managedWrite32 = (ThunkWrite32)mono_method_get_unmanaged_thunk(meth);
	//DMA8
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9readDMA8Mem", 2);
	managedReadDMA8 = (ThunkDMA8)mono_method_get_unmanaged_thunk(meth);
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9writeDMA8Mem", 2);
	managedWriteDMA8 = (ThunkDMA8)mono_method_get_unmanaged_thunk(meth);
	//async
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9async", 1);
	managedAsync = (ThunkAsync)mono_method_get_unmanaged_thunk(meth);
	//irq
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9irqCallback", 1);
	managedIrqCallback = (ThunkIrqCallback)mono_method_get_unmanaged_thunk(meth);
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9irqHandler", 0);
	managedIrqHandler = (ThunkIrqHandler)mono_method_get_unmanaged_thunk(meth);

	//config
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9configure", 0);
	managedConfig = (ThunkVoid)mono_method_get_unmanaged_thunk(meth);
	return 0;
}

EXPORT_C_(int32_t)
DEV9init(void)
{
	int32_t ret = LoadAssembly();
	mono_thread_attach(mono_get_root_domain());
	if (ret == 0)
	{
		PSELog.WriteLn("Loaded Plugin");

		MonoMethod *config = mono_class_get_method_from_name(pluginClassDEV9, "DEV9setSettingsDir", 1);
		MonoMethod *log = mono_class_get_method_from_name(pluginClassDEV9, "DEV9setLogDir", 1);

		MonoString *monoConfigPath = mono_string_new(pluginDomain, configDir.c_str());
		MonoString *monoLogPath = mono_string_new(pluginDomain, logDir.c_str());

		MonoString *args[1];

		args[0] = monoConfigPath;
		mono_runtime_invoke(config, NULL, (void**)args, NULL);

		args[0] = monoLogPath;
		mono_runtime_invoke(log, NULL, (void**)args, NULL);

		MonoException* ex;

		int32_t ret = managedInit(&ex);

		if (ex)
		{
			PSELog.WriteLn("InnitError");
			return -1;
		}

		return ret;
	}
	return -1;
}

EXPORT_C_(int32_t)
DEV9open(void* pDsp)
{
	mono_thread_attach(mono_get_root_domain());
	MonoException* ex;

	int32_t ret = managedOpen(pDsp, &ex);

	if (ex)
	{
		PSELog.WriteLn("OpenError");
		return -1;
	}

	return ret;
}

EXPORT_C_(void)
DEV9close()
{
	//mono_thread_attach(mono_get_root_domain());
	MonoException* ex;
	managedClose(&ex);

	if (ex)
	{
		PSELog.WriteLn("CloseError");
		throw;
	}
}

EXPORT_C_(void)
DEV9shutdown()
{
	MonoException* ex;
	managedShutdown(&ex);

	if (ex)
		throw;

	//Cleanup refrences
	if (pluginImage != NULL)
	{
		mono_image_close(pluginImage);
		pluginImage = NULL;
	}
	if (pluginDomain != NULL)
	{
		mono_domain_free(pluginDomain, false);
		pluginDomain = NULL;
	}
}

EXPORT_C_(void)
DEV9setSettingsDir(const char* dir)
{
	PSELog.Write("SetSetting\n");

	configDir = dir;
	string configPath = configDir + configFileName;

	ifstream reader;
	reader.open(configPath, ios::in);

	if (reader.is_open())
	{
		getline(reader, pluginPath);
		if (!reader.eof())
		{
			getline(reader, monoLibPath);
		}
		if (!reader.eof())
		{
			getline(reader, monoEtcPath);
		}
		reader.close();
	}
	else
	{
		pluginPath.append(configDir);
		pluginPath.append(pluginFileName);

		ofstream writer;
		writer.open(configPath, ios::out | ios::trunc);
		writer.write(pluginPath.c_str(), pluginPath.length());
		writer.close();
	}

	PSELog.WriteLn(pluginPath.c_str());
	PSELog.WriteLn(monoLibPath.c_str());
	PSELog.WriteLn(monoEtcPath.c_str());
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
	//mono_thread_attach(mono_get_root_domain());
	MonoException* ex;
	uint8_t ret = managedRead8(addr, &ex);

	if (ex)
		throw;

	return ret;
}

EXPORT_C_(uint16_t)
DEV9read16(uint32_t addr)
{
	//mono_thread_attach(mono_get_root_domain());
	MonoException* ex;
	uint16_t ret = managedRead16(addr, &ex);

	if (ex)
		throw;

	return ret;
}

EXPORT_C_(uint32_t)
DEV9read32(uint32_t addr)
{
	//mono_thread_attach(mono_get_root_domain());
	MonoException* ex;
	uint32_t ret = managedRead32(addr, &ex);

	if (ex)
		throw;

	return  ret;
}

EXPORT_C_(void)
DEV9write8(uint32_t addr, uint8_t value)
{
	//mono_thread_attach(mono_get_root_domain());
	MonoException* ex;
	managedWrite8(addr, value, &ex);

	if (ex)
		throw;
}

EXPORT_C_(void)
DEV9write16(uint32_t addr, uint16_t value)
{
	//mono_thread_attach(mono_get_root_domain());
	MonoException* ex;
	managedWrite16(addr, value, &ex);

	if (ex)
		throw;
}

EXPORT_C_(void)
DEV9write32(uint32_t addr, uint32_t value)
{
	//mono_thread_attach(mono_get_root_domain());
	MonoException* ex;
	managedWrite32(addr, value, &ex);

	if (ex)
		throw;
}

EXPORT_C_(void)
DEV9readDMA8Mem(uint8_t* memPointer, int32_t size)
{
	MonoException* ex;
	managedReadDMA8(memPointer, size, &ex);

	if (ex)
		throw;
}

EXPORT_C_(void)
DEV9writeDMA8Mem(uint8_t* memPointer, int32_t size)
{
	MonoException* ex;
	managedWriteDMA8(memPointer, size, &ex);

	if (ex)
		throw;
}

EXPORT_C_(void)
DEV9async(uint32_t cycles)
{
	//mono_thread_attach(mono_get_root_domain());
	MonoException* ex;
	managedAsync(cycles, &ex);

	if (ex)
		throw;
}

EXPORT_C_(void)
DEV9irqCallback(void* DEV9callback)
{
	//mono_thread_attach(mono_get_root_domain());
	PSELog.WriteLn("SetCallback");
	MonoException* ex;

	PSELog.WriteLn("Call Helper");
	MonoObject *ret = CyclesCallbackFromFunctionPointer((void*)DEV9callback, &ex);

	if (ex)
		throw;

	PSELog.WriteLn("SetIRQ");
	managedIrqCallback(ret, &ex);

	if (ex)
		throw;
}

EXPORT_C_(void*)
DEV9irqHandler()
{
	//mono_thread_attach(mono_get_root_domain());
	PSELog.WriteLn("GetHandler");
	MonoException* ex;

	PSELog.WriteLn("GetIRQ");
	MonoObject *ret = managedIrqHandler(&ex);

	if (ex)
		throw;

	PSELog.WriteLn("Call Helper");
	void *retPtr = FunctionPointerFromIRQHandler(ret, &ex);

	if (ex)
		throw;

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
	bool preInit = false;
	if (pluginDomain == NULL)
	{
		preInit = true;
		int32_t ret = LoadAssembly();
		
		if (ret != 0)
			throw;
	}
	mono_thread_attach(mono_get_root_domain());

	MonoException* ex;
	managedConfig(&ex);
	if (ex)
		throw;

	if (preInit)
	{
		//Cleanup refrences
		if (pluginImage != NULL)
		{
			mono_image_close(pluginImage);
			pluginImage = NULL;
		}
		if (pluginDomain != NULL)
		{
			mono_domain_free(pluginDomain, false);
			pluginDomain = NULL;
		}
	}
}