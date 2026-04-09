namespace Nox.Servers {
	public interface IServerEndpoints {
		/// <summary>URL of the /.well-known/nox document.</summary>
		public string WellKnown { get; }

		/// <summary>WebFinger template URL with {uri} placeholder.</summary>
		public string Webfinger { get; }

		/// <summary>NodeInfo links document URL.</summary>
		public string Nodeinfo { get; }
	}
}
