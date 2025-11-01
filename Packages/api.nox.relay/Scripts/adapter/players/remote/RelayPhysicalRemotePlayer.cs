using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.Avatars.Voice;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using NoxTransform = Nox.CCK.Utils.Transform;

namespace api.nox.relay {
	public class RelayPhysicalRemotePlayer : RelayPhysicalPlayer {
		private IRuntimeAvatar _avatar;

		public override IRuntimeAvatar GetAvatar()
			=> _avatar;

		private void Update() {
			var tps   = Reference.Adapter.Tps;
			var lerpT = 1f - Mathf.Exp(-tps * Time.deltaTime);
			foreach (var part in Reference.GetParts())
				part.Update(lerpT);
		}

		private CancellationTokenSource _avatarLoadingCts;

		public async UniTask<IRuntimeAvatar> SetAvatar(IAvatarIdentifier identifier) {
			Logger.LogDebug($"Loading avatar for identifier {identifier?.ToString() ?? "null"}");

			if (identifier == null || !identifier.IsValid())
				return null;

			if (identifier.Equals(_avatar?.GetIdentifier()))
				return _avatar;

			_avatarLoadingCts?.Cancel();
			_avatarLoadingCts = new CancellationTokenSource();
			await UniTask.SwitchToMainThread();

			if (_avatar == null) {
				var loading = await Main.AvatarAPI.LoadLoading(token: _avatarLoadingCts.Token);
				if (loading == null) {
					Logger.LogWarning("Failed to create loading avatar for PhysicalRemotePlayer", this);
				} else await SetAvatar(loading);
			}

			var asset = (await Main.AvatarAPI.SearchAssets(
						identifier.ToString(),
						Main.AvatarAPI.MakeAssetSearchRequest()
							.SetEngines(new[] { EngineExtensions.CurrentEngine.GetEngineName() })
							.SetPlatforms(new[] { PlatformExtensions.CurrentPlatform.GetPlatformName() })
							.SetLimit(1)
							.SetVersions(new[] { identifier.GetVersion() })
					)
					.AttachExternalCancellation(_avatarLoadingCts.Token)).GetAssets()
				.FirstOrDefault();
			if (_avatarLoadingCts.IsCancellationRequested)
				return null;

			if (asset == null) {
				Logger.LogWarning($"Avatar asset not found for identifier {identifier.ToString()}");
				var err = await Main.AvatarAPI.LoadError();
				err.SetIdentifier(identifier);
				await SetAvatar(err);
				_avatarLoadingCts = null;
				return null;
			}

			if (!Main.AvatarAPI.HasInCache(asset.GetHash())) {
				var download = Main.AvatarAPI.DownloadToCache(
					asset.GetUrl(),
					hash: asset.GetHash(),
					progress: p => Logger.LogDebug($"Downloading avatar {identifier.ToString()}: {p:P1}"),
					token: _avatarLoadingCts.Token
				);
				await download.Start();
				if (_avatarLoadingCts.IsCancellationRequested)
					return null;
			}

			var avatar = await Main.AvatarAPI.LoadFromCache(
				asset.GetHash(),
				progress: p => Logger.LogDebug($"Loading avatar {identifier.ToString()}: {p:P1}"),
				token: _avatarLoadingCts.Token
			);
			if (_avatarLoadingCts.IsCancellationRequested)
				return null;

			if (avatar == null) {
				Logger.LogError($"Failed to load avatar from cache for identifier {identifier.ToString()}");
				var err = await Main.AvatarAPI.LoadError();
				err.SetIdentifier(identifier);
				await SetAvatar(err);
				_avatarLoadingCts = null;
				return null;
			}

			Logger.LogDebug($"Avatar loaded: {identifier.ToString()}");
			avatar.SetIdentifier(identifier);
			await SetAvatar(avatar);
			_avatarLoadingCts = null;

			return avatar;
		}

		private async UniTask<bool> SetAvatar(IRuntimeAvatar runtimeAvatar) {
			Logger.LogDebug("Setting avatar for XRController");
			if (runtimeAvatar == _avatar)
				return true;

			var old = _avatar;
			_avatar = runtimeAvatar;

			if (_avatar == null) {
				Logger.LogWarning("Setting avatar to null, removing current avatar.");
				_avatar = old;
				return false;
			}

			var root = _avatar.GetDescriptor().GetAnchor();
			if (!root) {
				Logger.LogError("Avatar descriptor root is null, cannot set avatar.");
				_avatar = old;
				return false;
			}

			// Remove old properties and parts BEFORE disposing the old avatar
			var properties = Reference.GetProperties<RelayParameter>();
			foreach (var prop in properties)
				Reference.RemoveProperty(prop.GetKey());

			foreach (var p in Reference.GetParts())
				Reference.RemovePart(p.GetId());

			if (old != null)
				await old.Dispose();

			Logger.LogDebug($"Attaching avatar to {runtimeAvatar.GetDescriptor()}", runtimeAvatar.GetDescriptor().GetAnchor());
			root.transform.SetParent(transform, false);
			root.transform.localPosition = Vector3.zero;
			root.transform.localRotation = Quaternion.identity;

			// Activate the avatar BEFORE getting parameters so the Animator can initialize
			root.SetActive(true);

			// Wait one frame for the Animator to initialize
			await UniTask.Yield();

			var parameterModule = _avatar.GetDescriptor()
				?.GetModules<IParameterModule>()
				.FirstOrDefault();

			if (parameterModule == null) {
				Logger.LogError("Avatar does not have a ParameterModule, cannot set avatar.");
				return false;
			}

			var parameters = parameterModule.GetParameters();

			foreach (var param in parameters) {
				if (param.IsReadOnly()) continue;
				var n = param.GetName();
				switch (n) {
					case "tracking/head/active":
					case "tracking/left_hand/active":
					case "tracking/right_hand/active":
					case "tracking/left_foot/active":
					case "tracking/right_foot/active":
					case "tracking/left_toes/active":
					case "tracking/right_toes/active":
						param.Set(false);
						break;
				}
			}


			foreach (var parameter in parameters) {
				var relayParam = new RelayParameter(Reference, parameter);
				Reference.AddProperty(relayParam);
			}


			var rigModule = _avatar.GetDescriptor()
				?.GetModules<IRiggingModule>()
				.FirstOrDefault();

			foreach (var part in rigModule?.GetParts() ?? Array.Empty<IRigPart>()) {
				var relayPart = new RelayRigPart(Reference, part);
				Reference.AddPart(relayPart);
			}

			return true;
		}

		private void OnDestroy() {
			var properties = Reference.GetProperties<RelayParameter>();
			foreach (var prop in properties)
				Reference.RemoveProperty(prop.GetKey());
		}
	}
}