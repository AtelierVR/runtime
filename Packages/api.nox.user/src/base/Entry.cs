using System;
using Nox.Users;

namespace api.nox.user {
	[Serializable]
	public class Entry : IEntry {
		public string label;
		public string value;

		public string GetLabel()
			=> label;

		public string GetValue()
			=> value;
	}
}