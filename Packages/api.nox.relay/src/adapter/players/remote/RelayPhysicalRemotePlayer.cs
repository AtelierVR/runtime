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

		public override void OnMove(ushort part, NoxTransform move) { }

		private void Update() {
			var tps   = Reference.Adapter.Tps;
			var lerpT = 1f - Mathf.Exp(-tps * Time.deltaTime);
			foreach (var part in Reference.GetParts())
				part.LerpTarget(lerpT);
		}

		public override void OnParameter(int key, byte[] value) {
			if (_avatar == null)
				return;

			var parameterModule = _avatar?.GetDescriptor()
				?.GetModules<IParameterModule>()
				.FirstOrDefault();

			if (parameterModule == null) return;
			var parameter = parameterModule.GetParameter(key);
			if (parameter == null || parameter.IsReadOnly() || !parameter.IsSyncable()) return;

			parameter.Deserialize(value);
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

			if (old != null)
				await old.Dispose();

			var properties = Reference.GetProperties<RelayParameter>();
			foreach (var prop in properties)
				Reference.RemoveProperty(prop.GetKey());

			Logger.LogDebug($"Attaching avatar to {runtimeAvatar.GetDescriptor()}", runtimeAvatar.GetDescriptor().GetAnchor());
			root.transform.SetParent(transform, false);
			root.transform.localPosition = Vector3.zero;
			root.transform.localRotation = Quaternion.identity;

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
					case "tracking/left_toe/active":
					case "tracking/right_toe/active":
						param.Set(false);
						break;
				}
			}

			properties = Reference.GetProperties<RelayParameter>();

			foreach (var prop in properties) {
				var linked = parameters.FirstOrDefault(p => p.GetName() == prop.GetKey());
				if (linked == null) {
					Logger.LogDebug($"Removing parameter {prop.GetKey()} for player {Reference.GetId()}");
					Reference.RemoveProperty(prop.GetKey());
					continue;
				}

				Logger.LogDebug($"Linking parameter {prop.GetKey()} for player {Reference.GetId()}");
				prop.Attach(linked);
			}

			foreach (var parameter in parameters) {
				var linked = properties.FirstOrDefault(p => p.GetKey() == parameter.GetName());
				if (linked == null) {
					Logger.LogDebug($"Adding parameter {parameter.GetName()} for player {Reference.GetId()}");
					linked = new RelayParameter(Reference, parameter);
					Reference.AddProperty(linked);
					continue;
				}

				Logger.LogDebug($"Re-linking parameter {linked.GetKey()} for player {Reference.GetId()}");
				linked.Attach(parameter);
			}

			foreach (var p in Reference.GetParts())
				Reference.RemovePart(p.GetId());

			var rigModule = _avatar.GetDescriptor()
				?.GetModules<IRiggingModule>()
				.FirstOrDefault();

			foreach (var part in rigModule?.GetParts() ?? Array.Empty<IRigPart>()) {
				var relayPart = new RelayPart(Reference, part);
				Reference.AddPart(relayPart);
				Logger.LogDebug($"Linking transform '{part.GetId()}' to local player '{Reference.GetDisplay()}'");
			}

			SetVoice(Reference.GetAudio());

			root.SetActive(true);
			return true;
		}

		private void OnDestroy() {
			var properties = Reference.GetProperties<RelayParameter>();
			foreach (var prop in properties)
				prop.Detach();
		}

		public override void SetVoice(AudioClip clip) {
			var voiceModule = _avatar.GetDescriptor()
				?.GetModules<IVoiceModule>()
				.FirstOrDefault();

			if (voiceModule == null) {
				Logger.LogWarning("Avatar does not have a VoiceModule, cannot set audio clip.");
				return;
			}

			var source = voiceModule.GetSource();

			source.clip   = clip;
			source.loop   = true;
			source.mute   = false;
			source.volume = 1.0f;

			source.Play();
		}
	}
}