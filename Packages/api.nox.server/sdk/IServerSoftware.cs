using System;

namespace Nox.Servers {
	public interface IServerSoftware {
		public string Name { get; }

		public Version Version { get; }
	}
}
