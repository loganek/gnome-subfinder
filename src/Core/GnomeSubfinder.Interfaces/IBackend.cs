namespace GnomeSubfinder.Core.Interfaces
{
	public interface IBackend
	{
		int SubtitlesCount (VideoFileInfo file, int language);
		double[] GetSubtitlesRatio(VideoFileInfo file, int language);
		SubtitleFileInfo[] SearchSubtitles(VideoFileInfo video, string languages);
		bool Init (params object[] parameters);
		string GetName();
		Gdk.Pixbuf GetPixbuf(int width, int height);
	}
}

