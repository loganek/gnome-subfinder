using CookComputing.XmlRpc;

namespace GnomeSubfinder.Backends.OpenSubtitles
{
	public struct FoundSubtitleInfo
	{
		public string MatchedBy;
		public string SubFileName;
		public int SubActualCD;
		public int SubSumCD;
		public int SubSize;
		public string SubFormat;
		public string SubAuthorComment;
		public string SubAddDate;
		public double SubRating;
		public int SubDownloadsCnt;
		public string MovieReleaseName;
		public string MovieName;
		public int MovieYear;
		public double MovieImdbRating;
		public string LanguageName;
		public string SubDownloadLink;
		public string ZipDownloadLink;
	}

	public struct FoundSubtitleResponse
	{
		public string status;
		public object data;
	}

	public struct LogInOutInfo
	{
		public string token;
		public string status;
	}

	public struct NoOperationInfo
	{
		public string status;
		public double seconds;
	}

	[XmlRpcMissingMapping (MappingAction.Ignore)]
	public struct SubSearchInfo
	{
		public string sublanguageid;
		public string moviehash;
		public double? moviebytesize;
		public string imdbid;

		public SubSearchInfo (string sublanguageid, string moviehash, double? moviebytesize, string imdbid)
		{
			this.sublanguageid = sublanguageid;
			this.moviehash = moviehash;
			this.moviebytesize = moviebytesize;
			this.imdbid = imdbid;
		}
	}


	public interface IXmlRpcApi : IXmlRpcProxy
	{
		[XmlRpcMethod]
		LogInOutInfo LogIn (string username, string password, string language, string useragent);

		[XmlRpcMethod]
		LogInOutInfo LogOut (string token);

		[XmlRpcMethod] 
		NoOperationInfo NoOperation (string token);

		[XmlRpcMethod]
		FoundSubtitleResponse SearchSubtitles (string token, SubSearchInfo[] searchInfo);

	}
}

