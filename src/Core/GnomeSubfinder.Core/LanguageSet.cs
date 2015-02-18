using System.Collections.Generic;
using Gdk;

namespace GnomeSubfinder.Core.Core
{
	public class LanguageSet
	{
		readonly Dictionary<string, string> langs;

		static LanguageSet instance;

		public static LanguageSet Instance {
			get {
				if (instance == null) {
					instance = new LanguageSet ();
				}
				return instance;
			}
		}

		LanguageSet ()
		{
			langs = new Dictionary<string, string> {
				{ "Belarusian", "bel" },
				{ "Bulgarian", "bul" },
				{ "Chinese", "chi" },
				{ "Czech", "ces" },
				{ "Danish", "dan" },
				{ "English", "eng" },
				{ "Finnish", "fin" },
				{ "French", "fra" },
				{ "Greek", "grk" },
				{ "Hindi", "hin" },
				{ "Hungarian", "hun" },
				{ "Irish", "gle" },
				{ "Italian", "ita" },
				{ "Japanese", "jpn" },
				{ "Polish", "pol" },
				{ "Portuguese", "por" },
				{ "Slovak", "slo" },
				{ "Slovenian", "svl" },
				{ "Spanish", "spa" },
				{ "Turkish", "tur" },
			};
		}

		public string this [string key] {
			get { return langs [key]; }
		}

		public Pixbuf GetFlag (string country, int width, int height)
		{
			country = country.Replace (' ', '_');
			return Pixbuf.LoadFromResource ("Core.Resources.flags." + country + ".png").ScaleSimple (width, height, InterpType.Nearest);
		}

		public Dictionary<string, string> Languages {
			get { return langs; }
		}
	}
}

