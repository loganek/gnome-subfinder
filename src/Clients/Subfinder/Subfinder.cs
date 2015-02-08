using System;
using System.IO;
using Gtk;
using Mono.Unix;

namespace Subfinder
{
	public class Subfinder
	{
		public static string LocaleDir
		{
			get 
			{
				string installed_application_prefix = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);
				if (Directory.Exists (Path.Combine (installed_application_prefix, "share", "Subfinder")))
					return installed_application_prefix;
				var entry_directory = new DirectoryInfo (installed_application_prefix);
				if (entry_directory != null && entry_directory.Parent != null && entry_directory.Parent.Parent != null)
					installed_application_prefix = entry_directory.Parent.Parent.FullName;
				return Path.Combine (installed_application_prefix, "share", "locale");
			}
		}
			
		static void Main(String[] args)
		{
			Application.Init ();
			Catalog.Init ("Subfinder", LocaleDir);

			var client = new MainWindow (args);
			client.Run ();

			Application.Run ();
		}

		internal static Builder FromResource (string resourceName)
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly ();
			var builder = new Builder ();

			using (Stream stream = assembly.GetManifestResourceStream (resourceName))
			using (var reader = new StreamReader (stream)) 
			{
				builder.AddFromString (reader.ReadToEnd ());
			}

			return builder;
		}
	}
}

