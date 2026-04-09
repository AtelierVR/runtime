using System.Collections.Generic;

namespace Nox.Servers {
	public interface IServerVersions : IReadOnlyDictionary<string, string> {
		/// <summary>Node.js runtime version.</summary>
		public string Node { get; }
	}
}
