using api.nox.relay.connection;
using api.nox.relay.types;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.relay.Instances {
	public class RelayInstance : INoxObject {
		public uint          Id;
		public byte          InternalId;
		public ushort        PlayerCount;
		public ushort        MaxPlayerCount;
		public InstanceFlags Flags;

		public Connection Connection;

		public async UniTask<types.Enter.InstanceResponseEnter> RequestEnter(string display = null, string password = null, types.Enter.EnterFlags flags = types.Enter.EnterFlags.None)
			=> await Connection.Request<types.Enter.InstanceResponseEnter>(
					new types.Enter.InstanceRequestEnter {
						ConnectionId = Connection.Id,
						InternalId   = InternalId,
						Display      = display,
						Password     = password,
						Flags        = flags
					},
					RequestType.Enter,
					ResponseType.Enter,
					Connection.NextState()
				)
				?? types.Enter.InstanceResponseEnter.CreateUnknown(Connection.Id, InternalId, "Unknown enter request");

		public override string ToString()
			=> $"{GetType().Name}[Id={Id}, InternalId={InternalId}, ConnectionId={Connection.Id}, PlayerCount={PlayerCount}, MaxPlayerCount={MaxPlayerCount}, Flags={Flags}]";
	}
}