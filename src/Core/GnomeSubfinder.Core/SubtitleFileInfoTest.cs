using NUnit.Framework;
using GnomeSubfinder.Core.DataStructures;
using GnomeSubfinder.Backends.OpenSubtitles;

namespace Core
{
	[TestFixture]
	public class SubtitleFileInfoTest
	{
		readonly string[] langs = { "pol", "eng" };
		readonly string[] backends = { "OpenSubtitles.org"  /* todo */};

		[Test]
		public void CheckLanguageSelector ()
		{
			var s1 = new SubtitleFileInfo {Language = "eng"};
			var s2 = new SubtitleFileInfo {Language = "pol"};
			Assert.AreEqual (s2, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}

		[Test]
		[Ignore("Implement second backend first")]
		public void CheckBackendSelector ()
		{
			var s1 = new SubtitleFileInfo {Language = "eng", Backend = new OpenSubtitlesBackend ()};
				var s2 = new SubtitleFileInfo {Language = "eng", Backend = null /* todo */};
			Assert.AreEqual (s2, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}

		[Test]
		public void CheckDownloadsCountSelector ()
		{
			var s1 = new SubtitleFileInfo {Language = "eng", DownloadsCount = 14, Backend =new OpenSubtitlesBackend ()};
			var s2 = new SubtitleFileInfo { Language = "eng", DownloadsCount = 20, Backend = new OpenSubtitlesBackend () };
			Assert.AreEqual (s2, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}

		[Test]
		public void CheckRatingSelector ()
		{
			var s1 = new SubtitleFileInfo {
				Language = "eng",
				DownloadsCount = 14,
				Backend = new OpenSubtitlesBackend (),
				Rating = 3.4
			};
			var s2 = new SubtitleFileInfo {
				Language = "eng",
				DownloadsCount = 14,
				Backend = new OpenSubtitlesBackend (),
				Rating = 1.6
			};
			Assert.AreEqual (s1, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}
	}
}

