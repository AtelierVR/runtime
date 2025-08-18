using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.avatar {
	public class AvatarLoader {
		public static readonly List<IAvatar> Avatar = new();

		public static readonly UnityEvent<IAvatar> OnAdded   = new();
		public static readonly UnityEvent<IAvatar> OnRemoved = new();

		internal static void InvokeAdded(IAvatar avatar) {
			Avatar.Add(avatar);
			OnAdded.Invoke(avatar);
			Main.Instance.CoreAPI.EventAPI.Emit("avatar_added", avatar);
		}

		internal static void InvokeRemoved(IAvatar avatar) {
			Avatar.Remove(avatar);
			OnRemoved.Invoke(avatar);
			Main.Instance.CoreAPI.EventAPI.Emit("avatar_removed", avatar);
		}

		[NoxPublic(NoxAccess.Method)]
		public static async UniTask<AssetBundleAvatar> LoadFromCache(string hash, Action<float> progress = null, CancellationToken token = default) {
			Logger.Log($"Loading avatar from cache: {hash}");

			var path = AvatarCache.GetIfExist(hash);
			if (!string.IsNullOrEmpty(path))
				return await LoadFromPath(path, progress, token);

			Logger.LogError($"Avatar with hash {hash} not found in cache.");

			return null;
		}

		[NoxPublic(NoxAccess.Method)]
		public static async UniTask<AssetBundleAvatar> LoadFromPath(string path, Action<float> progress = null, CancellationToken token = default) {
			Logger.Log($"Loading avatar from path: {path}");

			var avatar = await AssetBundleAvatar.Load(path, progress, token);
			if (avatar == null) {
				Logger.LogError($"Failed to load world from path: {path}");
				return null;
			}

			InvokeAdded(avatar);
			return avatar;
		}

		[NoxPublic(NoxAccess.Method)]
		public static async UniTask<AssetAvatar> LoadFromAssets(string ns, string path, Action<float> progress = null, CancellationToken token = default) {
			Logger.Log($"Loading avatar from assets: {ns}:{path}");	
			
			var avatar = await AssetAvatar.Load(ns, path, progress, token);

			if (avatar == null) {
				Logger.LogError($"Failed to load world from assets: {ns}:{path}");
				return null;
			}

			InvokeAdded(avatar);
			return avatar;
		}
	}
}