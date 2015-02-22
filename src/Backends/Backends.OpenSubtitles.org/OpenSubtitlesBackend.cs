using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CookComputing.XmlRpc;
using GnomeSubfinder.Core.DataStructures;
using System.Reflection;
using System.Globalization;

namespace GnomeSubfinder.Backends.OpenSubtitles
{
	public class OpenSubtitlesBackend : IBackend
	{
		readonly IXmlRpcApi osProxy;
		string userToken;

		public OpenSubtitlesBackend ()
		{
			osProxy = XmlRpcProxyGen.Create<IXmlRpcApi> ();
			osProxy.Url = "http://api.opensubtitles.org/xml-rpc";
		}

		public bool LogIn (string username, string password)
		{
			LogInOutInfo info = osProxy.LogIn (username, password, CultureInfo.CurrentCulture.TwoLetterISOLanguageName, "SolEol 0.0.8"); // todo own useragent!

			try {
				CheckStatus (info.status);
			} catch {
				//todo log
				return false;
			}

			userToken = info.token;
			return true;
		}

		public bool LogOut ()
		{
			LogInOutInfo info = osProxy.LogOut (userToken);

			try {
				CheckStatus (info.token);
			} catch {
				//todo log
				return false;
			}

			userToken = string.Empty;
			return true;
		}

		static void CheckStatus (string status)
		{
			int num = Convert.ToInt32 (status.Substring (0, 3));
			if (num >= 200 && num < 300)
				; // todo logger status ok
			else {
				throw new Exception (status);
			}
		}

		#region IBackend implementation

		public int SubtitlesCount (VideoFileInfo file, int language)
		{
			throw new NotImplementedException ();
		}

		public double[] GetSubtitlesRatio (VideoFileInfo file, int language)
		{
			throw new NotImplementedException ();
		}

		public string GetName ()
		{
			return "OpenSubtitles.org";
		}

		public SubtitleFileInfo[] SearchSubtitles (VideoFileInfo video, string[] languages)
		{
			var subs = new List<SubtitleFileInfo> ();
			string hash = ComputeMovieHash (video.FileName);

			var subinfo = new [] { new SubSearchInfo (String.Join (",", languages), hash, video.Size, null) };

			var foundSubs = osProxy.SearchSubtitles (userToken, subinfo);

			try {
				CheckStatus (foundSubs.status);
			} catch {
				//todo log
				return subs.ToArray ();
			}

			var a = foundSubs.data as object[];

			if (a != null) {
				foreach (var subnode in a) {
					var dict = subnode as XmlRpcStruct;
					subs.Add (new SubtitleFileInfo {
						DownloadFile = dict ["SubDownloadLink"].ToString (), 
						Rating = Convert.ToDouble (dict ["SubRating"]),
						Language = dict ["SubLanguageID"].ToString (),
						DownloadsCount = Convert.ToInt32 (dict ["SubDownloadsCnt"]),
						IdMovieImdb = dict ["IDMovieImdb"].ToString (),
						Backend = this,
						Video = video
					});
				}
			}

			return subs.ToArray ();
		}

		public bool Init (params object[] parameters)
		{
			return parameters.Length != 2 ? LogIn ("", "") : LogIn (parameters [0].ToString (), parameters [1].ToString ());

		}

		public Tuple<Assembly, string> GetLogoAssemblyInfo ()
		{
			return Tuple.Create (Assembly.GetExecutingAssembly (), "GnomeSubfinder.Backends.OpenSubtitles.logo.gif");
		}

		#endregion

		static string ComputeMovieHash (string filename)
		{
			byte[] result;
			using (Stream input = File.OpenRead (filename)) {
				long streamsize = input.Length;
				long lhash = streamsize;

				long i = 0;
				var buffer = new byte[sizeof(long)];
				while (i < 65536 / sizeof(long) && (input.Read (buffer, 0, sizeof(long)) > 0)) {
					i++;
					lhash += BitConverter.ToInt64 (buffer, 0);
				}

				input.Position = Math.Max (0, streamsize - 65536);
				i = 0;
				while (i < 65536 / sizeof(long) && (input.Read (buffer, 0, sizeof(long)) > 0)) {
					i++;
					lhash += BitConverter.ToInt64 (buffer, 0);
				}
				input.Close ();
				result = BitConverter.GetBytes (lhash);
				Array.Reverse (result);
			}

			var hexBuilder = new StringBuilder ();
			foreach (byte t in result) {
				hexBuilder.Append (t.ToString ("x2"));
			}
			return hexBuilder.ToString ();
		}
	}
}

