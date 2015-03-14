using System;
using System.Collections.Generic;
using System.Net;
using GnomeSubfinder.Core.DataStructures;

namespace GnomeSubfinder.Core.Core
{
	public class DownloadStatusChangedEventArgs : EventArgs
	{
		public SubtitleFileInfo SubtitleFile { get; private set; }

		public Exception Error { get; private set; }

		public DownloadStatusChangedEventArgs (SubtitleFileInfo subtitleFile, Exception error = null)
		{
			SubtitleFile = subtitleFile;
			Error = error;
		}
	}

	public delegate void DownloadStatusChangedEventHandler (object sender, DownloadStatusChangedEventArgs e);

	public class SubtitleDownloader
	{
		class TimeoutedWebClient : WebClient
		{
			readonly int timeout;

			public TimeoutedWebClient (int timeout)
			{
				this.timeout = timeout;
			}

			protected override WebRequest GetWebRequest (Uri address)
			{
				var result = base.GetWebRequest (address);
				if (result == null)
					return null;
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

		public int Count {
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
			new System.Threading.Thread (() => {
				processed = 0;
				foreach (var subtitleFile in subtitleFiles) {
					DownloadSingleFile (subtitleFile);
				}
			}).Start ();
		}

		void DownloadSingleFile (SubtitleFileInfo file)
		{
			var cli = new TimeoutedWebClient (timeout);
			byte[] data = null;
			Exception err = null;
			try {
				data = cli.DownloadData (file.DownloadFile);
				new SubtitleSaver ().Save (file, data);

				if (Preferences.Instance.Encode) {
					var encoder = new EncodeChanger (file.CurrentPath);
					if (Preferences.Instance.AutoDetectEncoding) {
						encoder.AutoChangeEncoding (Preferences.Instance.EncodeTo);
					} else {
						encoder.ChangeEncoding (Preferences.Instance.EncodeFrom, Preferences.Instance.EncodeTo);
					}
				}

			} catch (Exception ex) {
				err = ex;
			} finally {
				OnDownloadStatusChanged (new DownloadStatusChangedEventArgs (file, err));
				processed++;
				if (Processed == Count)
					OnDownloadCompleted (new EventArgs ());
			}
		}

		public void Add (SubtitleFileInfo s)
		{
			subtitleFiles.Add (s);
		}

		public void Clear ()
		{
			subtitleFiles.Clear ();
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
	}
}

