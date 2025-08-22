using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;

namespace api.nox.avatar {
	public class AssetBundleRuntimeRuntimeAvatar : BaseRuntimeRuntimeAvatar {
		public AssetBundle Bundle;
		public string      Path;

		public bool CanUnloadAssetBundle() {
			foreach (var a0 in AvatarLoader.Avatar)
				if (a0 is AssetBundleRuntimeRuntimeAvatar a1 && a1.Path == Path && a1.GetId() != GetId())
					return false;
			return true;
		}

		private static AssetBundle GetAssetBundle(string path) {
			foreach (var a0 in AvatarLoader.Avatar)
				if (a0 is AssetBundleRuntimeRuntimeAvatar a1 && a1.Path == path)
					return a1.Bundle;
			return null;
		}


		public static async UniTask<AssetBundleRuntimeRuntimeAvatar> Load(string path, Action<float> progress, CancellationToken token) {
			progress?.Invoke(0);

			var avatar = new AssetBundleRuntimeRuntimeAvatar {
				Path   = path,
				Bundle = GetAssetBundle(path)
			};

			avatar.Bundle ??= await AssetBundle.LoadFromFileAsync(path)
				.ToUniTask(progress: new Progress<float>(p => progress?.Invoke(p * .25f)), cancellationToken: token);

			progress?.Invoke(.25f);

			if (!avatar.Bundle) {
				Logger.LogError($"Failed to load avatar from path: {path}");
				return null;
			}

			// Load the avatar from the bundle (prefab)
			var prefab = (from a1 in avatar.Bundle.GetAllAssetNames()
				where a1.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
				select avatar.Bundle.LoadAsset<GameObject>(a1)).FirstOrDefault();

			if (!prefab) {
				Logger.LogError($"No prefab found in avatar bundle: {path}");
				await avatar.Dispose();
				return null;
			}

			prefab.SetActive(false);

			avatar.Root = (await Object.InstantiateAsync(prefab)
					.ToUniTask(progress: new Progress<float>(p => progress?.Invoke(.25f + p * .75f)), cancellationToken: token))
				.FirstOrDefault();

			if (!avatar.Root) {
				Logger.LogError($"Failed to instantiate avatar prefab from bundle: {path}");
				await avatar.Dispose();
				return null;
			}

			avatar.Root.name  = $"[{avatar.GetType().Name}_{avatar.GetId()}]";
			avatar.Descriptor = avatar.Root.GetComponent<IAvatarDescriptor>();

			if (avatar.Descriptor == null) {
				Logger.LogError($"Avatar prefab does not have a valid descriptor: {path}");
				await avatar.Dispose();
				return null;
			}

			var result = await AvatarSetup.Prepare(
				avatar,
				progress: p => progress?.Invoke(.75f + p * .25f),
				token: token
			);

			if (!result) {
				Logger.LogError($"Failed to prepare avatar: {path}");
				await avatar.Dispose();
				return null;
			}

			progress?.Invoke(1);

			avatar.Root.SetActive(false);

			return avatar;
		}

		public override async UniTask Dispose() {
			if (Root) {
				Object.Destroy(Root);
				Root = null;
			}

			if (Bundle && CanUnloadAssetBundle()) {
				await Bundle.UnloadAsync(true);
				Bundle = null;
			}

			Descriptor = null;
		}
	}
}