using System;

namespace Nox.Servers {
	public interface IServer {
		/// <summary>Unique node identifier within a cluster.</summary>
		public string Id { get; }

		/// <summary>Public domain address of the server.</summary>
		public string Address { get; }

		/// <summary>Gateway URLs keyed by name (web, ws, api).</summary>
		public IServerGateway Gateway { get; }

		/// <summary>Instance metadata (title, description, icon, contact).</summary>
		public IServerMetadata Metadata { get; }

		/// <summary>Runtime versions keyed by name (e.g. "node").</summary>
		public IServerVersions Versions { get; }

		/// <summary>Protocol endpoint URLs (well-known, webfinger, nodeinfo).</summary>
		public IServerEndpoints Endpoints { get; }

		/// <summary>Supported feature flags.</summary>
		public string[] Features { get; }

		/// <summary>Supported capability flags.</summary>
		public string[] Capabilities { get; }

		/// <summary>Software identifier.</summary>
		public IServerSoftware Software { get; }

		/// <summary>When this node started.</summary>
		public DateTime ReadyAt { get; }

		/// <summary>Ed25519 public key (base64 SPKI DER).</summary>
		public string PublicKey { get; }

		/// <summary>Listening port.</summary>
		public int Port { get; }

		/// <summary>Operational status: "online", "maintenance" or "degraded".</summary>
		public string Status { get; }

		/// <summary>Maintenance message displayed to users, or null.</summary>
		public string Maintenance { get; }
	}
}