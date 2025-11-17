using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Players;
using Nox.Avatars.Rigging;
using Nox.CCK.Network;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.relay {
	public class RelayLocalPlayer : RelayPlayer, ILocalPlayerAvatar {
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

				return true;
			}

			Logger.LogWarning($"Failed to request avatar change: {response}");
			return false;
		}

		public override IAvatarIdentifier GetAvatar()
			=> RelayExtensions.GetRuntimeAvatarController()?.GetIdentifier();

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

		public async UniTask<bool> OnAvatarReady() {
			Logger.LogDebug("Local player avatar is ready");
			SetupParameters();
			SetupTransforms();
			await UniTask.Yield();
			return true;
		}

		private void SetupTransforms() {
			// Sauvegarder les valeurs des anciennes parts
			var oldParts = GetParts().ToDictionary(p => p.GetId(), p => p);

			foreach (var part in GetParts())
				RemovePart(part.GetId());
			var rigModule = RelayExtensions
				.GetRuntimeAvatarController()
				?.GetDescriptor()
				?.GetModules<IRiggingModule>()
				.FirstOrDefault();
			if (rigModule == null) return;
			var parts = rigModule.GetParts();
			foreach (var part in parts) {
				var relayPart = new RelayRigPart(this, part);
				AddPart(relayPart);

				// Restaurer les valeurs de l'ancienne part si elle existe
				if (oldParts.TryGetValue(part.GetId(), out var oldPart)) {
					if (oldPart.TryGetPosition(out var position))
						relayPart.SetPosition(position);
					if (oldPart.TryGetRotation(out var rotation))
						relayPart.SetRotation(rotation);
					if (oldPart.TryGetScale(out var scale))
						relayPart.SetScale(scale);
					if (oldPart.TryGetVelocity(out var velocity))
						relayPart.SetVelocity(velocity);
					if (oldPart.TryGetAngularVelocity(out var angularVelocity))
						relayPart.SetAngularVelocity(angularVelocity);
				}

				relayPart.SetDirty(DirtyBy.Local);
			}
		}

		private void SetupParameters() {
			// Sauvegarder les valeurs des anciens paramètres
			var oldProperties = GetProperties<RelayParameter>().ToDictionary(p => p.GetKey(), p => p.GetValue());

			var properties = GetProperties<RelayParameter>();
			foreach (var p in properties)
				RemoveProperty(p.GetKey());

			var parameterModule = RelayExtensions
				.GetRuntimeAvatarController()
				?.GetDescriptor()
				?.GetModules<IParameterModule>()
				.FirstOrDefault();

			if (parameterModule == null)
				return;

			var parameters = parameterModule.GetParameters();
			foreach (var p in parameters) {
				var pa = new ReferencedRelayParameter(this, p);
				AddProperty(pa);

				// Restaurer la valeur de l'ancien paramètre si il existe
				if (oldProperties.TryGetValue(p.GetKey(), out var oldValue))
					pa.SetValue(oldValue, DirtyBy.Local);

				pa.SetDirty(DirtyBy.Local);
			}
		}

		public UniTask<bool> OnAvatarFailed(string reason)
			=> UniTask.FromResult(true);
	}
}