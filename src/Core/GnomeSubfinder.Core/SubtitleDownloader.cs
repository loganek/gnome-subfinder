using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using GnomeSubfinder.Core.DataStructures;

namespace GnomeSubfinder.Core.Core
{
	public class DownloadStatusChangedEventArgs : EventArgs
	{
		public SubtitleFileInfo SubtitleFile {get; private set;}
		public bool Error { get; private set; }

		public DownloadStatusChangedEventArgs (SubtitleFileInfo subtitleFile, bool error)
		{
			SubtitleFile = subtitleFile;
			Error = error;
		}
	}

	public delegate void DownloadStatusChangedEventHandler(object sender, DownloadStatusChangedEventArgs e);

	public class SubtitleDownloader
	{
		class TimeoutedWebClient : WebClient
		{
			readonly int timeout;

			public TimeoutedWebClient (int timeout)
			{
				this.timeout = timeout;
			}

			protected override WebRequest GetWebRequest( Uri address)
			{
				var result = base.GetWebRequest(address);
			    if (result == null) return null;
			    result.Timeout = timeout;
			    return result;
			}
		}

	    readonly int timeout;
		int processed;
		readonly List<SubtitleFileInfo> subtitleFiles = new List<SubtitleFileInfo> ();

		public event DownloadStatusChangedEventHandler DownloadStatusChanged;
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

		public SubtitleDownloader (int timeout)
		{
			this.timeout = timeout;
		}

		public void Download ()
		{
		    processed = 0;
			foreach (var subtitleFile in subtitleFiles) {
				DownloadSingleFile (subtitleFile);
			}
		}

		private void DownloadSingleFile(SubtitleFileInfo file)
		{
			var cli = new TimeoutedWebClient (timeout);
			cli.DownloadDataAsync (new Uri (file.DownloadFile));

			var tmp = file;
			cli.DownloadDataCompleted += (sender, e) => {
				bool err = false;
				try {
					SaveFile (tmp, e.Result);
				} catch (Exception) {
					err = true;
				} finally {
					err = err | e.Cancelled | (e.Error != null);
					OnDownloadStatusChanged (new DownloadStatusChangedEventArgs (tmp, err));
					Interlocked.Increment (ref processed);
					if (Processed == Total)
						OnDownloadCompleted (new EventArgs ());
				}
			};
		}

		public void Add (SubtitleFileInfo s)
		{
			subtitleFiles.Add (s);
		}

		protected virtual void OnDownloadStatusChanged (DownloadStatusChangedEventArgs e)
		{
			if (DownloadStatusChanged != null)
				DownloadStatusChanged (this, e);
		}

		protected virtual void OnDownloadCompleted (EventArgs e)
		{
			if (DownloadCompleted != null)
				DownloadCompleted (this, e);
		}

		void SaveFile (SubtitleFileInfo tmp, byte[] data)
		{
			new SubtitleSaver ().Save(tmp, data);
		}
	}
}

