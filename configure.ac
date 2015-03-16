m4_define([subfinder_version_major], [1])
m4_define([subfinder_version_minor], [0])

m4_define([concat], $1$2)
m4_define([subfinder_version], concat(subfinder_version_major., subfinder_version_minor))
m4_define([subfinder_asm_version], [subfinder_version.0.0])
AC_SUBST([ASM_VERSION], [subfinder_asm_version])

AC_INIT([GNOME Subfinder], subfinder_version, [todo], [gnome-subfinder], [todo])
AM_INIT_AUTOMAKE
AM_SILENT_RULES([yes])

AC_CONFIG_MACRO_DIR([m4])

FIND_MONO_COMPILER
FIND_MONO_RUNTIME
CHECK_GUI_APPLICATION

AC_ARG_ENABLE(
	release, 
	AC_HELP_STRING([--enable-release], [Use release configuration]),
	enable_release=yes, 
	enable_release=`test ! -d .git && echo "yes" || echo "no"`)

AM_CONDITIONAL(ENABLE_RELEASE, test "x$enable_release" = "xyes")
ENABLE_RELEASE=$enable_release

AC_SUBST(ENABLE_RELEASE)
if test "x$enable_release" = "xyes"; then
	MCS_FLAGS="-define:RELEASE"
else
	MCS_FLAGS=""
fi

AC_CONFIG_FILES([
Makefile
src/Makefile
src/Core/Makefile
src/Core/GnomeSubfinder.DataStructures/Makefile
src/Core/GnomeSubfinder.Core/Makefile
src/Core/GnomeSubfinder.GUIHelper/Makefile
src/xml-rpc.net/Makefile
src/Backends/Makefile
src/Backends/Backends.OpenSubtitles.org/Makefile
src/Clients/Makefile
src/Clients/Subfinder/Makefile
src/Clients/SubfinderConsole/Makefile
src/AssemblyInfo.cs
])

AC_OUTPUT

cat <<EOF

${PACKAGE}-${VERSION}

  Build Environment
    Install Prefix:        ${prefix}
    Datadir:               ${expanded_datadir}
    Libdir:                ${expanded_libdir}

    Mono C# Compiler:      ${MCS} ${GMCS_FLAGS}
    Mono Runtime:          ${MONO}

  Extra features:
    GUI Application:       ${enable_gui_app}
      Gtk#:                ${gtk_sharp_version}

  Build/Development:
    Release Build:         ${enable_release}

EOF
