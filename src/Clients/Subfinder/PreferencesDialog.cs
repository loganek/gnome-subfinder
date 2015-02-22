using System;
using System.Collections.Generic;
using System.Globalization;
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
		[UI] readonly TreeView nonSelectedLanguagesView;
		[UI] readonly TreeView selectedLanguagesView;
		[UI] readonly Entry sevenZipPath;
		[UI] readonly ComboBox backendsCombo;
		[UI] readonly Entry tempDirEntry;
		[UI] readonly SpinButton timeoutSpinButton;
		[UI] readonly CheckButton overrideSubtitlesCheckButton;
		[UI] readonly Entry playerEntry;
		[UI] readonly Entry playerArgsEntry;

		readonly ListStore selectedLanguages;
		readonly ListStore nonSelectedLanguages;

		public PreferencesDialog (Builder builder, IntPtr handle) : base (handle)
		{
			builder.Autoconnect (this);

			selectedLanguages = new ListStore (typeof(string), typeof(Pixbuf), typeof(string));
			nonSelectedLanguages = new ListStore (typeof(string), typeof(Pixbuf), typeof(string));

			LoadPreferences ();

			Response += (sender, e) => {
				if ((int)e.ResponseId == 1) { 
					SavePreferences ();
				}
				Destroy ();
			};
		}

		void SavePreferences ()
		{
			Func<ListStore, bool, List<string>> appendLangs = (langStore, selected) => {
				var l = new List<string> ();
				foreach (object[] row in langStore) {
					var s = row [2] as string;
					if (selected)
						s += "_";
					l.Add (s);
				}
				return l;
			};

			Preferences.Instance.Languages = 
				string.Join (",", appendLangs (selectedLanguages, true).Concat (appendLangs (nonSelectedLanguages, false)));
			Preferences.Instance.SevenZipPath = sevenZipPath.Text;
			Preferences.Instance.TempDirPath = tempDirEntry.Text;
			Preferences.Instance.DownloadingTimeout = (int)(timeoutSpinButton.Value * 1000);
			Preferences.Instance.OverrideSubtitles = overrideSubtitlesCheckButton.Active;
			Preferences.Instance.Player = playerEntry.Text;
			Preferences.Instance.PlayerArgs = playerArgsEntry.Text;
			Preferences.Instance.Save ();
		}

		void LoadPreferences ()
		{
			sevenZipPath.Text = Preferences.Instance.SevenZipPath;
			tempDirEntry.Text = Preferences.Instance.TempDirPath;
			timeoutSpinButton.Value = Preferences.Instance.DownloadingTimeout / 1000.0;
			overrideSubtitlesCheckButton.Active = Preferences.Instance.OverrideSubtitles;
			playerEntry.Text = Preferences.Instance.Player;
			playerArgsEntry.Text = Preferences.Instance.PlayerArgs;

			nonSelectedLanguagesView.Model = nonSelectedLanguages;
			selectedLanguagesView.Model = selectedLanguages;

			FillLangsTree ();
		}

		void FillLangsTree ()
		{
			foreach (var lang in Preferences.Instance.GetAllLanguages ()) {
				bool selected = Preferences.Instance.GetSelectedLanguages ().Contains (lang);
				string langName = LanguageSet.Instance.Languages.FirstOrDefault (x => x.Value == lang).Key;
				var store = selected ? selectedLanguages : nonSelectedLanguages;
				store.AppendValues (langName, LanguageSet.Instance.GetFlag (langName, 40, 20), lang);
			}
		}
			
		static TreeIter GetSelectedIter(TreeView tree)
		{
			if (tree.Selection.GetSelectedRows ().Length == 0)
				return TreeIter.Zero;

			TreeIter iter;
			tree.Selection.GetSelected (out iter);

			return iter;
		}

		void MoveItem (int position, bool absolute)
		{
			TreeIter tmpIter = GetSelectedIter (selectedLanguagesView);
			if (tmpIter.Equals(TreeIter.Zero))
				return;
			TreeIter iter;
			int pos = absolute ? position : position + Convert.ToInt32 (selectedLanguages.GetPath (tmpIter).ToString ());

			if (pos == -1 || pos >= selectedLanguages.IterNChildren ())
				return;

			selectedLanguages.GetIterFromString (out iter, pos.ToString (CultureInfo.InvariantCulture));
			if (position <= 0)
				selectedLanguages.MoveBefore (tmpIter, iter);
			else
				selectedLanguages.MoveAfter (tmpIter, iter);
		}

		void TopBtnClick (object sender, EventArgs e)
		{
			MoveItem (0, true);
		}

		void BottomBtnClick (object sender, EventArgs e)
		{
			MoveItem (selectedLanguages.IterNChildren () - 1, true);
		}

		void UpBtnClick (object sender, EventArgs e)
		{
			MoveItem (-1, false);
		}

		void DownBtnClick (object sender, EventArgs e)
		{
			MoveItem (1, false);
		}

		static void SwapLanguages(TreeView t, ListStore l1, ListStore l2)
		{
			var iter = GetSelectedIter (t);
			if (iter.Equals(TreeIter.Zero))
				return;

			l2.AppendValues (l1.GetValue (iter, 0), l1.GetValue (iter, 1), l1.GetValue (iter, 2));
			l1.Remove (ref iter);
		}

		void SelectLanguage (object sender, EventArgs e)
		{
			SwapLanguages (nonSelectedLanguagesView, nonSelectedLanguages, selectedLanguages);
		}

		void DeselectLanguage (object sender, EventArgs e)
		{
			SwapLanguages (selectedLanguagesView, selectedLanguages, nonSelectedLanguages);
		}
	}
}

