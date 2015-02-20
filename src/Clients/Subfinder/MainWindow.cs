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
		[UI] readonly FileFilter videoFilter;
		[UI] readonly TreeView filesTreeView;
		[UI] readonly TreeView subsTree;
		[UI] readonly TreeView treeview3;
		[UI] readonly ProgressBar downloadStatus;
		[UI] readonly Viewport treeParent;
		[UI] readonly Button searchButton;
		[UI] readonly Button downloadSelectedButton;
		[UI] readonly Statusbar appStatusbar;
		[UI] readonly Notebook mainNotebook;
		[UI] readonly ListStore videosStore;
		[UI] readonly ScrolledWindow scrolledwindow4;
		[UI] readonly ScrolledWindow scrolledwindow3;
		[UI] readonly Menu subsInfoMenu;

		TreeModelSort sorter;

		readonly Spinner waitWidget = new Spinner { Visible = true, Active = true };

		TreeStore subtitlesStore;
		readonly ListStore oneClickVideoStore;

		readonly BackendManager controller = new BackendManager ();

		public MainWindow (Builder builder, IntPtr handle) : base (handle)
		{
			builder.Autoconnect (this);
			Destroyed += (sender, e) => {
				Preferences.Instance.ActiveTab = mainNotebook.CurrentPage;
				Application.Quit ();
			};

			ConfigureTreeView ();

			mainNotebook.SwitchPage += (o, args) => treeview3.Reparent (args.PageNum == 0 ? scrolledwindow4 : scrolledwindow3);

			oneClickVideoStore = new ListStore (typeof(Gdk.Pixbuf), typeof(string), typeof(SubtitleFileInfo));
			treeview3.Model = oneClickVideoStore;
			mainNotebook.CurrentPage = Preferences.Instance.ActiveTab < 2 ? Preferences.Instance.ActiveTab : 0;
		}

		void ConfigureTreeView ()
		{
			subtitlesStore = new TreeStore (typeof(bool), typeof(double), typeof(int), typeof(string), typeof(string), typeof(SubtitleFileInfo));
			sorter = new TreeModelSort (subtitlesStore);
			subsTree.Model = sorter;
		}

		void ColumnClicked (object sender, EventArgs e)
		{
			int id;
			SortType order;
			sorter.GetSortColumnId (out id, out order);
			sorter.SetSortColumnId (Array.IndexOf (subsTree.Columns, sender), order == SortType.Descending || id == -1 ? SortType.Ascending : SortType.Descending);
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

		void OpenBtnClick (object sender, EventArgs e)
		{
			foreach (var f in LoadVideoFiles ()) {
				bool canAdd = videosStore.Cast<object[]> ().All (filename => filename [0] as string != f);
				if (canAdd) {
					videosStore.AppendValues (f);
				}
			}
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
					Application.Invoke ((e, s) => ShowMessage ("Error: " + ex.Message));
				} catch (WebException ex) {
					Application.Invoke ((e, s) => ShowMessage ("Web exception: " + ex.Message));
				}
			}
			Application.Invoke ((sndr, evnt) => {
				treeParent.Remove (waitWidget);
				treeParent.Add (subsTree);
				searchButton.Sensitive = true;
				ShowInfo (String.Format ("Search completed. Found: {0} file(s).", count));
			});
		}

		void SearchBtnClick (object sender, EventArgs e)
		{
			if (videosStore.IterNChildren () == 0) {
				ShowInfo ("Add files first");
				return;
			}

			subtitlesStore.Clear ();

			searchButton.Sensitive = false;
			treeParent.Remove (subsTree);
			treeParent.Add (waitWidget);
			ShowInfo ("Searching subtitles.");
			new System.Threading.Thread (FindSubtitles).Start ();
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
			downloadSelectedButton.Sensitive = false;

			downloader.DownloadStatusChanged += (sdr, evt) => Application.Invoke ((sndr, evnt) => {
				downloadStatus.Text = downloader.Processed + "/" + downloader.Total;
				downloadStatus.Fraction = downloader.Status;
				oneClickVideoStore.AppendValues (
					Gdk.Pixbuf.LoadFromResource (string.Format ("Subfinder.Resources.{0}.png", evt.Error ? "bad" : "good")),
					evt.SubtitleFile.Video.FileName, evt.SubtitleFile);
			});

			new System.Threading.Thread (downloader.Download).Start ();

			downloader.DownloadCompleted += (sdr, evt) => Application.Invoke ((sndr, evnt) => {
				downloadSelectedButton.Sensitive = true;
				ShowInfo ("Download completed");
				ShowMessage (Catalog.GetString ("Download completed!"));
			});
		}

		void DownloadSelectedBtnClick (object sender, EventArgs e)
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
				ShowMessage ("Select subtitles first");
				return;
			}

			DownloadSubtitles (subs.ToArray ());
		}

		void ShowAboutActicate (object sender, EventArgs e)
		{
			var builder = new Builder (null, "Subfinder.subfinder.glade", null);
			var dialog = new AboutDialog (builder.GetObject ("aboutDialog").Handle);
			dialog.Run ();
			dialog.Destroy ();
		}

		void PreferencesActiveBtn (object sender, EventArgs e)
		{
			var builder = new Builder (null, "Subfinder.subfinder-properties.glade", null);
			var preferences = new PreferencesDialog (controller, builder, builder.GetObject ("preferencesDialog").Handle);
			preferences.Run ();
			preferences.Destroy ();
		}

		void RemoveVideoBtnClick (object sender, EventArgs e)
		{
			TreePath[] treePath = filesTreeView.Selection.GetSelectedRows ();

			for (int i = treePath.Length - 1; i >= 0; i--) {
				TreeIter iter;
				if (videosStore.GetIter (out iter, treePath [i])) {
					videosStore.Remove (ref iter);
				}
			}
		}

		void OneClickDownloadBtn (object sender, EventArgs evt)
		{
			var subs = new List<SubtitleFileInfo> ();
			var files = LoadVideoFiles ();
			try {
				foreach (var filename in files) {
					ShowInfo ("Searching subtitles for file " + filename);
					if (!File.Exists (filename))
						throw new IOException (Catalog.GetString ("File ") + filename + Catalog.GetString (" doesn't exists"));

					var langs = Preferences.Instance.GetSelectedLanguages ();
					var backends = langs; // todo!
					subs.Add (SubtitleFileInfo.MatchBest (controller.SearchSubtitles (new VideoFileInfo { FileName = filename }, langs), langs, backends));
				}
				DownloadSubtitles (subs.ToArray ());
			} catch (IOException ex) {
				Application.Invoke ((e, s) => ShowMessage ("Error: " + ex.Message));
			} catch (WebException ex) {
				Application.Invoke ((e, s) => ShowMessage ("Web exception: " + ex.Message));
			} catch (ArgumentException ex) {
				Application.Invoke ((e, s) => ShowMessage ("Subtitles not found: " + ex.Message));
			}
		}

		static void ShowMessage (string text)
		{
			var md = new MessageDialog (null, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, text);
			md.Run ();
			md.Destroy ();
		}

		void ShowInfo (string message)
		{
			appStatusbar.Push (0, message);
		}

		[GLib.ConnectBefore]
		void OnPopUpMenu (object sender, ButtonReleaseEventArgs e)
		{
			if (e.Event.Type != Gdk.EventType.ButtonRelease ||
			    e.Event.Button != 3)
				return;

			ViewPopupMenu ();	
		}

		void PopupMenuNoClick (object sender, PopupMenuArgs e)
		{
			ViewPopupMenu ();
		}

		void ViewPopupMenu ()
		{
			if (treeview3.Selection.CountSelectedRows () == 0)
				return;

			subsInfoMenu.Show ();
			subsInfoMenu.Popup ();
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

		SubtitleFileInfo GetSelected ()
		{
			TreeIter iter;
			treeview3.Selection.GetSelected (out iter);
			return treeview3.Model.GetValue (iter, 2) as SubtitleFileInfo;
		}

		void ShowMovieInfo (object sender, EventArgs e)
		{
			var sub = GetSelected ();
			if (sub == null)
				return;
			new System.Threading.Thread (() => {
				string url = "http://www.imdb.com/title/tt" + sub.IdMovieImdb;
				if (sub.IdMovieImdb == string.Empty || !PageExists (url)) {
					Application.Invoke ((s, ev) => ShowMessage ("Info not available"));
				} else {
					System.Diagnostics.Process.Start (url);
				}
			}).Start ();
		}

		void PlayVideoFile (object sender, EventArgs e)
		{
			var sub = GetSelected ();
			if (sub == null)
				return;

			if (Preferences.Instance.Player == string.Empty) {
				ShowMessage ("Player not configured");
				return;
			}

			string args = Preferences.Instance.PlayerArgs.Replace ("{0}", "\"" + sub.Video.FileName + "\"").Replace ("{1}", "\"" + sub.CurrentPath + "\""); 
			System.Diagnostics.Process.Start (Preferences.Instance.Player, args);
		}

		void CopyToClipboard (object sender, EventArgs e)
		{
			var sub = GetSelected ();
			if (sub == null)
				return;

			var cp = treeview3.GetClipboard (Gdk.Selection.Clipboard);
			cp.Text = sub.CurrentPath;
		}

		void QuitClick (object sender, EventArgs e)
		{
			Destroy ();
		}
	}
}
