using System.Linq;
using GnomeSubfinder.Core.DataStructures;
using System;
using System.Reflection;
using System.IO;

namespace GnomeSubfinder.Core.Core
{
	public class BackendManager
	{
		readonly BackendCollection backends = new BackendCollection ();

		public BackendManager ()
		{
			ReloadBackends ();
		}

		public void ReloadBackends ()
		{
			var path = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);
			string[] backendDlls = { Path.Combine(path, "GnomeSubfinder.OpenSubtitles.dll") }; 
			backends.Clear ();

			foreach (var dllFile in backendDlls) {
				if (!File.Exists (dllFile))
					continue;
				var assembly = Assembly.LoadFrom (dllFile);
				if (assembly == null)
					continue;
				foreach (var t in assembly.GetTypes ().Where (t => t.GetInterfaces ().Contains (typeof(IBackend)))) {
					backends.Add ((IBackend)Activator.CreateInstance (t));
				}
			}
		}

		public string[] GetBackendNames ()
		{
			return backends.Select (b => b.GetName ()).ToArray ();
		}

		public SubtitleFileInfo[] SearchSubtitles (VideoFileInfo video, string[] languages)
		{
			return backends.SelectMany (b => b.SearchSubtitles (video, languages)).ToArray ();
		}

		public BackendCollection Backends {
			get { return backends; }
		}
	}
}

