using System.Collections.Generic;

namespace GnomeSubfinder.Core.Core
{
	public class LanguageSet
	{
		readonly Dictionary<string, string> langs;

		static LanguageSet instance;

		public static LanguageSet Instance {
			get { return instance ?? (instance = new LanguageSet()); }
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
			
		public Dictionary<string, string> Languages {
			get { return langs; }
		}

		public string JoinedLanguages {
			get {
				return string.Join (",", langs.Values);
			}
		}
	}
}

