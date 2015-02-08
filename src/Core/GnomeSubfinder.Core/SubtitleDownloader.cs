using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using GnomeSubfinder.Core.Interfaces;

namespace GnomeSubfinder.Core.Core
{
	public class SubtitleDownloader
	{
		int processed;
		readonly List<SubtitleFileInfo> subtitleFiles = new List<SubtitleFileInfo> ();

		public event EventHandler DownloadStatusChanged;
		public event EventHandler DownloadCompleted;

		public int Processed { 
			get { return processed; }
		}

		public int Total {
			get { return subtitleFiles.Count; } 
		}

		public double Status { 
			get { return (double)Processed / subtitleFiles.Count; }
		}

		public void Download ()
		{
			processed = 0;
			foreach (var subtitleFile in subtitleFiles) {
				var cli = new WebClient ();
				cli.DownloadDataAsync (new Uri (subtitleFile.DownloadFile));

				var tmp = subtitleFile;
				cli.DownloadDataCompleted += (sender, e) => {
					string filename = Path.GetFileNameWithoutExtension (tmp.DownloadFile);
					File.WriteAllBytes (Path.Combine (Path.GetDirectoryName (tmp.Video.FileName), filename), e.Result);
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
	}
}

