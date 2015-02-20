using System.Reflection;
using System;

namespace GnomeSubfinder.Core.DataStructures
{
	public interface IBackend
	{
		int SubtitlesCount (VideoFileInfo file, int language);

		double[] GetSubtitlesRatio (VideoFileInfo file, int language);

		SubtitleFileInfo[] SearchSubtitles (VideoFileInfo video, string[] languages);

		bool Init (params object[] parameters);

		string GetName ();

		Tuple<Assembly, string> GetLogoAssemblyInfo ();
	}
}

