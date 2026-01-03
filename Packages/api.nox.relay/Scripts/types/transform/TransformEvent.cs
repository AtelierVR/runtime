using api.nox.relay.types.Instance;
using Nox.CCK.Utils;

namespace api.nox.relay.types.Transform {
	public class TransformEvent : RelayInstanceResponse {
		public TransformType Type;

		public Nox.CCK.Utils.TransformObject Transform;

		// Type == Player
		public ushort EntityId;
		public ushort PartRig;

		// Type == ByPath
		public string Path;

		public ushort ByEntityId;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			InternalId = buffer.ReadByte();
			Type       = buffer.ReadEnum<TransformType>();

			switch (Type) {
				case TransformType.EntityPart:
					EntityId = buffer.ReadUShort();
					PartRig  = buffer.ReadUShort();
					break;
				case TransformType.ByPath:
					Path = buffer.ReadString();
					break;
				default:
					return false;
			}

			Transform = new Nox.CCK.Utils.TransformObject();
			var flags = buffer.ReadEnum<TransformFlags>();
			if (flags.HasFlag(TransformFlags.Position))
				Transform.SetPosition(buffer.ReadVector3());
			if (flags.HasFlag(TransformFlags.Rotation))
				Transform.SetRotation(buffer.ReadQuaternion());
			if (flags.HasFlag(TransformFlags.Scale))
				Transform.SetScale(buffer.ReadVector3());
			if (flags.HasFlag(TransformFlags.Velocity))
				Transform.SetVelocity(buffer.ReadVector3());
			if (flags.HasFlag(TransformFlags.AngularVelocity))
				Transform.SetAngularVelocity(buffer.ReadVector3());
			
			ByEntityId = buffer.ReadUShort();
			return true;
		}

		public override string ToString()
			=> $"{GetType().Name}[ConnectionId={ConnectionId}, InternalId={InternalId}, Type={Type}"
				+ $"{(Type == TransformType.EntityPart ? $", EntityId={EntityId}, PartRig={PartRig}" : "")}"
				+ $"{(Type == TransformType.ByPath ? $", Path={Path}" : "")}"
				+ $", Transform={Transform}, ByEntityId={ByEntityId}]";
	}
}