using Gdk;
using GnomeSubfinder.Core.Core;

namespace GnomeSubfinder.Core.GUIHelper
{
	public static class LanguageSetGUIExtension
	{
		public static Pixbuf GetFlag (this LanguageSet langset, string country, int width, int height)
		{
			country = country.Replace (' ', '_');
			return Pixbuf.LoadFromResource ("GnomeSubfinder.GUIHelper.Resources.flags." + country + ".png").ScaleSimple (width, height, InterpType.Nearest);

		}
	}
}
