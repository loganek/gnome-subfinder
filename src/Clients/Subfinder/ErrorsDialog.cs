using System;
using System.Collections.Generic;
using Gtk;

using UI = Gtk.Builder.ObjectAttribute;

namespace Subfinder
{
	public class ErrorsDialog : Dialog
	{
		[UI] readonly TreeView errorsView;

		readonly TreeStore errorStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string));

		public ErrorsDialog (Dictionary<string, string> errorMessages, Builder builder, IntPtr handle) : base (handle)
		{
			builder.Autoconnect (this);

			Response += (sender, e) => Destroy ();

			errorsView.Model = errorStore;

			foreach (var m in errorMessages) {
				var iter = errorStore.AppendValues (Gdk.Pixbuf.LoadFromResource ("Subfinder.mov.png"), m.Key);
				errorStore.AppendValues (iter, Gdk.Pixbuf.LoadFromResource ("Subfinder.bad.png"), m.Value);
			}

			errorsView.ExpandAll ();
		}
	}
}

