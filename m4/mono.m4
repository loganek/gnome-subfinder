AC_DEFUN([FIND_PROGRAM_OR_FAIL],
[
	AC_CHECK_PROG($1, $2, yes, no)
	if test "x$$1" = "xno"; then
		AC_MSG_ERROR([You have to install '$2'])
	fi
	$1=$2
	AC_SUBST($1)
])

AC_DEFUN([FIND_MONO_COMPILER],
[
	FIND_PROGRAM_OR_FAIL(MCS, mcs)
])

AC_DEFUN([FIND_MONO_RUNTIME],
[
	FIND_PROGRAM_OR_FAIL(MONO, mono)
])

AC_DEFUN([FIND_GTK_SHARP],
[
	G_SHARP_REQUIRED_VERSION=2.99.4

	PKG_CHECK_MODULES(GLIBSHARP, glib-sharp-3.0 >= $G_SHARP_REQUIRED_VERSION)
	PKG_CHECK_MODULES(GTKSHARP, gtk-sharp-3.0 >= $G_SHARP_REQUIRED_VERSION)

	gtk_sharp_version=$(pkg-config --modversion gtk-sharp-3.0)

	AC_SUBST(GLIBSHARP_LIBS)
	AC_SUBST(GTKSHARP_LIBS)
])

AC_DEFUN([CHECK_GUI_APPLICATION],
[
	AC_ARG_ENABLE(gui-application, AC_HELP_STRING([--disable-gui-application], [Don't build gui application]), enable_gui_app="no", enable_gui_app="yes")

	if test "x$enable_gui_app" = "xyes"; then
		FIND_GTK_SHARP
#		PKG_CHECK_MODULES(GTK, gtk+-3.0 >= 3.15)

		AM_CONDITIONAL(GUI_APPLICATION_ENABLED, true)
	else
		AM_CONDITIONAL(GUI_APPLICATION_ENABLED, false)
	fi
])
