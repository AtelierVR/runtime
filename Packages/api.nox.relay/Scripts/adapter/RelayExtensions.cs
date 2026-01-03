using Nox.Avatars;
using Nox.Avatars.Controllers;
using Nox.Avatars.Rigging;
using Nox.CCK.Network;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Nox.Entities;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.relay {
	public static class RelayExtensions {
		public static void Move(this IMovingEntity entity, TransformObject transform, DirtyBy by) {
			if (!transform.IsSamePosition(entity.GetPosition()))
				entity.SetPosition(transform.GetPosition(), by);
			if (!transform.IsSameRotation(entity.GetRotation()))
				entity.SetRotation(transform.GetRotation(), by);
			if (!transform.IsSameVelocity(entity.GetVelocity()))
				entity.SetVelocity(transform.GetVelocity(), by);
			if (!transform.IsSameAngularVelocity(entity.GetAngularVelocity()))
				entity.SetAngularVelocity(transform.GetAngularVelocity(), by);
		}

		public static void Move(this IMultiPartEntity entity, ushort id, TransformObject transform, DirtyBy by) {
			if (!entity.TryGetPart(id, out var part)) {
				if (id.ToPlayerRig() == PlayerRig.Base && entity is IMovingEntity moving) {
					moving.Move(transform, by);
					return;
				}

				Logger.LogWarning($"Part {id} not found on entity {entity.GetId()}", tag: nameof(IMultiPartEntity));
				return;
			}

			if (!part.TryGetPosition(out var position) || !transform.IsSamePosition(position))
				part.SetPosition(transform.GetPosition(), by);
			if (!part.TryGetRotation(out var rotation) || !transform.IsSameRotation(rotation))
				part.SetRotation(transform.GetRotation(), by);
			if (!part.TryGetVelocity(out var velocity) || !transform.IsSameVelocity(velocity))
				part.SetVelocity(transform.GetVelocity(), by);
			if (!part.TryGetAngularVelocity(out var angularVelocity) || !transform.IsSameAngularVelocity(angularVelocity))
				part.SetAngularVelocity(transform.GetAngularVelocity(), by);
		}

		public static double DistanceWith(this IEntity a, IEntity b) {
			if (a is IMovingEntity ma && b is IMovingEntity mb)
				return Vector3.Distance(ma.GetPosition(), mb.GetPosition());
			return -1d;
		}

		public static IRuntimeAvatar GetRuntimeAvatarController()
			=> TryCurrentController(out var controller)
				? controller.GetAvatar()
				: null;

		public static void TryGetTransform(this IRigPart part, out Transform transform)
			=> part.TryGetTransform(out transform, out _);

		public static void TryGetTransform(this IRigPart part, out Transform transform, out Rigidbody rigid) {
			transform = part.GetTransform();
			rigid     = part.GetRigidbody();
		}

		public static bool TryCurrentController(out IControllerAvatar controller) {
			if (Main.ControllerAPI?.Current() is IControllerAvatar ca) {
				controller = ca;
				return true;
			}

			controller = null;
			return false;
		}
	}
}