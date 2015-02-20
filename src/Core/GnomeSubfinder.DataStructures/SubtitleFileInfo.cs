using System;

namespace GnomeSubfinder.Core.DataStructures
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

		public static SubtitleFileInfo MatchBest(SubtitleFileInfo[] enumerable, string[] langs, string[] backends)
		{
			if (enumerable.Length == 0)
				throw new ArgumentException ("cannot get best match from empty array");

			Array.Sort (enumerable,
				(SubtitleFileInfo x, SubtitleFileInfo y) => {
					int ix = Array.IndexOf (langs, x.Language), iy = Array.IndexOf (langs, y.Language);
					if (ix != iy)
						return ix > iy ? 1 : -1;
					ix = Array.IndexOf (backends, x.Backend.GetName ());
					iy = Array.IndexOf (backends, y.Backend.GetName ());
					if (ix != iy)
						return ix > iy ? 1 : -1;
					if (x.DownloadsCount != y.DownloadsCount)
						return x.DownloadsCount < y.DownloadsCount ? 1 : -1;
					return x.Rating < y.Rating ? 1 : -1;
				});

			return enumerable [0];
		}
	}
}

