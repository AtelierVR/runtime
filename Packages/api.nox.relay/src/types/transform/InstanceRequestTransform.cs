using System;
using api.nox.relay.types.Instance;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Transform {
	public class InstanceRequestTransform : RelayInstanceRequest {
		public TransformType           Type;
		public Nox.CCK.Utils.Transform Transform;

		// Type == Player
		public ushort PlayerId;
		public ushort PlayerRig;

		// Type == ByPath
		public string Path;

		// Type == Entity
		public ushort EntityId;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(Type);

			switch (Type) {
				case TransformType.Player:
					buffer.Write(PlayerId);
					buffer.Write(PlayerRig);
					break;
				case TransformType.ByPath:
					buffer.Write(Path);
					break;
				case TransformType.Entity:
					buffer.Write(EntityId);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			buffer.Write(Transform.Flags);
			if (Transform.Flags.HasFlag(TransformFlags.Position))
				buffer.Write(Transform.GetPosition());
			if (Transform.Flags.HasFlag(TransformFlags.Rotation)) 
				buffer.Write(Transform.GetRotation());
			if (Transform.Flags.HasFlag(TransformFlags.Scale))
				buffer.Write(Transform.GetScale());
			if (Transform.Flags.HasFlag(TransformFlags.Velocity))
				buffer.Write(Transform.GetVelocity());
			if (Transform.Flags.HasFlag(TransformFlags.AngularVelocity))
				buffer.Write(Transform.GetAngularVelocity());

			return buffer;
		}

		public static InstanceRequestTransform CreatePlayer(ushort playerId, ushort rig, Nox.CCK.Utils.Transform transform)
			=> new() {
				Type      = TransformType.Player,
				PlayerId  = playerId,
				PlayerRig = rig,
				Transform = transform
			};

		public static InstanceRequestTransform CreateByPath(string path, Nox.CCK.Utils.Transform transform)
			=> new() {
				Type      = TransformType.ByPath,
				Path      = path,
				Transform = transform
			};

		public static InstanceRequestTransform CreateEntity(ushort entityId, Nox.CCK.Utils.Transform transform)
			=> new() {
				Type      = TransformType.Entity,
				EntityId  = entityId,
				Transform = transform
			};
	}
}