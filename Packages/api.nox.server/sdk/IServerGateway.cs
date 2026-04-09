using System.Collections.Generic;

namespace Nox.Servers {
	public interface IServerGateway : IReadOnlyDictionary<string, string> {
		/// <summary>Public REST API base URL (e.g. https://example.com/api/).</summary>
		public string Api { get; }

		/// <summary>HTTP/HTTPS base URL of the web frontend.</summary>
		public string Web { get; }

		/// <summary>WebSocket URL.</summary>
		public string Ws { get; }

		/// <summary>URL of the /.well-known/nox document.</summary>
		public string Wellknown { get; }
	}
}
