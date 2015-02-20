using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using System.Linq;


namespace GnomeSubfinder.Core.Core
{
	public class PreferencesConfiguration : ConfigurationSection
	{
        [ConfigurationProperty("languages", DefaultValue = "")]
        public string Languages
        {
            get { return this["languages"].ToString(); }
            set { this["languages"] = value; }
        }

        [ConfigurationProperty("sevenzip-path", DefaultValue = "")]
        public string SevenZipPath
        {
            get { return this["sevenzip-path"].ToString(); }
            set { this["sevenzip-path"] = value; }
        }

        [ConfigurationProperty("temp-dir-path", DefaultValue = "")]
        public string TempDirPath
        {
            get { return this["temp-dir-path"].ToString(); }
            set { this["temp-dir-path"] = value; }
        }

        [ConfigurationProperty("override-subtitles", DefaultValue = false)]
        public bool OverrideSubtitles
        {
            get { return (bool) this["override-subtitles"]; }
            set { this["override-subtitles"] = value; }
        }

        [ConfigurationProperty("down-timeout", DefaultValue = 3000)]
        public int DownTimeout
        {
            get { return Convert.ToInt32(this["down-timeout"]); }
            set { this["down-timeout"] = value; }
        }
	}

	public class Preferences
	{
		public const string SZIP_PATH_KEY = "sevenzip-path";
		public const string TEMP_DIR_PATH_KEY = "temp-dir-path";
		public const string LANGUAGES_KEY = "languages";
		public const string ACTIVE_TAB_KEY = "active-tab";
		public const string DOWN_TIMEOUT_PATH_KEY = "down-timeout";
		public const string OVERRIDE_SUBTITLES_PATH_KEY = "override-subtitles";
		public const string PLAYER_PATH_KEY = "player";
		public const string PLAYER_ARGS_PATH_KEY = "player-args";

	    readonly PreferencesConfiguration section;
	    readonly Configuration appSettings;
		static Preferences instance;

		public static Preferences Instance {
			get { return instance ?? (instance = new Preferences()); }
		}

		public Preferences()
		{
			var configFileMap = new ExeConfigurationFileMap {ExeConfigFilename = "gnome-subdownloader.config.1"};
		    configFileMap.RoamingUserConfigFilename = Path.Combine (
				Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), 
				"gnome-subdownloader", configFileMap.ExeConfigFilename
			);

			appSettings = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.PerUserRoaming);

			
			if (!(appSettings.Sections ["settings"] is PreferencesConfiguration)) {
				appSettings.Sections.Add ("settings", new PreferencesConfiguration ());
			}
			section = appSettings.Sections ["settings"] as PreferencesConfiguration;
		}

		public void Save()
		{
			appSettings.Save ();
		}



		public int ActiveTab {
			get { return GetConfigNode (ACTIVE_TAB_KEY, 0); }
			set { SetConfigNode (ACTIVE_TAB_KEY, value); }
		}

		public string TemporaryDirectory {
			get { return section.TempDirPath; }
			set { section.TempDirPath = value; }
		}

	    public string SZipPath
	    {
	        get { return section.SevenZipPath; }
	        set { section.SevenZipPath = value; }
	    }

	    public bool OverrideSubtitles {
			get { return section.OverrideSubtitles; }
			set { section.OverrideSubtitles = value; }
		}

		public int DownloadingTimeout {
            get { return section.DownTimeout; }
            set { section.DownTimeout = value; }
		}

		public string Player {
            get { return ""; }
            set {  }
		}

		public string PlayerArgs {
            get { return ""; }
            set { }
		}

		public string Languages {
			private get { return section.Languages; }
			set { section.Languages = value; }
		}

		public string[] GetSelectedLanguages ()
		{
		    return (from lang in Languages.Split(new[] {','}) where lang.EndsWith("_") select lang.Remove(3)).ToArray();
		}

		public string SelectedLanguages { 
			get { return string.Join (",", GetSelectedLanguages ()); } 
		}

		public string[] GetAllLanguages ()
		{
			return Languages.Replace ("_", "").Split (new []{ ',' });
		}

		T GetConfigNode<T> (string nodePath, T defaultValue, Func<T, bool> verifyMethod = null) where T : new()
		{
		    var val = new T();//(T)section [nodePath];
			if (verifyMethod != null)
				return verifyMethod (val) ? val : defaultValue;
			return val;
		}

		void SetConfigNode<T> (string nodePath, T value, Func<T, bool> verifyMethod = null)
		{
			if (verifyMethod != null && !verifyMethod (value))
			    return;

		    //section [nodePath] = value;
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
}

