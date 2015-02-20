using System;
using System.Collections.Generic;
using System.Linq;
using Gdk;
using GnomeSubfinder.Core.Core;
using GnomeSubfinder.Core.GUIHelper;
using Gtk;

using UI = Gtk.Builder.ObjectAttribute;

namespace Subfinder
{
	public class PreferencesDialog : Dialog
	{
		[UI] readonly TreeView languagesTree;
		[UI] readonly Entry sevenZipPath;
		[UI] readonly ComboBox backendsCombo;
		[UI] readonly Entry tempDirEntry;
		[UI] readonly SpinButton timeoutSpinButton;
		[UI] readonly CheckButton overrideSubtitlesCheckButton;
		[UI] readonly Entry playerEntry;
		[UI] readonly Entry playerArgsEntry;

		ListStore langsStore;
		readonly BackendManager controller;

		public PreferencesDialog (BackendManager controller, Builder builder, IntPtr handle) : base (handle)
		{
			this.controller = controller;
			builder.Autoconnect (this);
			sevenZipPath.Text = Preferences.Instance.SZipPath;
			tempDirEntry.Text = Preferences.Instance.TemporaryDirectory;
			timeoutSpinButton.Value = Preferences.Instance.DownloadingTimeout / 1000.0;
			overrideSubtitlesCheckButton.Active = Preferences.Instance.OverrideSubtitles;
			playerEntry.Text = Preferences.Instance.Player;
			playerArgsEntry.Text = Preferences.Instance.PlayerArgs;

			langsStore = new ListStore (typeof(bool), typeof(string), typeof(Pixbuf), typeof(string));

			languagesTree.Model = langsStore;

			FillLangsTree ();
			ConfigureBackendsCombo ();

			Response += (sender, e) => {
				if ((int)e.ResponseId == 1) { 
					var l = new List<string> ();
					foreach (object[] row in langsStore) {
						string s = row [3] as string;
						if ((bool)row [0])
							s += "_";
						l.Add (s);
					}

					Preferences.Instance.Languages = string.Join (",", l);
					Preferences.Instance.SZipPath = sevenZipPath.Text;
					Preferences.Instance.TemporaryDirectory = tempDirEntry.Text;
					Preferences.Instance.DownloadingTimeout = (int)(timeoutSpinButton.Value * 1000);
					Preferences.Instance.OverrideSubtitles = overrideSubtitlesCheckButton.Active;
					Preferences.Instance.Player = playerEntry.Text;
					Preferences.Instance.PlayerArgs = playerArgsEntry.Text;
					Preferences.Instance.Save ();
				}
				Destroy ();
			};
		}

		void CellToggled (object sender, ToggledArgs e)
		{ 
			TreeIter iter;
			if (langsStore.GetIterFromString (out iter, e.Path)) {
				bool val = (bool)langsStore.GetValue (iter, 0);
				langsStore.SetValue (iter, 0, !val);
			}	
		}

		void FillLangsTree ()
		{
			var selectedLangs = Preferences.Instance.GetSelectedLanguages ();
			var allLangs = Preferences.Instance.GetAllLanguages ();
			foreach (var lang in allLangs) {
				bool selected = selectedLangs.Contains (lang);
				string langName = LanguageSet.Instance.Languages.FirstOrDefault (x => x.Value == lang).Key;
				langsStore.AppendValues (selected, langName, LanguageSet.Instance.GetFlag (langName, 40, 20), lang);
			}
		}

		void ConfigureBackendsCombo ()
		{
			var store = new ListStore (typeof(Pixbuf), typeof(string));
			backendsCombo.Model = store;

			foreach (var s in controller.Backends) {
				var assemblyInfo = s.GetLogoAssemblyInfo ();
				store.AppendValues (new Gdk.Pixbuf (assemblyInfo.Item1, assemblyInfo.Item2), s.GetName ());          
			}
			if (store.IterNChildren () > 0)
				backendsCombo.Active = 0;
		}

		void MoveItem (int position, bool absolute)
		{
			if (languagesTree.Selection.GetSelectedRows ().Length == 0)
				return;

			TreeIter iter, tmpIter;
			languagesTree.Selection.GetSelected (out tmpIter);
			int pos = absolute ? position : position + Convert.ToInt32 (langsStore.GetPath (tmpIter).ToString ());

			if (pos == -1 || pos >= langsStore.IterNChildren ())
				return;

			langsStore.GetIterFromString (out iter, pos.ToString ());
			if (position <= 0)
				langsStore.MoveBefore (tmpIter, iter);
			else
				langsStore.MoveAfter (tmpIter, iter);
		}

		void TopBtnClick (object sender, EventArgs e)
		{
			MoveItem (0, true);
		}

		void BottomBtnClick (object sender, EventArgs e)
		{
			MoveItem (langsStore.IterNChildren () - 1, true);
		}

		void UpBtnClick (object sender, EventArgs e)
		{
			MoveItem (-1, false);
		}

		void DownBtnClick (object sender, EventArgs e)
		{
			MoveItem (1, false);
		}
	}
}

