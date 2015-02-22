using Gtk;

namespace Subfinder
{
	static class Utils
	{
		internal static TreeIter GetSelectedIter (TreeView tree)
		{
			TreeIter iter;
			tree.Selection.GetSelected (out iter);
			return iter;
		}

		internal static void ShowMessageDialog (string text, MessageType type)
		{
			var md = new MessageDialog (null, DialogFlags.Modal, type, ButtonsType.Ok, text);
			md.Run ();
			md.Destroy ();
		}
	}
}

