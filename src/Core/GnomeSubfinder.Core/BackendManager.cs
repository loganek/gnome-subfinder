using System.Linq;
using GnomeSubfinder.Core.DataStructures;

namespace GnomeSubfinder.Core.Core
{
	public class BackendManager
	{
		readonly BackendCollection backends = new BackendCollection ();

		public void AddBackend<T> (params object[] parameters) where T : IBackend, new()
		{
			backends.Add<T> (parameters);
		}

		public SubtitleFileInfo[] SearchSubtitles (VideoFileInfo video, string[] languages)
		{
			return backends.SelectMany (b => b.SearchSubtitles (video, languages)).ToArray ();
		}

		public BackendCollection Backends {
			get { return backends; }
		}
	}
}

