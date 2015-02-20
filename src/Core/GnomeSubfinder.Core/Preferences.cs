﻿using System;
using System.Diagnostics;
using System.IO;
using Mono.Unix.Native;
using System.Collections.Generic;
using System.Configuration;


namespace GnomeSubfinder.Core.Core
{
	public class PreferencesConfiguration : ConfigurationSection
	{
		static ConfigurationPropertyCollection properties;

		static readonly ConfigurationProperty _SZPath = new ConfigurationProperty(Preferences.SZIP_PATH_KEY, 
			typeof(string), Preferences.GetDefaultSZipPath ());

		static readonly ConfigurationProperty _TmpDirPath = new ConfigurationProperty(Preferences.TEMP_DIR_PATH_KEY, 
			typeof(string), Path.GetTempPath ());


		static readonly ConfigurationProperty _Languages = new ConfigurationProperty(Preferences.LANGUAGES_KEY, 
			typeof(string), string.Empty);


		static readonly ConfigurationProperty _ActiveTab = new ConfigurationProperty(Preferences.ACTIVE_TAB_KEY, 
			typeof(int), 0);


		static readonly ConfigurationProperty _DownTimeout = new ConfigurationProperty(Preferences.DOWN_TIMEOUT_PATH_KEY, 
			typeof(int), 3000);
			

		public PreferencesConfiguration ()
		{	
			properties = new ConfigurationPropertyCollection();

			properties.Add(_SZPath);
			properties.Add(_TmpDirPath);
			properties.Add(_Languages);
			properties.Add(_ActiveTab);
			properties.Add(_DownTimeout);
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get { return properties; }
		}

		public new object this [string index] {
				get { return base [index]; }
			set{ base [index] = value; }
		}
	}

	public class Preferences
	{
		public const string SZIP_PATH_KEY = "sevenzip-path";
		public const string TEMP_DIR_PATH_KEY = "temp-dir-path";
		public const string LANGUAGES_KEY = "languages";
		public const string ACTIVE_TAB_KEY = "active-tab";
		public const string DOWN_TIMEOUT_PATH_KEY = "down-timeout";

		PreferencesConfiguration section;
		Configuration appSettings;
		static Preferences instance;

		public static Preferences Instance {
			get {
				if (instance == null)
					instance = new Preferences ();
				return instance;
			}
		}

		public Preferences()
		{
			appSettings = ConfigurationManager.OpenExeConfiguration (ConfigurationUserLevel.PerUserRoamingAndLocal);
			
			if (appSettings.Sections ["settings"] == null || !(appSettings.Sections ["settings"] is PreferencesConfiguration)) {
				appSettings.Sections.Add ("settings", new PreferencesConfiguration ());
			}
			section = appSettings.Sections ["settings"] as PreferencesConfiguration;
		}

		public void Save()
		{
			appSettings.Save ();
		}



		public int ActiveTab {
			get { return GetGConfNode (ACTIVE_TAB_KEY, 0); }
			set { SetGConfNode (ACTIVE_TAB_KEY, value); }
		}

		public string TemporaryDirectory {
			get { return GetGConfNode (TEMP_DIR_PATH_KEY, Path.GetTempPath (), Directory.Exists); }
			set { SetGConfNode (TEMP_DIR_PATH_KEY, value, Directory.Exists); }
		}

		public string SZipPath {
			get { return GetGConfNode (SZIP_PATH_KEY, GetDefaultSZipPath (), IsValidExecutable); }
			set { SetGConfNode (SZIP_PATH_KEY, value, IsValidExecutable); }
		}

		public int DownloadingTimeout {
			get { return GetGConfNode (DOWN_TIMEOUT_PATH_KEY, 3000, x => x > 0); }
			set { SetGConfNode (DOWN_TIMEOUT_PATH_KEY, value, x => x > 0); }
		}

		public string Languages {
			private get { return GetGConfNode (LANGUAGES_KEY, LanguageSet.Instance.JoinedLanguages, x=> x!= string.Empty); }
			set { SetGConfNode (LANGUAGES_KEY, value, x => x != string.Empty); }
		}

		public string[] GetSelectedLanguages ()
		{
			var l = new List<string> ();
			foreach (var lang in Languages.Split(new []{','})) {
				if (lang.EndsWith ("_"))
					l.Add (lang.Remove(3));
			}
			return l.ToArray ();
		}

		public string SelectedLanguages { 
			get { return string.Join (",", GetSelectedLanguages ()); } 
		}

		public string[] GetAllLanguages ()
		{
			return Languages.Replace ("_", "").Split (new []{ ',' });
		}

		T GetGConfNode<T> (string nodePath, T defaultValue, Func<T, bool> verifyMethod = null)
		{
			var val = (T)section [nodePath];
			if (verifyMethod != null)
				return verifyMethod (val) ? val : defaultValue;
			return val;
		}

		bool SetGConfNode<T> (string nodePath, T value, Func<T, bool> verifyMethod = null)
		{
			if (verifyMethod != null && !verifyMethod (value))
				return false;

			section [nodePath] = value;
			return true;	
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
			string szPath = "7z";

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
			if (!File.Exists (path))
				return false;

			Stat buf;
			Syscall.stat (path, out buf);
			return (int)(buf.st_mode & (FilePermissions.S_IXGRP | FilePermissions.S_IXUSR | FilePermissions.S_IXOTH)) > 0;
		}
	}
}

