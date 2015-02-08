using Gtk;
using Gdk;
using Mono.Unix;
using GnomeSubfinder.Core.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Unix.Native;

namespace Subfinder
{
	public class PreferencesDialog
	{
		[Builder.Object]
		readonly Dialog preferencesDialog;
		[Builder.Object]
		readonly TreeView languagesTree;
		[Builder.Object]
		readonly Entry sevenZipPath;
		[Builder.Object]
		readonly ComboBox backendsCombo;
		[Builder.Object]
		readonly Entry tempDirEntry;

		ListStore langsStore;

		public PreferencesDialog ()
		{
			var builder = Subfinder.FromResource ("Subfinder.subfinder-properties.glade");
			builder.Autoconnect (this);
			sevenZipPath.Text = Preferences.Instance.SZipPath;
			tempDirEntry.Text = Preferences.Instance.TemporaryDirectory;
			ConfigureTreeView ();
			ConfigureBackendsCombo ();
		}

		public void Run ()
		{
			if (preferencesDialog.Run () == 1)
			{
				var l = new List<string> ();
				foreach (object[] row in langsStore)
				{
					if ((bool)row [0])
					{
						l.Add (row [3] as string);
					}
				}
	
				Preferences.Instance.Languages = string.Join (",", l);
				Preferences.Instance.SZipPath = sevenZipPath.Text;
				Preferences.Instance.TemporaryDirectory = tempDirEntry.Text;
			}

			preferencesDialog.Destroy ();
		}

		void ConfigureTreeView ()
		{
			langsStore = new ListStore (typeof(bool), typeof(string), typeof(Pixbuf), typeof(string));

			var rendererToggle = new CellRendererToggle ();
			rendererToggle.Toggled += (o, args) => {
				TreeIter iter;
				if (langsStore.GetIterFromString (out iter, args.Path)) {
					bool val = (bool)langsStore.GetValue (iter, 0);
					langsStore.SetValue (iter, 0, !val);
				}
			};
				
			languagesTree.AppendColumn (Catalog.GetString ("Select"), rendererToggle, "active", 0 );
			languagesTree.AppendColumn (Catalog.GetString ("Country"), new CellRendererText (), "text", 1);
			languagesTree.AppendColumn (Catalog.GetString ("Flag"), new CellRendererPixbuf (), "pixbuf", 2 );

			languagesTree.Model = langsStore;

			var langArray = Preferences.Instance.Languages.Split (',');

			foreach (var lang in LanguageSet.Instance.Languages)
			{
				langsStore.AppendValues (langArray.Contains (lang.Value), lang.Key, LanguageSet.Instance.GetFlag (lang.Key, 40, 20), lang.Value);
			}
		}
			
		void ConfigureBackendsCombo ()
		{
			var store = new ListStore(typeof(Pixbuf), typeof (string));
			backendsCombo.Model = store;

			CellRenderer cell = new CellRendererPixbuf ();
			backendsCombo.PackStart(cell, false);
			backendsCombo.AddAttribute(cell, "pixbuf",  0);
			cell = new CellRendererText ();
			backendsCombo.PackStart(cell, false);
			backendsCombo.AddAttribute(cell, "text", 1);

			var mgr = new BackendManager ();
			foreach(var s in mgr.Backends)
				store.AppendValues (s.GetPixbuf (10, 10), s.GetName ());          

			if (store.IterNChildren() > 0)
				backendsCombo.Active = 0;
		}
	}
}

