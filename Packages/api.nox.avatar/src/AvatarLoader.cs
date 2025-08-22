using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.avatar {
	public class AvatarLoader {
		public static readonly List<IRuntimeAvatar> Avatar = new();

		public static readonly UnityEvent<IRuntimeAvatar> OnAdded   = new();
		public static readonly UnityEvent<IRuntimeAvatar> OnRemoved = new();

		internal static void InvokeAdded(IRuntimeAvatar runtimeAvatar) {
			Avatar.Add(runtimeAvatar);
			OnAdded.Invoke(runtimeAvatar);
			Main.Instance.CoreAPI.EventAPI.Emit("avatar_added", runtimeAvatar);
		}

		internal static void InvokeRemoved(IRuntimeAvatar runtimeAvatar) {
			Avatar.Remove(runtimeAvatar);
			OnRemoved.Invoke(runtimeAvatar);
			Main.Instance.CoreAPI.EventAPI.Emit("avatar_removed", runtimeAvatar);
		}

		[NoxPublic(NoxAccess.Method)]
		public static async UniTask<AssetBundleRuntimeRuntimeAvatar> LoadFromCache(string hash, Action<float> progress = null, CancellationToken token = default) {
			Logger.Log($"Loading avatar from cache: {hash}");

			var path = AvatarCache.GetIfExist(hash);
			if (!string.IsNullOrEmpty(path))
				return await LoadFromPath(path, progress, token);

			Logger.LogError($"Avatar with hash {hash} not found in cache.");

			return null;
		}

		[NoxPublic(NoxAccess.Method)]
		public static async UniTask<AssetBundleRuntimeRuntimeAvatar> LoadFromPath(string path, Action<float> progress = null, CancellationToken token = default) {
			Logger.Log($"Loading avatar from path: {path}");

			var avatar = await AssetBundleRuntimeRuntimeAvatar.Load(path, progress, token);
			if (avatar == null) {
				Logger.LogError($"Failed to load world from path: {path}");
				return null;
			}

			InvokeAdded(avatar);
			return avatar;
		}

		[NoxPublic(NoxAccess.Method)]
		public static async UniTask<AssetRuntimeRuntimeAvatar> LoadFromAssets(string ns, string path, Action<float> progress = null, CancellationToken token = default) {
			Logger.Log($"Loading avatar from assets: {ns}:{path}");	
			
			var avatar = await AssetRuntimeRuntimeAvatar.Load(ns, path, progress, token);

			if (avatar == null) {
				Logger.LogError($"Failed to load world from assets: {ns}:{path}");
				return null;
			}

			InvokeAdded(avatar);
			return avatar;
		}
	}
}