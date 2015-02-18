using NUnit.Framework;
using GnomeSubfinder.Core.Interfaces;
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
			var s1 = new SubtitleFileInfo ("", 0.0, "eng", 0, null, null);
			var s2 = new SubtitleFileInfo ("", 0.0, "pol", 0, null, null);
			Assert.AreEqual (s2, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}

		[Test]
		[Ignore("Implement second backend first")]
		public void CheckBackendSelector ()
		{
			var s1 = new SubtitleFileInfo ("", 0.0, "eng", 0, new OpenSubtitlesBackend (), null);
			var s2 = new SubtitleFileInfo ("", 0.0, "eng", 0, null /* todo */, null);
			Assert.AreEqual (s2, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}

		[Test]
		public void CheckDownloadsCountSelector ()
		{
			var s1 = new SubtitleFileInfo ("", 0.0, "eng", 14, new OpenSubtitlesBackend (), null);
			var s2 = new SubtitleFileInfo ("", 0.0, "eng", 20, new OpenSubtitlesBackend (), null);
			Assert.AreEqual (s2, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}

		[Test]
		public void CheckRatingSelector ()
		{
			var s1 = new SubtitleFileInfo ("", 3.0, "eng", 20, new OpenSubtitlesBackend (), null);
			var s2 = new SubtitleFileInfo ("", 1.4, "eng", 20, new OpenSubtitlesBackend (), null);
			Assert.AreEqual (s1, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}
	}
}

