ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))

ASSEMBLY_FILE = $(ASSEMBLY).$(ASSEMBLY_EXTENSION)

all: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES)
	@mkdir -p $(top_builddir)/bin
	$(MCS) $(SOURCES) \
		$(MCS_FLAGS) \
		$(MCS_OPTIONS) \
		-target:$(TARGET) \
		-lib:$(top_builddir)/bin/ \
		-out:$@ \ 
		-debug
	@cp $@ $(top_builddir)/bin/
