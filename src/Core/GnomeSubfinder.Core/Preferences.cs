using System;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using System.Linq;


namespace GnomeSubfinder.Core.Core
{
	public class PreferencesConfiguration : ConfigurationSection
	{
		[ConfigurationProperty ("languages", DefaultValue = "")]
		public string Languages {
			get { return GetConfigNode (this ["languages"].ToString (), string.Join (",", LanguageSet.Instance.Languages.Values), x => x != string.Empty); }
			set { this ["languages"] = value; }
		}

		[ConfigurationProperty ("sevenzip-path", DefaultValue = "")]
		public string SevenZipPath {
			get { return GetConfigNode (this ["sevenzip-path"].ToString (), GetDefaultSZipPath (), IsValidExecutable); }
			set { this ["sevenzip-path"] = value; }
		}

		[ConfigurationProperty ("temp-dir-path", DefaultValue = "")]
		public string TempDirPath {
			get { return GetConfigNode (this ["temp-dir-path"].ToString (), Path.GetTempPath (), Directory.Exists); }
			set { this ["temp-dir-path"] = value; }
		}

		[ConfigurationProperty ("override-subtitles", DefaultValue = false)]
		public bool OverrideSubtitles {
			get { return (bool)this ["override-subtitles"]; }
			set { this ["override-subtitles"] = value; }
		}

		[ConfigurationProperty ("down-timeout")]
		public int DownloadingTimeout {
			get { return GetConfigNode (Convert.ToInt32 (this ["down-timeout"]), 3000, x => x > 0); }
			set { this ["down-timeout"] = value; }
		}

		[ConfigurationProperty ("active-tab")]
		public int ActiveTab {
			get { return GetConfigNode (Convert.ToInt32 (this ["active-tab"]), 0, x => Enumerable.Range (0, 2).Contains (x)); }
			set { this ["active-tab"] = value; }
		}

		[ConfigurationProperty ("player")]
		public string Player {
			get { return this ["player"].ToString (); }
			set { this ["player"] = value; }
		}

		[ConfigurationProperty ("player-args")]
		public string PlayerArgs {
			get { return this ["player-args"].ToString (); }
			set { this ["player-args"] = value; }
		}


		static T GetConfigNode<T> (T val, T defaultValue, Func<T, bool> verifyMethod = null)
		{
			if (verifyMethod != null)
				return verifyMethod (val) ? val : defaultValue;
			return val;
		}

		public static string GetDefaultSZipPath ()
		{
			var p = new Process { StartInfo = new ProcessStartInfo ("whereis", "7z") {
					UseShellExecute = false,
					RedirectStandardOutput = true
				}
			};
			p.Start ();

			string output = p.StandardOutput.ReadToEnd ();
			p.WaitForExit ();

			var paths = output.Split (' ');
			const string szPath = "7z";

			if (paths.Length < 2)
				return szPath;

			for (int i = 1; i < paths.Length; i++) {
				if (IsValidExecutable (paths [i]))
					return paths [i];
			}
			return szPath;    
		}

		static bool IsValidExecutable (string path)
		{
			return File.Exists (path);

			// todo On unix systems it can be extended by following code (but it requires Mono.Posix...): 
			// Mono.Unix.Native.Stat buf;
			// Mono.Unix.Native.Syscall.stat (path, out buf);
			// return (int)(buf.st_mode & (Mono.Unix.Native.FilePermissions.S_IXGRP |
			//	Mono.Unix.Native.FilePermissions.S_IXUSR | Mono.Unix.Native.FilePermissions.S_IXOTH)) > 0;
		}
	}

	public class Preferences
	{
		readonly Configuration appSettings;
		readonly PreferencesConfiguration configuration;
		static Preferences instance;

		public static Preferences Instance {
			get { return instance ?? (instance = new Preferences ()); }
		}

		public Preferences ()
		{
			var configFileMap = new ExeConfigurationFileMap { ExeConfigFilename = "gnome-subfinder.config.1" };
			configFileMap.RoamingUserConfigFilename = Path.Combine (
				Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), 
				"gnome-subfinder", configFileMap.ExeConfigFilename
			);

			appSettings = ConfigurationManager.OpenMappedExeConfiguration (configFileMap, ConfigurationUserLevel.PerUserRoaming);

			
			if (!(appSettings.Sections ["settings"] is PreferencesConfiguration)) {
				appSettings.Sections.Add ("settings", new PreferencesConfiguration ());
			}

			configuration = appSettings.Sections ["settings"] as PreferencesConfiguration;
		}

		public void Save ()
		{
			appSettings.Save ();
		}

		public string Languages {
			get { return configuration.Languages; }
			set { configuration.Languages = value; }
		}

		public string SevenZipPath {
			get { return configuration.SevenZipPath; }
			set { configuration.SevenZipPath = value; }
		}

		public string TempDirPath {
			get { return configuration.TempDirPath; }
			set { configuration.TempDirPath = value; }
		}

		public bool OverrideSubtitles {
			get { return configuration.OverrideSubtitles; }
			set { configuration.OverrideSubtitles = value; }
		}

		public int DownloadingTimeout {
			get { return configuration.DownloadingTimeout; }
			set { configuration.DownloadingTimeout = value; }
		}

		public int ActiveTab {
			get { return configuration.ActiveTab; }
			set { configuration.ActiveTab = value; }
		}

		public string Player {
			get { return configuration.Player; }
			set { configuration.Player = value; }
		}

		public string PlayerArgs {
			get { return configuration.PlayerArgs; }
			set { configuration.PlayerArgs = value; }
		}

		public string[] GetSelectedLanguages ()
		{
			return (from lang in Languages.Split (new[] { ',' })
				where lang.EndsWith ("_", StringComparison.Ordinal)
				select lang.Remove (3)).ToArray ();
		}

		public string SelectedLanguages { 
			get { return string.Join (",", GetSelectedLanguages ()); } 
		}

		public string[] GetAllLanguages ()
		{
			return Languages.Replace ("_", "").Split (new []{ ',' });
		}
	}
}

