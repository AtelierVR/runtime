using System;

namespace Nox.CCK.Mods {
	public class Perfomance {
		public string   Type;
		public string   Entry;
		public string   Key;
		public DateTime Start;
		public DateTime End;

		public TimeSpan Duration
			=> End - Start;
	}
}