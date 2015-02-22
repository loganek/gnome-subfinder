using GnomeSubfinder.Core.DataStructures;
using System.IO;
using System.Diagnostics;

namespace GnomeSubfinder.Core.Core
{
	public class SubtitleSaver
	{
		public void Save (SubtitleFileInfo fileInfo, byte[] zipData)
		{
			if (zipData == null)
				return;

			string tempDir = Preferences.Instance.TempDirPath;
			var tempName = Path.GetFileName (Path.GetTempFileName ()).Replace ('.', '_');
			var tempFullFile = Path.Combine (tempDir, tempName);

			File.WriteAllBytes (tempFullFile, zipData);

			RunUnzipProcess (tempDir, tempFullFile);

			string destination = Path.GetFileNameWithoutExtension (fileInfo.Video.FileName) + ".txt";
			string outDirectory = Path.GetDirectoryName (fileInfo.Video.FileName);

			bool overrideSubs = Preferences.Instance.OverrideSubtitles;

			if (!overrideSubs) {
				destination = GenerateNewFilename (destination, outDirectory);
			}
			if (outDirectory == null)
				return;
			string outputFilename = Path.Combine (outDirectory, destination);
			File.Copy (tempFullFile + "~", outputFilename, overrideSubs);
			fileInfo.CurrentPath = outputFilename;
		}

		static void RunUnzipProcess (string tempDir, string tempFullFile)
		{
			var p = new Process { StartInfo = new ProcessStartInfo (Preferences.Instance.SevenZipPath, "x -y -o" + tempDir + " " + tempFullFile) {
					UseShellExecute = false,
					RedirectStandardOutput = true
				}
			};

			p.Start ();
			p.WaitForExit ();
		}

		static string GenerateNewFilename (string filename, string directory)
		{
			string newFilename = filename;
			int index = 1;
			while (File.Exists (Path.Combine (directory, newFilename))) {
				newFilename = GenerateFilenameWithBrackets (filename, index++);
			}

			return newFilename;
		}

		static string GenerateFilenameWithBrackets (string filename, int index)
		{
			string ext = Path.GetExtension (filename);
			string noext = Path.GetFileNameWithoutExtension (filename);

			return string.Format ("{0}({1}){2}", noext, index, ext);
		}

	}
}

