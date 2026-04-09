using System;
using Nox.CCK.Network;
using Nox.Servers;

namespace api.nox.server
{
	// ReSharper disable InconsistentNaming
	[Serializable]
	public class Server : IServer
	{
		public static Server From(NoxWellKnown wk)
			=> wk == null ? null : new Server { reference = wk };

		public NoxWellKnown reference;

		public string Id
			=> reference.id;

		public string Address
			=> reference.address;

		public string Status
			=> reference.status;

		public int Port
			=> reference.port;

		public IServerSoftware Software
			=> ServerSoftware.From(reference.software);

		public string PublicKey
			=> reference.publicKey;

		public string Maintenance
			=> reference.maintenance;

		public string[] Features
			=> reference.features;

		public string[] Capabilities
			=> reference.capabilities;

		public IServerVersions Versions
			=> ServerVersions.From(reference.versions);

		public IServerGateway Gateway
			=> ServerGateway.From(reference.gateway);

		public IServerMetadata Metadata
			=> ServerMetadata.From(reference.metadata);

		public IServerEndpoints Endpoints
			=> ServerEndpoints.From(reference.endpoints);

		public DateTime ReadyAt
			=> reference.started > 0 
				? DateTimeOffset.FromUnixTimeMilliseconds((long)reference.started).UtcDateTime 
				: default;

		public override string ToString()
			=> $"{GetType().Name}[id={Id}, address={Address}, title={Metadata?.Title}]";
	}
}