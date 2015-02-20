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

		public GnomeSubfinder.Core.DataStructures.SubtitleFileInfo[] SearchSubtitles (GnomeSubfinder.Core.DataStructures.VideoFileInfo video, string[] languages)
		{
			var l = new List<GnomeSubfinder.Core.DataStructures.SubtitleFileInfo> ();
			foreach (var b in backends) {
				foreach (var s in b.SearchSubtitles (video, languages))
					l.Add (s);
			}
			return l.ToArray ();
		}

		public BackendCollection Backends {
			get { return backends; }
		}
	}
}

