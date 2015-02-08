using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using GnomeSubfinder.Core.Interfaces;
using System.Diagnostics;

namespace GnomeSubfinder.Core.Core
{
	public class SubtitleDownloader
	{
		int processed;
		readonly List<SubtitleFileInfo> subtitleFiles = new List<SubtitleFileInfo> ();

		public event EventHandler DownloadStatusChanged;
		public event EventHandler DownloadCompleted;
		string tempDirectory;

		string sZipPath;

		public int Processed { 
			get { return processed; }
		}

		public int Total {
			get { return subtitleFiles.Count; } 
		}

		public double Status { 
			get { return (double)Processed / subtitleFiles.Count; }
		}

		public void Download (string tempDir, string sZipPath)
		{
			tempDirectory = tempDir;
			this.sZipPath = sZipPath;
			processed = 0;
			foreach (var subtitleFile in subtitleFiles) {
				var cli = new WebClient ();
				cli.DownloadDataAsync (new Uri (subtitleFile.DownloadFile));

				var tmp = subtitleFile;
				cli.DownloadDataCompleted += (sender, e) => {
					SaveFile (tmp, e.Result);
					Interlocked.Increment (ref processed);
					OnDownloadStatusChanged (new EventArgs ());

					if (Processed == Total)
						OnDownloadCompleted (new EventArgs ());
				};
			}
		}

		public void Add (SubtitleFileInfo s)
		{
			subtitleFiles.Add (s);
		}

		protected virtual void OnDownloadStatusChanged (EventArgs e)
		{
			if (DownloadStatusChanged != null)
				DownloadStatusChanged (this, e);
		}

		protected virtual void OnDownloadCompleted (EventArgs e)
		{
			if (DownloadCompleted != null)
				DownloadCompleted (this, e);
		}

		private void SaveFile (SubtitleFileInfo tmp, byte[] data)
		{
			if (data == null)
				return;

			var tempName = Path.GetFileName (Path.GetTempFileName ()).Replace ('.', '_');
			var tempFullFile = Path.Combine (tempDirectory, tempName);
			File.WriteAllBytes (tempFullFile, data);

			var p = new Process { StartInfo = new ProcessStartInfo (sZipPath, "x -y -o" + tempDirectory + " " + tempFullFile) {
					UseShellExecute = false,
					RedirectStandardOutput = true
				}
			};
			p.Start ();
			p.WaitForExit ();
			File.Copy (tempFullFile + "~", Path.Combine(Path.GetDirectoryName(tmp.Video.FileName), Path.GetFileNameWithoutExtension (tmp.Video.FileName) + ".txt"));
		}
	}
}

