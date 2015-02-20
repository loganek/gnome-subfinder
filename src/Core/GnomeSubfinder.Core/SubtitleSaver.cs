using GnomeSubfinder.Core.DataStructures;
using System.IO;
using System.Diagnostics;

namespace GnomeSubfinder.Core.Core
{
	public class SubtitleSaver
	{
		readonly string tempDir;
		readonly string sZipPath;

		public SubtitleSaver ( string tempDirectory, string sZipPath)
		{
			tempDir = tempDirectory;
			this.sZipPath = sZipPath;
		}

		public void Save(SubtitleFileInfo fileInfo, byte[] zipData)
		{
			if (zipData == null)
				return;

			var tempName = Path.GetFileName (Path.GetTempFileName ()).Replace ('.', '_');
			var tempFullFile = Path.Combine (tempDir, tempName);
			File.WriteAllBytes (tempFullFile, zipData);

			var p = new Process { StartInfo = new ProcessStartInfo (sZipPath, "x -y -o" + tempDir + " " + tempFullFile) {
					UseShellExecute = false,
					RedirectStandardOutput = true
				}
			};

			p.Start ();
			p.WaitForExit ();
			string destination = Path.GetFileNameWithoutExtension (fileInfo.Video.FileName) + ".txt";
			string outDirectory = Path.GetDirectoryName (fileInfo.Video.FileName);
			MakeSpace (destination, outDirectory);
			File.Copy (tempFullFile + "~", Path.Combine(outDirectory, destination));
		}

		void MakeSpace (string filename, string directory)
		{
			string newFilename = filename;
			int index = 1;
			while (File.Exists (Path.Combine (directory, newFilename))) {
				newFilename = GenerateFilenameWithBrackets (filename, index++);
			}

			if (filename != newFilename) {
				File.Move (Path.Combine (directory, filename), Path.Combine (directory, newFilename));
			}
		}

		static string GenerateFilenameWithBrackets (string filename, int index)
		{
			string ext = Path.GetExtension (filename);
			string noext = Path.GetFileNameWithoutExtension (filename);

			return string.Format ("{0}({1}){2}", noext, index, ext);
		}

	}
}

