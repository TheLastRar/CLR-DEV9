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

string configDir;
string logDir;

MonoDomain *pluginDomain = NULL;
MonoAssembly *pluginAssembly;
MonoImage *pluginImage;
MonoClass *pluginClassPSE;
MonoClass *pluginClassPSE_Mono;
MonoClass *pluginClassDEV9;

string GetParentDirectory(const string& str)
{
	size_t found;
	found = str.find_last_of("/\\");
	return str.substr(0, found);
}

int32_t LoadAssembly()
{
	if (pluginDomain != NULL)
	{
		return 0;
	}

	PSELog.WriteLn("Init Mono");
	pluginDomain = InitMonoSafer(monoLibPath, monoEtcPath);

	PSELog.WriteLn("Load Assemblies");
	pluginAssembly = mono_domain_assembly_open(pluginDomain, pluginPath.c_str());

	if (!pluginAssembly)
	{
		PSELog.WriteLn("Init Mono Failed At PluginAssembly");
		return -1;
	}

	string mainPath = GetParentDirectory(pluginPath) + "/DummyMain.exe";

	MonoAssembly *dummyAssembly = mono_domain_assembly_open(pluginDomain, mainPath.c_str());

	if (!dummyAssembly)
	{
		PSELog.WriteLn("Init Mono Failed At DummyAssembly");
		return -1;
	}

	char* argv[1];
	argv[0] = (char *)mainPath.c_str();

	PSELog.WriteLn("jit_exec");

	int32_t ret = mono_jit_exec(pluginDomain, dummyAssembly, 1, argv);

	if (ret != 0)
	{
		PSELog.WriteLn("Init Mono Failed At jit_exec");
		return ret;
	}

	PSELog.WriteLn("Get Plugin Image");
	pluginImage = mono_assembly_get_image(pluginAssembly);

	if (!dummyAssembly)
	{
		PSELog.WriteLn("Init Mono Failed At get_image");
		return -1;
	}

	PSELog.WriteLn("Get Classes");
	pluginClassPSE = mono_class_from_name(pluginImage, "PSE", "CLR_PSE");

	if (!pluginClassPSE)
	{
		PSELog.WriteLn("Init Mono Failed At Get CLR_PSE");
		return -1;
	}

	pluginClassPSE_Mono = mono_class_from_name(pluginImage, "PSE", "CLR_PSE_Mono");

	if (!pluginClassPSE_Mono)
	{
		PSELog.WriteLn("Init Mono Failed At Get CLR_PSE_Mono");
		return -1;
	}

	pluginClassDEV9 = mono_class_from_name(pluginImage, "PSE", "CLR_PSE_DEV9");

	if (!pluginClassDEV9)
	{
		PSELog.WriteLn("Init Mono Failed At Get CLR_PSE_DEV9");
		return -1;
	}

	if ((mono_class_init(pluginClassPSE) & mono_class_init(pluginClassDEV9)) == false)
	{
		PSELog.WriteLn("Classes Failed To Init");
		return -1;
	}

	PSELog.WriteLn("Get Methods");
	//Load Methods
	MonoMethod *meth;
	//
	mono_image_addref(pluginImage);
	mono_gchandle_new(mono_object_new(pluginDomain,pluginClassDEV9),false);

	//init
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9init", 0);
	managedInit = (ThunkInit)mono_method_get_unmanaged_thunk(meth);
	//open
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9open", 1);
	managedOpen = (ThunkOpen)mono_method_get_unmanaged_thunk(meth);
	//close
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9close", 0);
	managedClose = (ThunkClose)mono_method_get_unmanaged_thunk(meth);
	//shutdown
	meth = mono_class_get_method_from_name(pluginClassDEV9, "DEV9shutdown", 0);
	managedClose = (ThunkShutdown)mono_method_get_unmanaged_thunk(meth);
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
	mono_thread_attach(mono_get_root_domain());
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
}

EXPORT_C_(void)
DEV9setSettingsDir(const char* dir)
{
	PSELog.Write("SetSetting\n");
	string configName = "CLR_DEV9_lx.ini";
	string pluginName = "CLR_DEV9.dll";

	configDir = dir;
	string configPath = configDir + configName;

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
		pluginPath.append(pluginName);

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
//log + settings dir

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
	//MonoException* ex;
	//managedReadDMA8(memPointer, size, &ex);

	//if (ex)
	//	throw;
}

EXPORT_C_(void)
DEV9writeDMA8Mem(uint8_t* memPointer, int32_t size)
{
	//MonoException* ex;
	//managedWriteDMA8(memPointer, size, &ex);

	//if (ex)
	//	throw;
}

EXPORT_C_(void)
Dev9async(uint32_t cycles)
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

	PSELog.WriteLn("GetHelper");
	MonoMethod *getDelegate = mono_class_get_method_from_name(pluginClassPSE_Mono, "CyclesCallbackFromFunctionPointer", 1);

	MonoObject*(*getDelegateThunk)(void* func, MonoException** ex);

	getDelegateThunk = (MonoObject*(*)(void* func, MonoException** ex))mono_method_get_unmanaged_thunk(getDelegate);

	PSELog.WriteLn("Call Helper");
	MonoObject *ret = getDelegateThunk((void*)DEV9callback, &ex);

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

	PSELog.WriteLn("GetHelper");
	MonoMethod *getFucnPtr = mono_class_get_method_from_name(pluginClassPSE_Mono, "FunctionPointerFromIRQHandler", 1);

	void*(*getFucnPtrThunk)(MonoObject* func, MonoException** ex);

	getFucnPtrThunk = (void*(*)(MonoObject* func, MonoException** ex))mono_method_get_unmanaged_thunk(getFucnPtr);

	PSELog.WriteLn("Call Helper");
	void *retPtr = getFucnPtrThunk(ret, &ex);

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
	PSELog.Write("Configure\n");
}