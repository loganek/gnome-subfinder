using System.Collections.Generic;
using GnomeSubfinder.Core.DataStructures;
using System.Linq;

namespace GnomeSubfinder.Core.Core
{
	public class BackendCollection : IEnumerator<IBackend>, IEnumerable<IBackend>
	{
		readonly List<IBackend> backends = new List<IBackend> ();
		int position = -1;

		public void Clear ()
		{
			backends.Clear ();
			Reset ();
		}

		public void Add (IBackend backend, params object[] parameters)
		{
			backend.Init (parameters);
			backends.Add (backend);
		}

		public T GetByType<T> () where T : class, IBackend
		{
			var c = (from backend in backends
			         where backend is T
			         select backend);

			var enumerable = c as IBackend[] ?? c.ToArray ();
			return enumerable.Any () ? enumerable [0] as T : null;
		}

		public IBackend GetByName (string dbName)
		{
			var c = (from backend in backends
			         where backend.GetName () == dbName
			         select backend);

			return c.FirstOrDefault ();
		}

		public string[] GetDatabaseBackends ()
		{
			return (from backend in backends
			        select backend.GetName ()).Distinct ().ToArray ();
		}

		#region IEnumerator implementation

		public bool MoveNext ()
		{
			position++;
			return (position < backends.Count);
		}

		public void Reset ()
		{
			position = -1;
		}

		object System.Collections.IEnumerator.Current {
			get {
				return backends [position];
			}
		}

		public IBackend Current {
			get {
				return backends [position];
			}
		}

		#endregion

		#region IDisposable implementation

		public void Dispose ()
		{
		}

		#endregion

		#region IEnumerable implementation

		public IEnumerator<IBackend> GetEnumerator ()
		{
			Reset ();
			return this;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		#endregion
	}
}

