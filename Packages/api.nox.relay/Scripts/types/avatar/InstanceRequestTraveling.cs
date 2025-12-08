using System;
using api.nox.relay.types.Instance;
using Nox.Avatars;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Avatar {
	public class InstanceRequestAvatarChanged : RelayInstanceRequest {
		public ushort            PlayerId;
		public IAvatarIdentifier AvatarIdentifier;


		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(PlayerId);
			buffer.Write(AvatarIdentifier.GetId());
			buffer.Write(AvatarIdentifier.GetServer());
			buffer.Write(AvatarIdentifier.GetVersion());
			return buffer;
		}

		public static InstanceRequestAvatarChanged CreateRequest(ushort pid, IAvatarIdentifier avatar)
			=> new() {
				PlayerId         = pid,
				AvatarIdentifier = avatar
			};
	}
}