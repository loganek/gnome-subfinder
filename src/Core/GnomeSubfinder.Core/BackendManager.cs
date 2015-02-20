using System.Linq;
using GnomeSubfinder.Backends.OpenSubtitles;
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

		public DataStructures.SubtitleFileInfo[] SearchSubtitles (DataStructures.VideoFileInfo video, string[] languages)
		{
			return backends.SelectMany (b => b.SearchSubtitles (video, languages)).ToArray ();
		}

		public BackendCollection Backends {
			get { return backends; }
		}
	}
}

