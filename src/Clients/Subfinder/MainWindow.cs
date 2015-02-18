using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GnomeSubfinder.Core.Core;
using GnomeSubfinder.Core.Interfaces;
using Gtk;
using Mono.Unix;
using System.Net;

namespace Subfinder
{
	public class MainWindow
	{
		[Builder.Object]
		readonly Window window;
		[Builder.Object]
		readonly FileFilter videoFilter;
		[Builder.Object]
		readonly TreeView filesTreeView;
		[Builder.Object]
		readonly TreeView subsTree;
		[Builder.Object]
		readonly AboutDialog aboutDialog;
		[Builder.Object]
		readonly ProgressBar downloadStatus;
		[Builder.Object]
		readonly Viewport treeParent;
		[Builder.Object]
		readonly Button searchButton;
		[Builder.Object]
		readonly Button downloadSelectedButton;
		[Builder.Object]
		readonly Statusbar appStatusbar;
		[Builder.Object]
		readonly Notebook mainNotebook;

		Spinner waitWidget = new Spinner { Visible = true, Active = true };

		TreeStore subtitlesStore;
		ListStore videosStore;
		Builder builder;
		BackendManager controller = new BackendManager ();

		public MainWindow (String[] args)
		{
			builder = Subfinder.FromResource ("Subfinder.subfinder.glade");
			builder.Autoconnect (this);

			ConfigureTreeView ();

			window.Destroyed += (sender, e) => {
				Preferences.Instance.ActiveTab = mainNotebook.CurrentPage;
				Application.Quit ();
			};

			videosStore = new ListStore (typeof(string));
			filesTreeView.AppendColumn ("Path", new CellRendererText (), "text", 0);
			filesTreeView.Model = videosStore;

			mainNotebook.CurrentPage = Preferences.Instance.ActiveTab < 2 ? Preferences.Instance.ActiveTab : 0;
		}

		public void Run ()
		{
			window.Show ();
		}

		void ConfigureTreeView ()
		{
			subtitlesStore = new TreeStore (typeof(bool), typeof(double), typeof(int), typeof(string), typeof(string), typeof(SubtitleFileInfo));

			var rendererToggle = new CellRendererToggle ();
			rendererToggle.Toggled += (o, args) => {
				TreeIter iter;
				if (subtitlesStore.GetIterFromString (out iter, args.Path)) {
					bool val = (bool)subtitlesStore.GetValue (iter, 0);
					subtitlesStore.SetValue (iter, 0, !val);
				}
			};

			var sorter = new TreeModelSort (subtitlesStore);

			var configureColumn = 
				new Action<string, CellRenderer, int, object[]> ((columnName, cellRenderer, columnNo, p) => {
					var cc = subsTree.AppendColumn (columnName, cellRenderer, p);
					cc.Clickable = true;
					cc.Clicked += (sender, e) => {
						int id;
						SortType order;
						sorter.GetSortColumnId (out id, out order);
						sorter.SetSortColumnId (columnNo, order == SortType.Descending || id == -1 ? SortType.Ascending : SortType.Descending);
					};
				});

			configureColumn (Catalog.GetString ("Download?"), rendererToggle, 0, new object[]{ "active", 0 });
			configureColumn (Catalog.GetString ("Rating"), new CellRendererText (), 1, new object[]{ "text", 1 });
			configureColumn (Catalog.GetString ("Downloads count"), new CellRendererText (), 2, new object[]{ "text", 2 });
			configureColumn (Catalog.GetString ("Language"), new CellRendererText (), 3, new object[]{ "text", 3 });
			configureColumn (Catalog.GetString ("Subtitles database"), new CellRendererText (), 4, new object[]{ "text", 4 });

			subsTree.Model = sorter;
		}

		string[] LoadVideoFiles ()
		{
			var videoChooser = new FileChooserDialog (Catalog.GetString ("Choose the file to open"), window, FileChooserAction.Open, new Object[] {
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
				foreach (var f in videoChooser.Files)
					files.Add (f.Path);
			}
			videoChooser.Destroy ();
			return files.ToArray ();
		}

		void OpenBtnClick (object sender, EventArgs e)
		{
			foreach (var f in LoadVideoFiles ()) {
				bool canAdd = true;
				foreach (object[] filename in videosStore) {
					if (filename [0] as string == f) {
						canAdd = false;
						break;
					}
				}
				if (canAdd) {
					videosStore.AppendValues (f);
				}
			}
		}

		void FindSubtitles ()
		{
			foreach (object[] videoFile in videosStore) {
				string filename = videoFile [0] as string;
				try {
					if (!File.Exists (filename))
						throw new IOException (Catalog.GetString ("File ") + filename + Catalog.GetString (" doesn't exists"));

					var f = new VideoFileInfo ();
					f.FileName = filename;
					var x = controller.SearchSubtitles (f, Preferences.Instance.GetSelectedLanguages ());

					var enumerable = x as SubtitleFileInfo[] ?? x.ToArray ();
					Application.Invoke ((e, s)=>{
						TreeIter iter = subtitlesStore.AppendValues(null, null, null, null, Path.GetFileName(filename), null);
					foreach (var sub in enumerable) {
						subtitlesStore.AppendValues (iter, false, sub.Rating, sub.DownloadsCount, sub.Language, sub.Backend.GetName (), sub);
						}});

					if (!enumerable.Any ()) {
						Application.Invoke ((e, s) => ShowMessage (Catalog.GetString ("Subtitles not found")));
					}
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

			new System.Threading.Thread (new System.Threading.ThreadStart (FindSubtitles)).Start ();
		}

		void DownloadSubtitles (SubtitleFileInfo[] subs)
		{
			var downloader = new SubtitleDownloader ();

			foreach (var s in subs)
				downloader.Add (s);

			downloadSelectedButton.Sensitive = false;

			downloader.DownloadStatusChanged += (sdr, evt) => {
				downloadStatus.Text = downloader.Processed + "/" + downloader.Total;
				downloadStatus.Fraction = downloader.Status;
			};

			new System.Threading.Thread (new System.Threading.ThreadStart (() => 
				downloader.Download (Preferences.Instance.TemporaryDirectory, Preferences.Instance.SZipPath))).Start ();

			downloader.DownloadCompleted += (sdr, evt) => Application.Invoke ((sndr, evnt) => {
				downloadSelectedButton.Sensitive = true;
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
			aboutDialog.Show ();
		}

		void PreferencesActiveBtn (object sender, EventArgs e)
		{
			var preferences = new PreferencesDialog (controller);
			preferences.Run ();
		}

		void RemoveVideoBtnClick (object sender, EventArgs e) 
		{
			TreeIter iter;
			TreePath[] treePath = filesTreeView.Selection.GetSelectedRows();

			for (int i  = treePath.Length - 1; i >= 0; i--) {
				if (videosStore.GetIter (out iter, treePath [i])) {
					videosStore.Remove (ref iter);
				}
			}
		}

		void OneClickDownloadBtn (object sender, EventArgs evt)
		{
			var subs = new List<SubtitleFileInfo> ();
			foreach (var filename in LoadVideoFiles ()) {
				try {
					if (!File.Exists (filename))
						throw new IOException (Catalog.GetString ("File ") + filename + Catalog.GetString (" doesn't exists"));

					var langs = Preferences.Instance.GetSelectedLanguages ();
					var x = controller.SearchSubtitles (new VideoFileInfo { FileName = filename }, langs);
					var enumerable = x as SubtitleFileInfo[] ?? x.ToArray ();
					var backends = langs; // todo!
					subs.Add (SubtitleFileInfo.MatchBest (enumerable, langs, backends));
					if (subs.Count == 0) {
						ShowMessage ("Select subtitles first");
						return;
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
	}
}
	