using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GnomeSubfinder.Core.Core;
using GnomeSubfinder.Core.DataStructures;
using Gtk;
using Mono.Unix;
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
		[UI] readonly Button oneClickDownloadButton;

		#endregion UI controls

		readonly Spinner waitWidget = new Spinner { Visible = true, Active = true };

		readonly object appenderLocker = new object ();

		readonly TreeStore subtitlesStore = new TreeStore (typeof(bool), typeof(double), typeof(int), typeof(string), typeof(string), typeof(SubtitleFileInfo));
		readonly TreeStore oneClickVideoStore = new TreeStore (typeof(Gdk.Pixbuf), typeof(string), typeof(SubtitleFileInfo));

		readonly BackendManager controller = new BackendManager ();

		SubtitleDownloader downloader = new SubtitleDownloader (Preferences.Instance.DownloadingTimeout);

		public MainWindow (Builder builder, IntPtr handle) : base (handle)
		{
			builder.Autoconnect (this);

			Destroyed += (sender, e) => {
				Preferences.Instance.ActiveTab = mainNotebook.CurrentPage;
				Preferences.Instance.Save ();
				Application.Quit ();
			};

			downloader.DownloadStatusChanged += on_DownloadStatusChanged;
			downloader.DownloadCompleted += on_downloadCompleted;

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

		int FindSubtitlesForSingleVideo (string filename)
		{
			int cnt = 0;
			try {
				if (!File.Exists (filename))
					throw new IOException (string.Format (Catalog.GetString ("File {0} doesn't exist."), filename));

				var languages = Preferences.Instance.GetSelectedLanguages ();
				var subs = controller.SearchSubtitles (new VideoFileInfo { FileName = filename }, languages);
				var bestSubs = SubtitleFileInfo.MatchBest (subs, languages, controller.GetBackendNames ());
				Application.Invoke ((e, s) => {
					TreeIter iter = subtitlesStore.AppendValues (null, null, null, null, System.IO.Path.GetFileName (filename), null);
					foreach (var sub in subs) {
						subtitlesStore.AppendValues (iter, sub == bestSubs, sub.Rating, sub.DownloadsCount, sub.Language, sub.Backend.GetName (), sub);
					}
				});
				cnt = subs.Length;
			} catch (IOException ex) {
				Application.Invoke ((e, s) => Utils.ShowMessageDialog (string.Format (Catalog.GetString ("Error: {0}"), ex.Message), MessageType.Error));
			} catch (WebException ex) {
				Application.Invoke ((e, s) => Utils.ShowMessageDialog (string.Format (Catalog.GetString ("Web exception: {0}"), ex.Message), MessageType.Error));
			} catch (ApplicationException ex) {
				Application.Invoke ((e, s) => Utils.ShowMessageDialog (string.Format (Catalog.GetString ("Application exception: {0}"), ex.Message), MessageType.Error));
			} catch (ArgumentException ex) {
				Application.Invoke ((e, s) => Utils.ShowMessageDialog (string.Format (Catalog.GetString ("Argument exception: {0}"), ex.Message), MessageType.Error));
			}
			return cnt;
		}

		void FindSubtitles ()
		{
			Application.Invoke ((sndr, evnt) => ShowInfo (Catalog.GetString ("Searching subtitles.")));
			int count = 0;
			foreach (object[] videoFile in videosStore) {
				count += FindSubtitlesForSingleVideo (videoFile [0] as string);
			}
			Application.Invoke ((sndr, evnt) => {
				treeParent.Remove (waitWidget);
				treeParent.Add (foundSubtitlesView);
				ActivateButtons (true);
				ShowInfo (string.Format (Catalog.GetString ("Search completed. Found: {0} file(s)."), count));
			});
		}

		TreeIter FindParentIter (SubtitleFileInfo subtitleFile)
		{
			TreeIter ret = TreeIter.Zero;
			oneClickVideoStore.Foreach ((model, path, iter) => {
				var t = oneClickVideoStore.GetValue (iter, 1) as string;
				if (t == subtitleFile.Video.FileName) {
					ret = iter;
					return true;
				}
				return false;
			});
			
			return ret;
		}

		static void ShowAboutActicate (object sender, EventArgs e)
		{
			var builder = new Builder (null, "Subfinder.subfinder.glade", null);
			var dialog = new AboutDialog (builder.GetObject ("aboutDialog").Handle);
			dialog.Run ();
			dialog.Destroy ();
		}

		void ShowInfo (string message)
		{
			appStatusbar.Push (0, message);
		}

		void ShowDownloadedPopupMenu ()
		{
			subtitlesMenu.Show ();
			subtitlesMenu.Popup ();
		}

		static bool PageExists (string url)
		{
			try {
				var request = (HttpWebRequest)WebRequest.Create (url);
				request.Method = WebRequestMethods.Http.Head;
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

		bool LoadVideosToList ()
		{
			var v = LoadVideoFiles ();
			foreach (var f in v) {
				if (videosStore.Cast<object[]> ().All (filename => filename [0] as string != f)) {
					videosStore.AppendValues (f);
				}
			}
			return v.Any ();
		}

		void DownloadSelectedSubtitles ()
		{
			downloader.Clear ();
			oneClickVideoStore.Clear ();

			subtitlesStore.Foreach ((model, path, iter) => {
				if ((bool)model.GetValue (iter, 0)) {
					var s = model.GetValue (iter, 5) as SubtitleFileInfo;
					if (s != null)
						downloader.Add (s);
				}
				return false;
			});

			if (downloader.Count == 0) {
				Utils.ShowMessageDialog (Catalog.GetString ("No subtitles found"), MessageType.Info);
				return;
			}

			ShowInfo (Catalog.GetString ("Downloading subtitles..."));
			ActivateButtons (false);

			downloader.Download ();
		}

		void ActivateButtons (bool lockBtn)
		{
			var buttons = new  []{ downloadSubtitles, oneClickDownloadButton, searchButton };
			foreach (var btn in buttons) {
				btn.Sensitive = lockBtn;
			}
		}

		#region Downloader handlers

		void on_DownloadStatusChanged (object sender, DownloadStatusChangedEventArgs e)
		{
			Application.Invoke ((s, evt) => {
				downloadStatus.Text = downloader.Processed + "/" + downloader.Count;
				downloadStatus.Fraction = downloader.Status;

				lock (appenderLocker) {
					TreeIter parent = FindParentIter (e.SubtitleFile);
					if (parent.Equals (TreeIter.Zero)) {
						parent = oneClickVideoStore.AppendValues (Gdk.Pixbuf.LoadFromResource ("Subfinder.mov.png"),
							e.SubtitleFile.Video.FileName, e.SubtitleFile);
					}
					oneClickVideoStore.AppendValues (parent, 
						Gdk.Pixbuf.LoadFromResource (string.Format ("Subfinder.{0}.png", e.Error ? "bad" : "good")),
						e.SubtitleFile.CurrentPath, e.SubtitleFile);
				}
			});
		}

		void on_downloadCompleted (object sender, EventArgs e)
		{
			Application.Invoke ((sndr, evnt) => {
				ActivateButtons (true);
				ShowInfo (Catalog.GetString ("Download completed"));
				Utils.ShowMessageDialog (Catalog.GetString ("Download completed!"), MessageType.Info);
			});
		}

		#endregion Downloader handlers

		#region Buttons clicked event

		void on_addVideo_clicked (object sender, EventArgs e)
		{
			LoadVideosToList ();
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
			DownloadSelectedSubtitles ();
		}

		void on_oneClickDownload_clicked (object sender, EventArgs evt)
		{
			subtitlesStore.Clear ();
			videosStore.Clear ();

			if (!LoadVideosToList ())
				return;

			ActivateButtons (false);

			new System.Threading.Thread (() => {
				FindSubtitles ();
				Application.Invoke ((s, e) => DownloadSelectedSubtitles ());
			}).Start ();
		}

		void on_searchSubtitles_clicked (object sender, EventArgs e)
		{
			subtitlesStore.Clear ();

			if (videosStore.IterNChildren () == 0) {
				ShowInfo (Catalog.GetString ("Add files first"));
				return;
			}

			ActivateButtons (false);
			treeParent.Remove (foundSubtitlesView);
			treeParent.Add (waitWidget);
			new System.Threading.Thread (FindSubtitles).Start ();
		}

		#endregion Buttons clicked event

		#region downloadedSubtitles context menu

		[GLib.ConnectBefore]
		void on_downloadedSubtitles_popup (object sender, PopupMenuArgs e)
		{
			ShowDownloadedPopupMenu ();	
		}

		void on_downloadedSubtitles_release (object sender, ButtonReleaseEventArgs e)
		{
			if (e.Event.Button == 3)
				ShowDownloadedPopupMenu ();
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
					Application.Invoke ((s, ev) => Utils.ShowMessageDialog (Catalog.GetString ("Info not available"), MessageType.Info));
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

		static void on_preferences_activate (object sender, EventArgs e)
		{
			var builder = new Builder (null, "Subfinder.subfinder-properties.glade", null);
			var preferences = new PreferencesDialog (builder, builder.GetObject ("preferencesDialog").Handle);
			preferences.Run ();
			preferences.Destroy ();
		}

		#endregion Menu activate methods
	}
}
