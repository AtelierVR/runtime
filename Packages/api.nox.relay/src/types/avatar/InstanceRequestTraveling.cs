using System;
using api.nox.relay.types.Instance;
using Nox.Avatars;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Avatar {
	public class InstanceRequestAvatarChanged : RelayInstanceRequest {
		public AvatarChangedAction Action;
		public string              Reason;

		public ushort            PlayerId;
		public IAvatarIdentifier AvatarIdentifier;


		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(Action);
			if (Action == AvatarChangedAction.Failed && !string.IsNullOrEmpty(Reason))
				buffer.Write(Reason);
			else if (Action == AvatarChangedAction.Request) {
				buffer.Write(PlayerId);
				buffer.Write(AvatarIdentifier.GetId());
				buffer.Write(AvatarIdentifier.GetServerAddress());
				buffer.Write(AvatarIdentifier.GetVersion());
			}

			return buffer;
		}

		public static InstanceRequestAvatarChanged CreateRequest(ushort pid, IAvatarIdentifier avatar)
			=> new() {
				PlayerId         = pid,
				Action           = AvatarChangedAction.Request,
				AvatarIdentifier = avatar
			};

		public static InstanceRequestAvatarChanged CreateFailed(string reason = null)
			=> new() {
				Action = AvatarChangedAction.Failed,
				Reason = reason
			};

		public static InstanceRequestAvatarChanged CreateReady()
			=> new() { Action = AvatarChangedAction.Ready };
	}
}