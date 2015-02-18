using GnomeSubfinder.Backends.OpenSubtitles;
using System.Collections.Generic;
using System.Globalization;

namespace GnomeSubfinder.Core.Core
{
	public class BackendManager
	{
		readonly BackendCollection backends = new BackendCollection ();

		public BackendManager ()
		{
			backends.Add <OpenSubtitlesBackend> ("loganek", "test", CultureInfo.CurrentCulture.ThreeLetterISOLanguageName);
		}

		public IEnumerable<Interfaces.SubtitleFileInfo> SearchSubtitles (Interfaces.VideoFileInfo video, string[] languages)
		{
			foreach (var b in backends) {
				foreach (var s in b.SearchSubtitles (video, languages))
					yield return s;
			}
		}

		public BackendCollection Backends {
			get { return backends; }
		}
	}
}

