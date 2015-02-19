using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SubfinderConsole
{
	public class OptionAttribute : Attribute
	{
		public string Help { get; set; }

		public bool HasValue { get; private set; }

		public char Shortcut { get; private set; }

		public string Fullname { get; private set; }

		public OptionAttribute (char shortcut, string fullname = "")
		{
			Shortcut = shortcut;
			Fullname = fullname;
		}

		public OptionAttribute (string fullname, bool hasValue)
		{
			HasValue = hasValue;
			Fullname = fullname;
		}
	}

	struct OptName
	{
		public object Value { get; set; }

		public string Name{ get; set; }

		public bool Noattr { get; set; }
	}

	public class OptionParser<T>
	{
		List<OptionAttribute> attributes;

		readonly T obj;

		readonly string[] arguments;

		List<string> freeArgs = new List<string> ();

		public string[] FreeArguments {
			get { return freeArgs.ToArray (); }
		}

		static OptName GetNameFromArg (string arg)
		{
			var o = new OptName ();
			o.Noattr = false;
			if (arg.StartsWith ("--", StringComparison.Ordinal)) {
				int index = arg.IndexOf ("=", StringComparison.Ordinal);
				if (index >= 0) {
					o.Name = arg.Substring (2, index - 2);
					o.Value = arg.Substring (index + 1);
				}
			} else if (arg.StartsWith ("-", StringComparison.Ordinal)) {
				o.Name = arg.Substring (1);
			} else {
				o.Name = arg;
				o.Noattr = true;
			}
			return o;
		}

		void LoadPropertyAttributes()
		{
			attributes = new List<OptionAttribute> ();
			foreach (var prop in obj.GetType ().GetProperties()) {
				var attribs = prop.GetCustomAttributes (true);
				for (int i = 0; i < attribs.Length; i++) {
					var attr = attribs [i] as OptionAttribute;
					if (attr != null) {
						attributes.Add (attr);
						break; // one attribute per property
					}
				}
			}
		}

		Tuple<OptionAttribute, PropertyInfo> GetPropertyByOptionName (Type t, string name)
		{
			foreach (var prop in t.GetProperties()) {
				foreach (var attribute in attributes) {
					if (attribute.Fullname == name || attribute.Shortcut.ToString () == name) {
						return Tuple.Create (attribute, prop);
					}
				}
			}
			return null;
		}

		public T OptionsObject {
			get { return obj; }
		}

		public OptionParser (T obj, string[] args)
		{
			this.obj = obj;
			arguments = args;

			LoadPropertyAttributes ();
		}

		public void Parse()
		{
			var info = obj.GetType ();
			freeArgs.Clear ();

			foreach (var arg in arguments) {
				var argObj = GetNameFromArg (arg);

				if (argObj.Noattr) {
					freeArgs.Add (argObj.Name);
					continue;
				}

				var opt = GetPropertyByOptionName (info, argObj.Name);
				if (opt == null) {
					throw new ArgumentException ("Unrecognized option " + argObj.Name);
				}

				if (argObj.Value == null && opt.Item2.PropertyType != typeof(bool)) {
					throw new ArgumentException (String.Format ("Option {0} should be boolean type", argObj.Name));
				}

				if (opt.Item1.HasValue != (argObj.Value != null)) {
					throw new ArgumentException (String.Format("Option {0} {1} value, but value has {2} been passed", 
						argObj.Name, opt.Item1.HasValue ? "expects" : "doesn't expect", opt.Item1.HasValue? "not" : ""));
				}

				if (opt.Item1.HasValue) {
					opt.Item2.SetValue (obj, Convert.ChangeType (argObj.Value, opt.Item2.PropertyType), null);
				} else {
					opt.Item2.SetValue (obj, true, null);
				}
			}
		}

		public string GetHelp(string optionsDescription)
		{
			var builder = new StringBuilder ("Usage: " + AppDomain.CurrentDomain.FriendlyName + " " + optionsDescription);
			foreach (var attribute in attributes)
			{
				if (attribute.Fullname != string.Empty) {
					builder.Append ("--" + attribute.Fullname);
					if (attribute.HasValue) {
						builder.Append ("=values");
					}
					if (attribute.Shortcut != 0) {
						builder.Append (',');
					}
				} 
				if (attribute.Shortcut != 0) {
					builder.Append ("-" + attribute.Shortcut);
				}
				builder.Append ("\t - " + attribute.Help);
				builder.AppendLine ();
			}
			return builder.ToString ();
		}
	}
}
