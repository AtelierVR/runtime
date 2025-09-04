using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Logger = Nox.CCK.Utils.Logger;
using NoxTransform = Nox.CCK.Utils.Transform;

namespace api.nox.relay {
	public class RelayPhysicalRemotePlayer : RelayPhysicalPlayer {
		public IRuntimeAvatar Avatar;

		public override IRuntimeAvatar GetAvatar()
			=> Avatar;

		public override void OnMove(ushort part, NoxTransform move) {
			if (part == PlayerRig.Base.ToIndex())
				transform.Move(move);
		}

		public override void OnParameter(int key, byte[] value) {
			if (Avatar == null)
				return;

			var parameterModule = Avatar?.GetDescriptor()
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

			if (identifier.Equals(Avatar?.GetIdentifier()))
				return Avatar;

			_avatarLoadingCts?.Cancel();
			_avatarLoadingCts = new CancellationTokenSource();
			await UniTask.SwitchToMainThread();

			if (Avatar == null) {
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
				download.Start();
				await download.Wait();
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
			if (runtimeAvatar == Avatar)
				return true;

			var old = Avatar;
			Avatar = runtimeAvatar;

			if (Avatar == null) {
				Logger.LogWarning("Setting avatar to null, removing current avatar.");
				Avatar = old;
				return false;
			}

			var root = Avatar.GetDescriptor().GetRoot();
			if (!root) {
				Logger.LogError("Avatar descriptor root is null, cannot set avatar.");
				Avatar = old;
				return false;
			}

			if (old != null)
				await old.Dispose();

			Logger.LogDebug($"Attaching avatar to {runtimeAvatar.GetDescriptor()}", runtimeAvatar.GetDescriptor().GetRoot());
			root.transform.SetParent(transform, false);
			root.transform.position      = Reference.GetPosition();
			root.transform.localRotation = Reference.GetRotation();

			var parameterModule = Avatar?.GetDescriptor()
				?.GetModules<IParameterModule>()
				.FirstOrDefault();

			if (parameterModule == null) {
				Logger.LogWarning("Avatar has no parameter module, cannot configure tracking parameters.");
				return true;
			}

			var parameters = parameterModule.GetParameters();
			foreach (var param in parameters) {
				if (param.IsReadOnly()) continue;
				var n = param.GetName();
				switch (n) {
					case "tracking/head/active":
						param.Set(true);
						break;
					case "tracking/left_hand/active":
					case "tracking/right_hand/active":
					case "tracking/left_foot/active":
					case "tracking/right_foot/active":
						param.Set(false);
						break;
				}
			}

			foreach (var net in Reference.Parameters) {
				var param = parameters.FirstOrDefault(pa => pa.GetHash() == net.Key);
				if (param == null || param.IsReadOnly() || !param.IsSyncable()) continue;
				param.Deserialize(net.Value.Item1);
			}

			root.SetActive(true);
			return true;
		}
	}
}