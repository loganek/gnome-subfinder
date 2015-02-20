using System;

namespace GnomeSubfinder.Core.DataStructures
{
	public class SubtitleFileInfo
	{
		public string CurrentPath 
		{ get; set; }

		public string DownloadFile
		{ get; set; }

		public double Rating
		{ get; set; }

		public int DownloadsCount 
		{ get;	set; }

		public string Language 
		{ get; set; }

		public string IdMovieImdb 
		{ get; set; }

		public IBackend Backend
		{ get; set; }

		public VideoFileInfo Video 
		{ get; set; }

		public static SubtitleFileInfo MatchBest (SubtitleFileInfo[] enumerable, string[] langs, string[] backends)
		{
			if (enumerable.Length == 0)
				throw new ArgumentException ("cannot get best match from empty array");

			Array.Sort (enumerable,
				(x, y) => {
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

