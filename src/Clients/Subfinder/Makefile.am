ASSEMBLY = Subfinder
SOURCES = MainWindow.cs PreferencesDialog.cs Subfinder.cs 
REFERENCES = $(BIN_DIR)/GnomeSubfinder.Core.dll $(BIN_DIR)/GnomeSubfinder.DataStructures.dll $(BIN_DIR)/GnomeSubfinder.GUIHelper.dll $(BIN_DIR)/SubfinderConsole.exe System.Configuration Mono.Posix
ASSEMBLY_BUILD_FLAGS = $(GLIBSHARP_LIBS) $(GTKSHARP_LIBS)
TARGET = exe
RESOURCES = $(wildcard Resources/*) $(wildcard *.glade)
RESOURCES_PREFIX = Subfinder.
include $(top_srcdir)/m4/build.mk
