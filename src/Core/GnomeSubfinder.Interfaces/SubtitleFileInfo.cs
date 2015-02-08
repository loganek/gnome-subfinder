namespace GnomeSubfinder.Core.Interfaces
{
	public class SubtitleFileInfo
	{
		public string DownloadFile
		{ get; private set; }

		public double Rating
		{ get; private set; }

		public int DownloadsCount 
		{ get;	private set; }

		public string Language 
		{ get; private set; }

		public IBackend Backend
		{ get; private set; }

		public VideoFileInfo Video 
		{ get; private set; }

		public SubtitleFileInfo (string downloadFile, double rating, string language, int downloadsCount, IBackend backend, VideoFileInfo video)
		{
			DownloadFile = downloadFile;
			Rating = rating;
			Language = language;
			DownloadsCount = downloadsCount;
			Backend = backend;
			Video = video;
		}
	}
}

