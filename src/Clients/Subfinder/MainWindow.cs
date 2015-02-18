﻿using System;
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

			window.Destroyed += (sender, e) => Application.Quit ();

			videosStore = new ListStore (typeof(string));
			filesTreeView.AppendColumn ("Path", new CellRendererText (), "text", 0);
			filesTreeView.Model = videosStore;
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


		void OpenBtnClick (object sender, EventArgs e)
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

			if (videoChooser.Run () == (int)ResponseType.Accept) {
				foreach (var f in videoChooser.Files)
					videosStore.AppendValues (f.Path);
			}

			videoChooser.Destroy ();
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
					var x = controller.SearchSubtitles (f, Preferences.Instance.Languages);

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
				} finally {
					Application.Invoke ((sndr, evnt) => {
						treeParent.Remove (waitWidget);
						treeParent.Add (subsTree);
						searchButton.Sensitive = true;
					});
				}
			}
		}

		void SearchBtnClick (object sender, EventArgs e)
		{
			subtitlesStore.Clear ();

			searchButton.Sensitive = false;
			treeParent.Remove (subsTree);
			treeParent.Add (waitWidget);

			new System.Threading.Thread (new System.Threading.ThreadStart (FindSubtitles)).Start ();
		}

		void DownloadSelectedBtnClick (object sender, EventArgs e)
		{
			downloadSelectedButton.Sensitive = false;
			var downloader = new SubtitleDownloader ();
			subtitlesStore.Foreach((model, path, iter) => {
				if ((bool)model.GetValue(iter, 0)) {
					var s = model.GetValue(iter, 5) as SubtitleFileInfo;
					if (s != null)
						downloader.Add (s);
				}
				return false;
			});
				
			downloader.DownloadStatusChanged += (sdr, evt) => {
				downloadStatus.Text = downloader.Processed + "/" + downloader.Total;
				downloadStatus.Fraction = downloader.Status;
			};

			new System.Threading.Thread (new System.Threading.ThreadStart (() => 
				downloader.Download(Preferences.Instance.TemporaryDirectory, Preferences.Instance.SZipPath))).Start ();

			downloader.DownloadCompleted += (sdr, evt) => Application.Invoke ((sndr, evnt) => {
				downloadSelectedButton.Sensitive = true;
				ShowMessage (Catalog.GetString ("Download completed!"));
			});
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

		static void ShowMessage (string text)
		{
			var md = new MessageDialog (null, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, text);
			md.Run ();
			md.Destroy ();
		}
	}
}
	