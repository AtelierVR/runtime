using api.nox.relay.types.Instance;
using Nox.CCK.Players;
using Nox.CCK.Utils;

namespace api.nox.relay.types.Transform {
	public class TransformEvent : RelayInstanceResponse {
		public TransformType Type;

		public Nox.CCK.Utils.Transform Transform;

		// Type == Player
		public ushort    PlayerId;
		public PlayerRig PlayerRig;

		// Type == ByPath
		public string Path;

		// Type == Entity
		public ushort EntityId;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			InternalId = buffer.ReadByte();
			Type       = buffer.ReadEnum<TransformType>();

			switch (Type) {
				case TransformType.Player:
					PlayerId  = buffer.ReadUShort();
					PlayerRig = buffer.ReadEnum<PlayerRig>();
					break;
				case TransformType.ByPath:
					Path = buffer.ReadString();
					break;
				case TransformType.Entity:
					EntityId = buffer.ReadUShort();
					break;
				default:
					return false;
			}

			Transform = new Nox.CCK.Utils.Transform();

			if (Type.HasFlag(TransformFlags.Position))
				Transform.SetPosition(buffer.ReadVector3());
			if (Type.HasFlag(TransformFlags.Rotation))
				Transform.SetRotation(buffer.ReadQuaternion());
			if (Type.HasFlag(TransformFlags.Scale))
				Transform.SetScale(buffer.ReadVector3());
			if (Type.HasFlag(TransformFlags.Velocity))
				Transform.SetVelocity(buffer.ReadVector3());
			if (Type.HasFlag(TransformFlags.AngularVelocity))
				Transform.SetAngularVelocity(buffer.ReadVector3());

			return true;
		}

		public override string ToString()
			=> $"{GetType().Name}[ConnectionId={ConnectionId}, InternalId={InternalId}, Type={Type}"
				+ $"{(Type == TransformType.Player ? $", PlayerId={PlayerId}, PlayerRig={PlayerRig}" : "")}"
				+ $"{(Type == TransformType.ByPath ? $", Path={Path}" : "")}"
				+ $"{(Type == TransformType.Entity ? $", EntityId={EntityId}" : "")}"
				+ $", Transform={Transform}]";
	}
}