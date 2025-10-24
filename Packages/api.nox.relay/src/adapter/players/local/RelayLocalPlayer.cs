using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.relay {
	public class RelayLocalPlayer : RelayPlayer {
		private RelayPhysicalLocalPlayer _physical;

		public override bool IsLocal()
			=> true;

		public override bool TryGetPhysical<T>(out T physical) {
			physical = _physical as T;
			return physical;
		}

		// ReSharper disable Unity.PerformanceAnalysis
		public override bool MakePhysical() {
			if (_physical)
				return _physical;
			var parent   = Adapter.EntitiesRoot;
			var prefab   = Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("physical/local_player.prefab");
			var instance = Object.Instantiate(prefab, GetPosition(), GetRotation(), parent.transform);
			_physical     = instance.GetComponent<RelayPhysicalLocalPlayer>();
			instance.name = $"[{_physical.GetType().Name}_{GetId()}]";
			_physical.SetReference(this);
			Logger.Log($"Created physical component for player {GetDisplay()} ({GetId()}) at {GetPosition()}");
			return _physical;
		}

		public override void DestroyPhysical() {
			if (!_physical) return;
			Logger.Log($"Destroying physical component for player {GetDisplay()} ({GetId()}) at {GetPosition()}");
			Object.Destroy(_physical.gameObject);
			_physical = null;
		}

		public override async UniTask<bool> SetAvatar(IAvatarIdentifier identifier) {
			if (identifier == null) {
				Logger.LogWarning("Cannot set avatar: identifier is null");
				return false;
			}

			var packet = types.Avatar.InstanceRequestAvatarChanged.CreateRequest(Reference.Id, identifier);

			if (!RelayExtensions.TryCurrentController(out var controller)) {
				Logger.LogWarning("Cannot set avatar: no controller available");
				return false;
			}

			var response = await Adapter.Instance.RequestAvatarChange(packet);

			if (response.IsSuccess) {
				var avatar = await controller.SetAvatar(identifier);
				if (avatar == null) {
					Logger.LogWarning("Failed to set avatar: loaded avatar is null");
					return false;
				}

				var properties = GetProperties<RelayParameter>();
				var parameterModule = avatar?.GetDescriptor()
					?.GetModules<IParameterModule>()
					.FirstOrDefault();

				if (parameterModule == null)
					return true;
				var parameters = parameterModule.GetParameters();

				foreach (var prop in properties) {
					var linked = parameters.FirstOrDefault(p => p.GetName() == prop.GetKey());
					if (linked == null) {
						RemoveProperty(prop.GetKey());
						continue;
					}

					prop.Attach(linked);
				}

				foreach (var parameter in parameters) {
					var linked = properties.FirstOrDefault(p => p.GetKey() == parameter.GetName());
					if (linked == null) {
						linked = new RelayParameter(parameter);
						AddProperty(linked);
						continue;
					}

					linked.Attach(parameter);
				}

				return true;
			}

			Logger.LogWarning($"Failed to request avatar change: {response}");
			return false;
		}

		public override IAvatarIdentifier GetAvatar()
			=> RelayExtensions.TryCurrentController(out var controller)
				? controller.GetAvatar().GetIdentifier()
				: null;

		public override bool HasPhysical()
			=> _physical;

		private string _lastVoiceId;

		public void SendVoice() {
			return; // Disabled for now
			var voice = Main.MicrophoneAPI.GetCurrent();

			if (voice?.GetName() != _lastVoiceId) {
				var lastMic = Main.MicrophoneAPI.Get(_lastVoiceId);
				lastMic?.Stop("relay");
				_lastVoiceId = voice?.GetName();
				Logger.Log($"Local player voice changed to: {_lastVoiceId ?? "null"}");
			}

			var clip = voice?.Start("relay");
			if (!clip) return;

			var packet = types.Voice.InstanceRequestVoice.CreateRequest(clip);
			if (!packet.IsEmpty())
				Adapter.Instance.SendVoice(packet).Forget();
		}
	}
}