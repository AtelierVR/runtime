using Nox.Avatars;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.relay {
	public class RelayRemotePlayer : RelayPlayer {
		internal float                     DistanceToLocal = -1f;
		private  RelayPhysicalRemotePlayer _physicalComponent;

		public override bool TryGetPhysical<T>(out T physical) {
			physical = _physicalComponent as T;
			return physical;
		}

		// ReSharper disable Unity.PerformanceAnalysis
		public override bool MakePhysical() {
			if (_physicalComponent)
				return _physicalComponent;
			var parent   = Adapter.EntitiesRoot;
			var prefab   = Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("physical/remote_player.prefab");
			var instance = Object.Instantiate(prefab, GetPosition(), GetRotation(), parent.transform);
			_physicalComponent = instance.GetComponent<RelayPhysicalRemotePlayer>();
			instance.name      = $"[{_physicalComponent.GetType().Name}_{GetId()}]";
			_physicalComponent.SetReference(this);
			Logger.Log($"Created physical component for player {GetDisplay()} ({GetId()}) at {GetPosition()}");
			return _physicalComponent;
		}

		public override void DestroyPhysical() {
			if (!_physicalComponent) return;
			Logger.Log($"Destroying physical component for player {GetDisplay()} ({GetId()}) at {GetPosition()}");
			Object.Destroy(_physicalComponent);
			_physicalComponent = null;
		}

		public override bool HasPhysical()
			=> _physicalComponent;

		public override void SetAvatar(IRuntimeAvatar avatar, IAvatarIdentifier identifier = null) {
			Logger.Log($"Setting avatar for player {GetDisplay()} ({GetId()}) to {(identifier != null ? identifier.ToString() : "null")}");
		}
	}
}