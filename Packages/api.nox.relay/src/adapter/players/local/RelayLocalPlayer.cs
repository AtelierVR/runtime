using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Controllers;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.relay {
	public class RelayLocalPlayer : RelayPlayer {
		public override bool IsLocal()
			=> true;

		private RelayPhysicalLocalPlayer _physicalComponent;

		public void SendTransform() {
			var trs = Transforms
				.Where(e => e.Value.DeliveryType == DeliveryType.LocalModified)
				.ToArray();
			if (trs.Length == 0) return;
			foreach (var tr in trs) {
				var packet = types.Transform.InstanceRequestTransform.CreatePlayer(Reference.Id, tr.Key, tr.Value);
				Adapter.Instance.SendTransform(packet).Forget();
				tr.Value.DeliveryType = DeliveryType.None;
				Transforms[tr.Key]    = tr.Value;
			}
		}

		public void SendParameters() {
			var paramsToSend = Parameters
				.Where(p => p.Value.Item2 == DeliveryType.LocalModified)
				.ToArray();
			if (paramsToSend.Length == 0) return;
			var dictionary = paramsToSend.ToDictionary(p => p.Key, p => p.Value.Item1);
			var packet     = types.Avatar.InstanceRequestAvatarParams.CreateRequest(Reference.Id, dictionary);
			Adapter.Instance.SendAvatarParams(packet).Forget();
			foreach (var p in paramsToSend)
				Parameters[p.Key] = (p.Value.Item1, DeliveryType.None);
		}

		public override bool TryGetPhysical<T>(out T physical) {
			physical = _physicalComponent as T;
			return physical;
		}

		// ReSharper disable Unity.PerformanceAnalysis
		public override bool MakePhysical() {
			if (_physicalComponent)
				return _physicalComponent;
			var parent   = Adapter.EntitiesRoot;
			var prefab   = Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("physical/local_player.prefab");
			var instance = Object.Instantiate(prefab, GetPosition(), GetRotation(), parent.transform);
			_physicalComponent = instance.GetComponent<RelayPhysicalLocalPlayer>();
			instance.name      = $"[{_physicalComponent.GetType().Name}_{GetId()}]";
			_physicalComponent.SetReference(this);
			Logger.Log($"Created physical component for player {GetDisplay()} ({GetId()}) at {GetPosition()}");
			return _physicalComponent;
		}

		public override void DestroyPhysical() {
			if (!_physicalComponent) return;
			Logger.Log($"Destroying physical component for player {GetDisplay()} ({GetId()}) at {GetPosition()}");
			Object.Destroy(_physicalComponent.gameObject);
			_physicalComponent = null;
		}

		internal static bool TryCurrentController(out IControllerAvatar controller) {
			if (Main.ControllerAPI.GetCurrent() is IControllerAvatar ca) {
				controller = ca;
				return true;
			}

			controller = null;
			return false;
		}

		public override async UniTask<bool> SetAvatar(IAvatarIdentifier identifier) {
			if (identifier == null) {
				Logger.LogWarning("Cannot set avatar: identifier is null");
				return false;
			}

			var packet = types.Avatar.InstanceRequestAvatarChanged.CreateRequest(Reference.Id, identifier);

			if (!TryCurrentController(out var controller)) {
				Logger.LogWarning("Cannot set avatar: no controller available");
				return false;
			}

			var response = await Adapter.Instance.RequestAvatarChange(packet);

			if (response.IsSuccess)
				return await controller.SetAvatar(identifier) != null;

			Logger.LogWarning($"Failed to request avatar change: {response}");
			return false;
		}

		public override IAvatarIdentifier GetAvatar()
			=> TryCurrentController(out var controller)
				? controller.GetAvatar().GetIdentifier()
				: null;

		public override bool HasPhysical()
			=> _physicalComponent;
	}
}