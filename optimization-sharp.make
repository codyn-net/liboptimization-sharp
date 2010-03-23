

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/Optimization.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug

PROTOBUF_NET_DLL_SOURCE=protobuf-net.dll
OPTIMIZATION_SHARP_DLL_MDB_SOURCE=bin/Debug/Optimization.dll.mdb
OPTIMIZATION_SHARP_DLL_MDB=$(BUILD_DIR)/Optimization.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/Optimization.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release

PROTOBUF_NET_DLL_SOURCE=protobuf-net.dll
OPTIMIZATION_SHARP_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(PROTOBUF_NET_DLL) \
	$(OPTIMIZATION_SHARP_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(OPTIMIZATION_SHARP_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	Optimization/Application.cs \
	Optimization/AssemblyInfo.cs \
	Optimization/Attributes.cs \
	Optimization/Boundary.cs \
	Optimization/Connection.cs \
	Optimization/Constants.cs \
	Optimization/Directories.cs \
	Optimization/Fitness.cs \
	Optimization/Job.cs \
	Optimization/NumericSetting.cs \
	Optimization/Optimizer.cs \
	Optimization/Options.cs \
	Optimization/Parameter.cs \
	Optimization/Random.cs \
	Optimization/Registry.cs \
	Optimization/Settings.cs \
	Optimization/Solution.cs \
	Optimization/State.cs \
	Optimization/Visual.cs \
	Optimization.Math/Expression.cs \
	Optimization.Math/Instruction.cs \
	Optimization.Math/Operations.cs \
	Optimization.Math/Tokenizer.cs \
	Optimization.Math/Constants.cs \
	Optimization.Messages/Batch.cs \
	Optimization.Messages/Cancel.cs \
	Optimization.Messages/Communication.cs \
	Optimization.Messages/Response.cs \
	Optimization.Messages/Task.cs \
	Optimization.Messages/Token.cs \
	Optimization.Messages/Messages.cs \
	Optimization.Storage/Storage.cs \
	Optimization.Storage/Records.cs \
	Optimization.Dispatcher/Dispatcher.cs \
	Optimization.Dispatcher/Webots.cs \
	Optimization.Dispatcher.Internal/Dispatcher.cs \
	Optimization.Dispatcher.Internal/Internal.cs \
	Optimization.Dispatcher.Internal/Registry.cs

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	optimization-sharp.pc.in 

REFERENCES =  \
	Mono.Data.SqliteClient \
	Mono.Posix \
	System.Data \
	System \
	System.Xml

DLL_REFERENCES =  \
	$(BUILD_DIR)/protobuf-net.dll

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

PROTOBUF_NET_DLL = $(BUILD_DIR)/protobuf-net.dll
OPTIMIZATION_SHARP_PC = $(BUILD_DIR)/optimization-sharp.pc

$(eval $(call emit-deploy-target,PROTOBUF_NET_DLL))
$(eval $(call emit-deploy-wrapper,OPTIMIZATION_SHARP_PC,optimization-sharp.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
