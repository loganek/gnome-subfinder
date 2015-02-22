using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GnomeSubfinder.Core.Core;
using GnomeSubfinder.Core.DataStructures;
using Gtk;
using Mono.Posix;
using System.Net;

using UI = Gtk.Builder.ObjectAttribute;

namespace Subfinder
{
	public class MainWindow : Window
	{
		#region UI controls
		[UI] readonly FileFilter videoFilter;
		[UI] readonly TreeView videoFilesView;
		[UI] readonly TreeView foundSubtitlesView;
		[UI] readonly TreeView downloadedSubtitlesView;
		[UI] readonly ProgressBar downloadStatus;
		[UI] readonly Viewport treeParent;
		[UI] readonly Button searchButton;
		[UI] readonly Button downloadSubtitles;
		[UI] readonly Statusbar appStatusbar;
		[UI] readonly Notebook mainNotebook;
		[UI] readonly ListStore videosStore;
		[UI] readonly ScrolledWindow simpleModeContainer;
		[UI] readonly ScrolledWindow advancedModeContainer;
		[UI] readonly Menu subtitlesMenu;
		#endregion UI controls

		readonly Spinner waitWidget = new Spinner { Visible = true, Active = true };

		readonly object appenderLocker = new object ();

		readonly TreeStore subtitlesStore = new TreeStore (typeof(bool), typeof(double), typeof(int), typeof(string), typeof(string), typeof(SubtitleFileInfo));
		readonly TreeStore oneClickVideoStore = new TreeStore (typeof(Gdk.Pixbuf), typeof(string), typeof(SubtitleFileInfo));

		readonly BackendManager controller = new BackendManager ();

		public MainWindow (Builder builder, IntPtr handle) : base (handle)
		{
			builder.Autoconnect (this);

			Destroyed += (sender, e) => {
				Preferences.Instance.ActiveTab = mainNotebook.CurrentPage;
				Preferences.Instance.Save ();
				Application.Quit ();
			};

			foundSubtitlesView.Model = new TreeModelSort (subtitlesStore);

			mainNotebook.SwitchPage += (o, args) => downloadedSubtitlesView.Reparent (args.PageNum == 0 ? simpleModeContainer : advancedModeContainer);
 
			downloadedSubtitlesView.Model = oneClickVideoStore;
			mainNotebook.CurrentPage = Preferences.Instance.ActiveTab < 2 ? Preferences.Instance.ActiveTab : 0;
		}

		void ColumnClicked (object sender, EventArgs e)
		{
			int id;
			SortType order;
			var model = foundSubtitlesView.Model as TreeModelSort;
			model.GetSortColumnId (out id, out order);
			model.SetSortColumnId (Array.IndexOf (foundSubtitlesView.Columns, sender), order == SortType.Descending || id == -1 ? SortType.Ascending : SortType.Descending);
		}

		void SelectSubToDownload (object sender, ToggledArgs e)
		{
			TreeIter iter;
			if (subtitlesStore.GetIterFromString (out iter, e.Path)) {
				var val = (bool)subtitlesStore.GetValue (iter, 0);
				subtitlesStore.SetValue (iter, 0, !val);
			}
		}

		IEnumerable<string> LoadVideoFiles ()
		{
			var videoChooser = new FileChooserDialog (Catalog.GetString ("Choose the file to open"), this, FileChooserAction.Open, new Object[] {
				Catalog.GetString ("Cancel"),
				ResponseType.Cancel,
				Catalog.GetString ("Open"),
				ResponseType.Accept
			}) {
				SelectMultiple = true,
				Filter = videoFilter
			};

			var files = new List<string> ();
			if (videoChooser.Run () == (int)ResponseType.Accept) {
				files.AddRange (videoChooser.Files.Select (f => f.Path));
			}
			videoChooser.Destroy ();
			return files.ToArray ();
		}



		void FindSubtitles ()
		{
			int count = 0;
			foreach (object[] videoFile in videosStore) {
				var filename = videoFile [0] as string;
				try {
					if (!File.Exists (filename))
						throw new IOException (Catalog.GetString ("File ") + filename + Catalog.GetString (" doesn't exists"));

					var subs = controller.SearchSubtitles (new VideoFileInfo { FileName = filename }, Preferences.Instance.GetSelectedLanguages ());

					Application.Invoke ((e, s) => {
						TreeIter iter = subtitlesStore.AppendValues (null, null, null, null, System.IO.Path.GetFileName (filename), null);
						foreach (var sub in subs) {
							subtitlesStore.AppendValues (iter, false, sub.Rating, sub.DownloadsCount, sub.Language, sub.Backend.GetName (), sub);
						}
						count += subs.Length;
					});
				} catch (IOException ex) {
					Application.Invoke ((e, s) => Utils.ShowMessageDialog ("Error: " + ex.Message, MessageType.Error));
				} catch (WebException ex) {
					Application.Invoke ((e, s) => Utils.ShowMessageDialog ("Web exception: " + ex.Message, MessageType.Error));
				} catch (ApplicationException ex) {
					Application.Invoke ((e, s) => Utils.ShowMessageDialog ("Web exception: " + ex.Message, MessageType.Error));
				}
			}
			Application.Invoke ((sndr, evnt) => {
				treeParent.Remove (waitWidget);
				treeParent.Add (foundSubtitlesView);
				searchButton.Sensitive = true;
				ShowInfo (String.Format ("Search completed. Found: {0} file(s).", count));
			});
		}



		TreeIter FindParentIter (SubtitleFileInfo subtitleFile)
		{
			TreeIter iter;
			bool ok = oneClickVideoStore.GetIterFirst (out iter);
			while (ok) {
				var t = oneClickVideoStore.GetValue (iter, 1) as string;
				if (t == subtitleFile.Video.FileName) {
					return iter;
				}
				ok = oneClickVideoStore.IterNext (ref iter);
			}
			return TreeIter.Zero;
		}

		void DownloadSubtitles (SubtitleFileInfo[] subs)
		{
			if (subs.Length == 0)
				return;

			var downloader = new SubtitleDownloader (Preferences.Instance.DownloadingTimeout);
			oneClickVideoStore.Clear ();

			foreach (var s in subs)
				downloader.Add (s);

			ShowInfo ("Downloading subtitles...");
			downloadSubtitles.Sensitive = false;

			downloader.DownloadStatusChanged += (sdr, evt) => Application.Invoke ((sndr, evnt) => {
				downloadStatus.Text = downloader.Processed + "/" + downloader.Total;
				downloadStatus.Fraction = downloader.Status;

				lock (appenderLocker) {
					TreeIter parent = FindParentIter (evt.SubtitleFile);
					if (parent.Equals (TreeIter.Zero)) {
						parent = oneClickVideoStore.AppendValues (Gdk.Pixbuf.LoadFromResource (string.Format ("Subfinder.mov.png")),
							evt.SubtitleFile.Video.FileName, evt.SubtitleFile);
					}
					oneClickVideoStore.AppendValues (parent, 
						Gdk.Pixbuf.LoadFromResource (string.Format ("Subfinder.{0}.png", evt.Error ? "bad" : "good")),
						evt.SubtitleFile.CurrentPath, evt.SubtitleFile);
				}
			});

			downloader.Download ();

			downloader.DownloadCompleted += (sdr, evt) => Application.Invoke ((sndr, evnt) => {
				downloadSubtitles.Sensitive = true;
				ShowInfo ("Download completed");
				Utils.ShowMessageDialog (Catalog.GetString ("Download completed!"), MessageType.Info);
			});
		}

		static void ShowAboutActicate (object sender, EventArgs e)
		{
			var builder = new Builder (null, "Subfinder.subfinder.glade", null);
			var dialog = new AboutDialog (builder.GetObject ("aboutDialog").Handle);
			dialog.Run ();
			dialog.Destroy ();
		}

		static void PreferencesActiveBtn (object sender, EventArgs e)
		{
			var builder = new Builder (null, "Subfinder.subfinder-properties.glade", null);
			var preferences = new PreferencesDialog (builder, builder.GetObject ("preferencesDialog").Handle);
			preferences.Run ();
			preferences.Destroy ();
		}
			
		void ShowInfo (string message)
		{
			appStatusbar.Push (0, message);
		}
			
		void ViewPopupMenu ()
		{
			subtitlesMenu.Show ();
			subtitlesMenu.Popup ();
		}

		static bool PageExists (string url)
		{
			try {
				var request = (HttpWebRequest)WebRequest.Create (url);
				request.Method = "HEAD";
				request.Timeout = Preferences.Instance.DownloadingTimeout;
				return ((HttpWebResponse)request.GetResponse ()).StatusCode == HttpStatusCode.OK;
			} catch {
				return false;
			}
		}

		SubtitleFileInfo GetSelectedSubtitles ()
		{
			TreeIter iter = Utils.GetSelectedIter (downloadedSubtitlesView);
			return iter.Equals (TreeIter.Zero) ? null : downloadedSubtitlesView.Model.GetValue (iter, 2) as SubtitleFileInfo;
		}

		#region Buttons clicked event
		void on_addVideo_clicked (object sender, EventArgs e)
		{
			foreach (var f in LoadVideoFiles ()) {
				if (videosStore.Cast<object[]> ().All (filename => filename [0] as string != f)) {
					videosStore.AppendValues (f);
				}
			}
		}

		void on_removeVideo_clicked (object sender, EventArgs e)
		{
			TreePath[] treePath = videoFilesView.Selection.GetSelectedRows ();

			for (int i = treePath.Length - 1; i >= 0; i--) {
				TreeIter iter;
				if (videosStore.GetIter (out iter, treePath [i])) {
					videosStore.Remove (ref iter);
				}
			}
		}

		void on_downloadSelected_clicked (object sender, EventArgs e)
		{
			var subs = new List<SubtitleFileInfo> ();
			subtitlesStore.Foreach ((model, path, iter) => {
				if ((bool)model.GetValue (iter, 0)) {
					var s = model.GetValue (iter, 5) as SubtitleFileInfo;
					if (s != null)
						subs.Add (s);
				}
				return false;
			});

			if (subs.Count == 0) {
				Utils.ShowMessageDialog ("Select subtitles first", MessageType.Info);
				return;
			}

			DownloadSubtitles (subs.ToArray ());
		}

		void on_oneClickDownload_click (object sender, EventArgs evt)
		{
			var subs = new List<SubtitleFileInfo> ();
			var files = LoadVideoFiles ();
			try {
				foreach (var filename in files) {
					ShowInfo ("Searching subtitles for file " + filename);
					if (!File.Exists (filename))
						throw new IOException (string.Format(Catalog.GetString ("File {0} doesn't exists."), filename));

					var langs = Preferences.Instance.GetSelectedLanguages ();
					subs.Add (SubtitleFileInfo.MatchBest (
						controller.SearchSubtitles (new VideoFileInfo { FileName = filename }, langs), langs, controller.GetBackendNames ()));
				}
				DownloadSubtitles (subs.ToArray ());
			} catch (IOException ex) {
				Application.Invoke ((e, s) => Utils.ShowMessageDialog ("Error: " + ex.Message, MessageType.Error));
			} catch (WebException ex) {
				Application.Invoke ((e, s) => Utils.ShowMessageDialog ("Web exception: " + ex.Message, MessageType.Error));
			} catch (ArgumentException) {
				Application.Invoke ((e, s) => Utils.ShowMessageDialog ("Subtitles not found. ", MessageType.Error));
			}
		}

		void on_searchSubtitles_clicked (object sender, EventArgs e)
		{
			if (videosStore.IterNChildren () == 0) {
				ShowInfo ("Add files first");
				return;
			}

			subtitlesStore.Clear ();

			searchButton.Sensitive = false;
			treeParent.Remove (foundSubtitlesView);
			treeParent.Add (waitWidget);
			ShowInfo ("Searching subtitles.");
			new System.Threading.Thread (FindSubtitles).Start ();
		}
		#endregion Buttons clicked event

		#region downloadedSubtitles context menu
		[GLib.ConnectBefore]
		void on_downloadedSubtitles_popup (object sender, PopupMenuArgs e)
		{
			ViewPopupMenu ();	
		}

		void on_downloadedSubtitles_release (object sender, ButtonReleaseEventArgs e)
		{
			if (e.Event.Button != 3)
				ViewPopupMenu ();
		}
		#endregion downloadedSubtitles context menu

		#region Menu activate methods
		void on_quit_activate (object sender, EventArgs e)
		{
			Destroy ();
		}

		void on_showMovieInfo_activate (object sender, EventArgs e)
		{
			var sub = GetSelectedSubtitles ();
			if (sub == null)
				return;
			new System.Threading.Thread (() => {
				string url = "http://www.imdb.com/title/tt" + sub.IdMovieImdb;
				if (sub.IdMovieImdb == string.Empty || !PageExists (url)) {
					Application.Invoke ((s, ev) => Utils.ShowMessageDialog ("Info not available", MessageType.Info));
				} else {
					System.Diagnostics.Process.Start (url);
				}
			}).Start ();
		}

		void on_playVideo_activate (object sender, EventArgs e)
		{
			var sub = GetSelectedSubtitles ();
			if (sub == null)
				return;

			if (Preferences.Instance.Player == string.Empty) {
				Utils.ShowMessageDialog ("Player not configured", MessageType.Info);
				return;
			}

			string args = Preferences.Instance.PlayerArgs.
				Replace ("{0}", "\"" + sub.Video.FileName + "\"").Replace ("{1}", "\"" + sub.CurrentPath + "\""); 
			System.Diagnostics.Process.Start (Preferences.Instance.Player, args);
		}

		void on_copySubtitlesPath_activate (object sender, EventArgs e)
		{
			var sub = GetSelectedSubtitles ();
			if (sub == null)
				return;

			var cp = downloadedSubtitlesView.GetClipboard (Gdk.Selection.Clipboard);
			cp.Text = sub.CurrentPath;
		}
		#endregion Menu activate methods
	}
}
