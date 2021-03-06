﻿using System;
using System.IO;
using Gtk;
using Mono.Unix;
using SubfinderConsole;

namespace Subfinder
{
	public class Subfinder
	{
		public static string LocaleDir {
			get {
				string applicationPrefix = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);
				var pathWithLocale = Path.Combine (applicationPrefix, "locale");
				if (Directory.Exists (pathWithLocale))
					return pathWithLocale;
				var entry_directory = new DirectoryInfo (applicationPrefix);
				if (entry_directory != null && entry_directory.Parent != null && entry_directory.Parent.Parent != null)
					applicationPrefix = entry_directory.Parent.Parent.FullName;
				return Path.Combine (applicationPrefix, "share", "locale");
			}
		}

		static void Main (String[] args)
		{
			var optParser = new OptionParser<Options> (new Options (), args);
			try {
				optParser.Parse ();
			} catch (ArgumentException ex) {
				Console.WriteLine (string.Format (Catalog.GetString ("Cannot parse arguments: {0}"), ex.Message));
				return;
			}
				
			if (optParser.OptionsObject.Console) {
				new SubfinderConsole.SubfinderConsole (optParser).Run ();
				return;
			}

			Application.Init ();
			Catalog.Init ("gnome-subfinder", LocaleDir);

			var builder = new Builder (null, "Subfinder.subfinder.glade", null);
			var win = new MainWindow (builder, builder.GetObject ("window").Handle);
			win.Show ();

			Application.Run ();
		}
	}
}

