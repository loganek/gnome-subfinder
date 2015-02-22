SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))

SOURCES_BUILD += $(ifelse $(ASSEMBLY_INFO), "",  $(top_srcdir)/src/AssemblyInfo.cs, $(ASSEMBLY_INFO))

BIN_DIR = $(top_srcdir)/bin

RESOURCES_EXPANDED = $(addprefix $(srcdir)/, $(RESOURCES))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED), \
	-resource:$(resource),$(RESOURCES_PREFIX)$(notdir $(resource)))


ASSEMBLY_REFERENCES = $(addprefix -r:, $(REFERENCES))

THEME_ICONS_SOURCE = $(wildcard $(srcdir)/ThemeIcons/*/*/*.png) $(wildcard $(srcdir)/ThemeIcons/scalable/*/*.svg)
THEME_ICONS_RELATIVE = $(subst $(srcdir)/ThemeIcons/, , $(THEME_ICONS_SOURCE))

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(top_builddir)/bin/$(ASSEMBLY).$(ASSEMBLY_EXTENSION)

OUTPUT_FILES = \
	$(ASSEMBLY_FILE) \
	$(ASSEMBLY_FILE).mdb

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(THEME_ICONS_SOURCE)

CLEANFILES = $(OUTPUT_FILES)
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in

UNIQUE_FILTER_PIPE = tr [:space:] \\n | sort | uniq
BUILD_DATA_DIR = $(top_builddir)/bin/share/$(PACKAGE)

INSTALL_ICONS = $(top_srcdir)/build/private-icon-theme-installer "$(mkinstalldirs)" "$(INSTALL_DATA)"

INSTALL_DIR_RESOLVED = $(firstword $(subst , $(DEFAULT_INSTALL_DIR), $(INSTALL_DIR)))

FILTERED_LINK = $(shell echo "$(LINK)" | $(UNIQUE_FILTER_PIPE))
DEP_LINK = $(shell echo "$(LINK)" | $(UNIQUE_FILTER_PIPE) | sed s,-r:,,g | grep '$(top_builddir)/bin/')
DLL_MAP_VERIFIER_ASSEMBLY = $(top_srcdir)/build/dll-map-verifier.exe

moduledir = $(INSTALL_DIR_RESOLVED)
module_SCRIPTS = $(OUTPUT_FILES)

all: $(ALL_TARGETS)

run:
	@pushd $(top_builddir); \
	make run; \
	popd;

test:
	@pushd $(top_builddir)/tests; \
	make $(ASSEMBLY); \
	popd;

build-debug:
	@echo $(DEP_LINK)


$(ASSEMBLY_FILE).mdb: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD)  $(DEP_LINK) $(RESOURCES_EXPANDED) #$(DLL_MAP_VERIFIER_ASSEMBLY) 
	@mkdir -p $(top_builddir)/bin
	$(MCS) \
		$(GMCS_FLAGS) \
		$(ASSEMBLY_BUILD_FLAGS) \
		$(ASSEMBLY_REFERENCES) \
		$$warn \
		-debug -target:$(TARGET) -out:$@ \
		$(BUILD_DEFINES) $(ENABLE_TESTS_FLAG) \
		$(FILTERED_LINK) $(RESOURCES_BUILD) $(SOURCES_BUILD)
	@if [ -e $(srcdir)/$(notdir $@.config) ]; then \
		cp $(srcdir)/$(notdir $@.config) $(top_builddir)/bin; \
	fi;
	@if [ ! -z "$(EXTRA_BUNDLE)" ]; then \
		cp $(EXTRA_BUNDLE) $(top_builddir)/bin; \
	fi;

theme-icons: $(THEME_ICONS_SOURCE)
	@$(INSTALL_ICONS) -il "$(BUILD_DATA_DIR)" "$(srcdir)" $(THEME_ICONS_RELATIVE)

install-data-hook: $(THEME_ICONS_SOURCE)
	@$(INSTALL_ICONS) -i "$(DESTDIR)$(pkgdatadir)" "$(srcdir)" $(THEME_ICONS_RELATIVE)
	$(EXTRA_INSTALL_DATA_HOOK)

uninstall-hook: $(THEME_ICONS_SOURCE)
	@$(INSTALL_ICONS) -u "$(DESTDIR)$(pkgdatadir)" "$(srcdir)" $(THEME_ICONS_RELATIVE)
	$(EXTRA_UNINSTALL_HOOK)

