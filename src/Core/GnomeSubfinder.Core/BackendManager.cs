using GnomeSubfinder.Backends.OpenSubtitles;
using System.Collections.Generic;

namespace GnomeSubfinder.Core.Core
{
	public class BackendManager
	{
		readonly BackendCollection backends = new BackendCollection ();

		public BackendManager ()
		{
			backends.Add <OpenSubtitlesBackend> ("loganek", "test", "eng");
		}

		public IEnumerable<Interfaces.SubtitleFileInfo> SearchSubtitles(Interfaces.VideoFileInfo video, string language)
		{
			foreach (var b in backends) 
			{
				foreach (var s in b.SearchSubtitles (video, language))
					yield return s;
			}
		}

		public BackendCollection Backends
		{
			get { return backends; }
		}
 	}
}

