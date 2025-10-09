using System;
using System.Linq;

namespace Nox.CCK.Mods {
	public class Profile {
		public string   Type;
		public string   Entry;
		public string   Key;
		public DateTime Start;
		public DateTime End;

		public TimeSpan Duration
			=> End - Start;

		public string GetName()
			=> string.Join(".", new[] { Type, Entry, Key }.Where(x => !string.IsNullOrEmpty(x)));
	}
}