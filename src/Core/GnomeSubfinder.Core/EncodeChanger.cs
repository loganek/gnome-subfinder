using System.Text;
using System.IO;

namespace GnomeSubfinder.Core.Core
{
	public class EncodeChanger
	{
		readonly string filename;

		public EncodeChanger (string filename)
		{
			this.filename = filename;
		}

		public void ChangeEncoding (Encoding encFrom, Encoding encTo)
		{
			if (encFrom == encTo)
				return;

			File.WriteAllText(filename, File.ReadAllText(filename, encFrom), encTo);
		}

		public void AutoChangeEncoding (Encoding encTo)
		{
			Encoding encFrom;
			using (var reader = new StreamReader(filename))
			{
				reader.ReadToEnd();
				encFrom = reader.CurrentEncoding;
			}
			ChangeEncoding (encFrom, encTo);
		}
	}
}

