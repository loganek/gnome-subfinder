ASSEMBLY = GnomeSubfinder.GUIHelper
SOURCES = LanguageSet.cs
REFERENCES = $(BIN_DIR)/GnomeSubfinder.Core.dll 
ASSEMBLY_BUILD_FLAGS = $(GLIBSHARP_LIBS) $(GTKSHARP_LIBS)
TARGET = library
RESOURCES=$(wildcard Resources/flags/*)
RESOURCES_PREFIX=GnomeSubfinder.Core.GUIHelper.Resources.flags.
include $(top_srcdir)/m4/build.mk
