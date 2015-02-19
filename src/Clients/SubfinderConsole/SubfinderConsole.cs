using System;
using GnomeSubfinder.Core.Core;
using GnomeSubfinder.Core.Interfaces;
using System.Threading;
using System.IO;

namespace SubfinderConsole
{
	public class Options
	{
		[Option ("languages", true, Help = "subtitles languages (prirority order, ISO 639-2 format), e.g. \"eng,pol,spa\"")]
		public string Languages{ get; set; }

		[Option ('c', "console", Help = "use command line interface")]
		public bool Console { get; set; }

		[Option ('h', "help", Help = "show this help")]
		public bool Help { get; set; }

		[Option ("timeout", true, Help = "downloading timeout in milliseconds")]
		public int Timeout { get; set; }

		[Option ("tempdir", true, Help = "temporary directory")]
		public string Tempdir { get; set; }

		[Option ("7zip", true, Help = "7zip path")]
		public string SZipPath { get; set; }

		public Options()
		{
			var p = Preferences.Instance;
			Languages = p.SelectedLanguages;
			Timeout = p.DownloadingTimeout;
			Tempdir = p.TemporaryDirectory;
			SZipPath = p.SZipPath;
		}
	}

	public class SubfinderConsole
	{	
		OptionParser<Options> parser; 

		string[] files;
		readonly object syncer = new object ();
		bool ready;

		public SubfinderConsole (OptionParser<Options> parser)
		{
			this.parser = parser;
			files = parser.FreeArguments;
		}

		void Download()
		{
			Console.WriteLine ("Downloading subtitles for {0} files...", files.Length);
			ready = false;
			var downloader = new SubtitleDownloader (parser.OptionsObject.Timeout);
			var backends = new BackendManager ();
			var l = parser.OptionsObject.Languages.Split (new []{ ',' });
			foreach (var file in files) {
				downloader.Add (SubtitleFileInfo.MatchBest (backends.SearchSubtitles (new VideoFileInfo { FileName = file }, l), l, l)); // todo langs twice!
			}
			downloader.Download (parser.OptionsObject.Tempdir, parser.OptionsObject.SZipPath);

			downloader.DownloadStatusChanged += (sender, e) => Console.WriteLine (" * [{0}] Download subtitles for {1} {2}!", 
				e.Error ? "Error" : "OK", Path.GetFileName(e.SubtitleFile.Video.FileName), e.Error ? "failed" : "succeeded");

			downloader.DownloadCompleted += (sender, e) => {
				lock (syncer) {
					ready = true;
					Monitor.Pulse (syncer);
				}
				Console.WriteLine (" * Download completed");
			};
			lock (syncer) {
				while (!ready) {
					Monitor.Wait (syncer);
				}
			}
		}

		public void Run ()
		{
			if (parser.OptionsObject.Help || files.Length == 0) {
				Console.WriteLine (parser.GetHelp ("[options] FILE1 FILE2 ...\nOptions: \n"));
				return;
			}

			Download ();
		}

		public void PrintHelp()
		{
			Console.WriteLine (parser.GetHelp ("[options] FILE1 FILE2 ...\nOptions: \n"));
		}

		public static void Main (string[] args)
		{
			var optParser = new OptionParser<Options> (new Options (), args);

			try {
				optParser.Parse ();
			} catch (ArgumentException ex) {
				Console.WriteLine ("Cannot parse arguments: " + ex.Message);
				return;
			}

			new SubfinderConsole (optParser).Run ();
		}
	}
}
