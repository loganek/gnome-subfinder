using System;
using GConf;
using System.Diagnostics;
using System.IO;
using Mono.Unix.Native;

namespace GnomeSubfinder.Core.Core
{
	public class Preferences
	{
		readonly Client client = new Client ();
		const string GCONF_APP_PATH = "/apps/gnome-subfinder";
		const string SZIP_PATH_KEY = GCONF_APP_PATH + "/7zip-path";
		const string TEMP_DIR_PATH_KEY = GCONF_APP_PATH + "/temp-dir-path";
		const string LANGUAGES_KEY = GCONF_APP_PATH + "/languages";
		const string ACTIVE_TAB_KEY = GCONF_APP_PATH + "/active_tab";

		static Preferences instance;

		public static Preferences Instance {
			get {
				if (instance == null)
					instance = new Preferences ();
				return instance;
			}
		}

		Preferences ()
		{
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

		public string Languages {
			get { return GetGConfNode (LANGUAGES_KEY, string.Empty); }
			set { SetGConfNode (LANGUAGES_KEY, value); }
		}


		T GetGConfNode<T> (string nodePath, T defaultValue, Func<T, bool> verifyMethod = null)
		{
			try {
				var val = (T)client.Get (nodePath);
				if (verifyMethod != null)
					return verifyMethod (val) ? val : defaultValue;
				return val;
			} catch {
				// todo log
				return defaultValue;
			}
		}

		bool SetGConfNode<T> (string nodePath, T value, Func<T, bool> verifyMethod = null)
		{
			if (verifyMethod != null && !verifyMethod (value))
				return false;

			client.Set (nodePath, value);
			return true;	
		}

		static string GetDefaultSZipPath ()
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

