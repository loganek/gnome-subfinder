using System;
using System.IO;
using Gtk;
using Mono.Unix;

namespace Subfinder
{
	public class Subfinder
	{
		public static string LocaleDir {
			get {
				string installed_application_prefix = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);
				if (Directory.Exists (Path.Combine (installed_application_prefix, "share", "Subfinder")))
					return installed_application_prefix;
				var entry_directory = new DirectoryInfo (installed_application_prefix);
				if (entry_directory != null && entry_directory.Parent != null && entry_directory.Parent.Parent != null)
					installed_application_prefix = entry_directory.Parent.Parent.FullName;
				return Path.Combine (installed_application_prefix, "share", "locale");
			}
		}

		static void Main (String[] args)
		{
			Application.Init ();
			Catalog.Init ("Subfinder", LocaleDir);
			var builder = new Builder (null, "Subfinder.subfinder.glade", null);
			var win = new MainWindow (builder, builder.GetObject ("window").Handle);
			win.Show ();

			Application.Run ();
		}
	}
}

