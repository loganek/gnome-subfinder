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
				{ "Argentina", "spa" },
				{ "Australia", "eng" },
				{ "Austria", "ger" },
				{ "Belarus", "bel" },
				{ "Belgium", "dut" },
				{ "Brazil", "por" },
				{ "Bulgaria", "bul" },
				{ "Canada", "eng" },
				{ "Chile", "spa" },
				{ "China", "chi" },
				{ "Czech Republic", "ces" },
				{ "Denmark", "dan" },
				{ "Finland", "fin" },
				{ "France", "fra" },
				{ "Greece", "grk" },
				{ "Hungary", "hun" },
				{ "India", "hin" },
				{ "Ireland", "gle" },
				{ "Italy", "ita" },
				{ "Japan", "jpn" },
				{ "Mexico", "spa" },
				{ "New Zealand", "eng" },
				{ "Poland", "pol" },
				{ "Portugal", "por" },
				{ "Slovakia", "slo" },
				{ "Slovenia", "svl" },
				{ "Spain", "spa" },
				{ "Switzerland", "ger" },
				{ "Turkey", "tur" },
				{ "United Kingdom", "eng" },
				{ "United States of America", "eng" },
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

