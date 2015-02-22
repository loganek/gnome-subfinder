using NUnit.Framework;
using GnomeSubfinder.Core.DataStructures;

namespace Core
{
	class BackendB1: IBackend
	{
		#region IBackend implementation

		public int SubtitlesCount (VideoFileInfo file, int language)
		{
			throw new System.NotImplementedException ();
		}

		public double[] GetSubtitlesRatio (VideoFileInfo file, int language)
		{
			throw new System.NotImplementedException ();
		}

		public SubtitleFileInfo[] SearchSubtitles (VideoFileInfo video, string[] languages)
		{
			throw new System.NotImplementedException ();
		}

		public bool Init (params object[] parameters)
		{
			throw new System.NotImplementedException ();
		}

		public virtual string GetName ()
		{
			return "B1";
		}

		public System.Tuple<System.Reflection.Assembly, string> GetLogoAssemblyInfo ()
		{
			throw new System.NotImplementedException ();
		}

		#endregion
	}

	class BackendB2 : BackendB1
	{
		public override string GetName ()
		{
			return "B2";
		}
	}

	[TestFixture]
	public class SubtitleFileInfoTest
	{
		readonly string[] langs = { "pol", "eng" };
		readonly string[] backends = { "B1", "B2" };

		[Test]
		public void CheckLanguageSelector ()
		{
			var s1 = new SubtitleFileInfo {Language = "eng"};
			var s2 = new SubtitleFileInfo {Language = "pol"};
			Assert.AreEqual (s2, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}

		[Test]
		public void CheckBackendSelector ()
		{
			var s1 = new SubtitleFileInfo {Language = "eng", Backend = new BackendB2 ()};
			var s2 = new SubtitleFileInfo {Language = "eng", Backend = new BackendB1 ()};
			Assert.AreEqual (s2, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}

		[Test]
		public void CheckDownloadsCountSelector ()
		{
			var s1 = new SubtitleFileInfo {Language = "eng", DownloadsCount = 14, Backend = new BackendB1 ()};
			var s2 = new SubtitleFileInfo { Language = "eng", DownloadsCount = 20, Backend = new BackendB1 () };
			Assert.AreEqual (s2, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}

		[Test]
		public void CheckRatingSelector ()
		{
			var s1 = new SubtitleFileInfo {
				Language = "eng",
				DownloadsCount = 14,
				Backend = new BackendB1 (),
				Rating = 3.4
			};
			var s2 = new SubtitleFileInfo {
				Language = "eng",
				DownloadsCount = 14,
				Backend = new BackendB1 (),
				Rating = 1.6
			};
			Assert.AreEqual (s1, SubtitleFileInfo.MatchBest (new []{ s1, s2 }, langs, backends));
		}
	}
}

