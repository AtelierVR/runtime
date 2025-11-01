using System;
using api.nox.relay.types.Instance;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Nox.Entities;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Transform {
	public class InstanceRequestTransform : RelayInstanceRequest {
		public TransformType           Type;
		public Nox.CCK.Utils.Transform Transform;

		// Type == Player
		public ushort EntityId;
		public ushort PartRig;

		// Type == ByPath
		public string Path;


		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(Type);

			switch (Type) {
				case TransformType.EntityPart:
					buffer.Write(EntityId);
					buffer.Write(PartRig);
					break;
				case TransformType.ByPath:
					buffer.Write(Path);
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

		public static InstanceRequestTransform CreatePart(ushort playerId, IPart part)
			=> new() {
				Type      = TransformType.EntityPart,
				EntityId  = playerId,
				PartRig   = part.GetId(),
				Transform = ToTransform(part)
			};

		private static Nox.CCK.Utils.Transform ToTransform(IPart part) {
			var transform = new Nox.CCK.Utils.Transform();
			if (part.TryGetPosition(out var position))
				transform.SetPosition(position);
			if (part.TryGetRotation(out var rotation))
				transform.SetRotation(rotation);
			if (part.TryGetScale(out var scale))
				transform.SetScale(scale);
			if (part.TryGetVelocity(out var velocity))
				transform.SetVelocity(velocity);
			if (part.TryGetAngularVelocity(out var angularVelocity))
				transform.SetAngularVelocity(angularVelocity);
			return transform;
		}

		public static InstanceRequestTransform CreateByPath(string path, Nox.CCK.Utils.Transform transform)
			=> new() {
				Type      = TransformType.ByPath,
				Path      = path,
				Transform = transform
			};

		public static InstanceRequestTransform CreateEntity(ushort entityId, ushort rig, Nox.CCK.Utils.Transform transform)
			=> new() {
				Type      = TransformType.EntityPart,
				PartRig   = rig,
				EntityId  = entityId,
				Transform = transform
			};

		public static InstanceRequestTransform CreateEntity(ushort entityId, Nox.CCK.Utils.Transform transform)
			=> CreateEntity(entityId, PlayerRig.Base.ToIndex(), transform);
	}
}