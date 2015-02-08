using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GnomeSubfinder.Core.Core;
using GnomeSubfinder.Core.Interfaces;
using Gtk;
using Mono.Unix;

namespace Subfinder
{
	public class MainWindow
	{
		[Builder.Object]
		readonly Window window;
		[Builder.Object]
		readonly FileFilter videoFilter;
		[Builder.Object]
		readonly Entry videoFileName;
		[Builder.Object]
		readonly TreeView subsTree;
		[Builder.Object]
		readonly AboutDialog aboutDialog;
		[Builder.Object]
		readonly ProgressBar downloadStatus;

		ListStore subtitlesStore;
		Builder builder;
		string userLangs = string.Empty;

		public MainWindow (String[] args)
		{
			builder = Subfinder.FromResource ("Subfinder.subfinder.glade");
			builder.Autoconnect (this);

			ConfigureTreeView ();

			window.Destroyed += (sender, e) => Application.Quit ();
		}

		public void Run ()
		{
			window.Show ();
		}

		void ConfigureTreeView ()
		{
			subtitlesStore = new ListStore (typeof(bool), typeof(double), typeof(int), typeof(string), typeof(string), typeof(SubtitleFileInfo));

			var rendererToggle = new CellRendererToggle ();
			rendererToggle.Toggled += (o, args) => {
				TreeIter iter;
				if (subtitlesStore.GetIterFromString (out iter, args.Path)) {
					bool val = (bool) subtitlesStore.GetValue (iter, 0);
					subtitlesStore.SetValue (iter, 0, !val);
				}
			};

			var sorter = new TreeModelSort (subtitlesStore);

			var configureColumn = 
				new Action<string, CellRenderer, int, object[]>((columnName, cellRenderer, columnNo, p) => {
					var cc = subsTree.AppendColumn (columnName, cellRenderer, p);
					cc.Clickable = true;
					cc.Clicked += (sender, e) => {
						int id;
						SortType order;
						sorter.GetSortColumnId (out id, out order);
						sorter.SetSortColumnId (columnNo, order == SortType.Descending || id == -1 ? SortType.Ascending : SortType.Descending);
					};
				});

			configureColumn (Catalog.GetString ("Download?"), rendererToggle, 0, new object[]{"active", 0});
			configureColumn (Catalog.GetString ("Rating"), new CellRendererText (), 1, new object[]{ "text", 1 });
			configureColumn (Catalog.GetString ("Downloads count"), new CellRendererText (), 2, new object[]{ "text", 2 });
			configureColumn (Catalog.GetString ("Language"), new CellRendererText (), 3, new object[]{ "text", 3 });
			configureColumn (Catalog.GetString ("Subtitles database"), new CellRendererText (), 4, new object[]{ "text", 4 });

			subsTree.Model = sorter;
		}


		void OpenBtnClick (object sender, EventArgs e)
		{
			var videoChooser = new FileChooserDialog (Catalog.GetString ("Choose the file to open"),
				window, FileChooserAction.Open,
				new Object[] {
					Catalog.GetString ("Cancel"), ResponseType.Cancel,
					Catalog.GetString ("Open"), ResponseType.Accept
				});
			videoChooser.Filter = videoFilter;

			if (videoChooser.Run () == (int)ResponseType.Accept) 
			{
				videoFileName.Text = videoChooser.Filename;
			}

			videoChooser.Destroy ();
		}

		void SearchBtnClick (object sender, EventArgs e)
		{
			if (!File.Exists (videoFileName.Text))
				throw new Exception (Catalog.GetString ("File ") + videoFileName.Text + Catalog.GetString ("doesn't exists"));

			var searcher = new BackendManager ();
			var f = new VideoFileInfo ();
			f.FileName = videoFileName.Text;
			var x = searcher.SearchSubtitles (f, "eng");

			var enumerable = x as SubtitleFileInfo[] ?? x.ToArray ();
			foreach (var sub in enumerable) 
			{
				subtitlesStore.AppendValues (false, sub.Rating, sub.DownloadsCount, sub.Language, sub.Backend.GetName (), sub);
			}

			if (!enumerable.Any()) 
			{
				ShowMessage (Catalog.GetString ("Subtitles not found"));
			}
		}

		void DownloadSelectedBtnClick (object sender, EventArgs e)
		{
			var downloader = new SubtitleDownloader ();
			foreach (object[] row in subtitlesStore)
			{
				if ((bool)row [0])
				{
					var s = row [5] as SubtitleFileInfo;
					if (s != null)
						downloader.Add (s);
				}
			}
			downloader.Download ();
			downloader.DownloadStatusChanged += (sdr, evt) => {
				downloadStatus.Text = downloader.Processed + "/" + downloader.Total;
				downloadStatus.Fraction = downloader.Status;
			};

			downloader.DownloadCompleted += (sdr, evt) => Application.Invoke ((sndr, evnt) => ShowMessage (Catalog.GetString ("Download completed!")));
		}

		void ShowAboutActicate(object sender, EventArgs e)
		{
			aboutDialog.Show ();
		}

		void PreferencesActiveBtn (object sender, EventArgs e)
		{
			var preferences = new PreferencesDialog ();
			preferences.Run ();
		}

		static void ShowMessage (string text)
		{
			var md = new MessageDialog (null, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, text);
			md.Run ();
			md.Destroy ();
		}
	}
}
	