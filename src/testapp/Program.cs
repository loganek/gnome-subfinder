using System;
using CookComputing.XmlRpc;
using GnomeSubfinder.Core.Core;
using GnomeSubfinder.Core.Interfaces;

namespace Testapp
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var f = new GnomeSubfinder.Core.Interfaces.VideoFileInfo ();
			f.FileName = "/home/loganek/Downloads/Robots (2005) [1080p]/Robots.2005.1080p.BrRip.x264.YIFY.mp4";
			var searcher = new BackendManager ();

			var x = searcher.SearchSubtitles (f, "eng");
			Console.WriteLine ("Hello World!");
		}
	}
}
