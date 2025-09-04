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

		// Système de file d'attente pour limiter le nombre de chargements simultanés
		private static readonly SemaphoreSlim LoadingSemaphore     = new SemaphoreSlim(3, 3);
		private static          int           _currentLoadingCount = 0;

		/// <summary>
		/// Nombre actuel d'avatars en cours de chargement
		/// </summary>
		public static int CurrentLoadingCount
			=> _currentLoadingCount;

		/// <summary>
		/// Nombre maximum d'avatars pouvant être chargés simultanément
		/// </summary>
		public static int MaxConcurrentLoads
			=> 3;

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
			// Attendre qu'un slot de chargement soit disponible
			await LoadingSemaphore.WaitAsync(token);

			try {
				Interlocked.Increment(ref _currentLoadingCount);
				Logger.Log($"Loading avatar from path: {path} (Queue: {_currentLoadingCount}/{MaxConcurrentLoads})");

				var avatar = await AssetBundleRuntimeRuntimeAvatar.Load(path, progress, token);
				if (avatar == null) {
					Logger.LogError($"Failed to load avatar from path: {path}");
					return null;
				}

				InvokeAdded(avatar);
				return avatar;
			} finally {
				Interlocked.Decrement(ref _currentLoadingCount);
				LoadingSemaphore.Release();
			}
		}

		[NoxPublic(NoxAccess.Method)]
		public static async UniTask<AssetRuntimeRuntimeAvatar> LoadFromAssets(string ns, string path, Action<float> progress = null, CancellationToken token = default) {
			// Attendre qu'un slot de chargement soit disponible
			await LoadingSemaphore.WaitAsync(token);

			try {
				Interlocked.Increment(ref _currentLoadingCount);
				Logger.Log($"Loading avatar from assets: {ns}:{path} (Queue: {_currentLoadingCount}/{MaxConcurrentLoads})");

				var avatar = await AssetRuntimeRuntimeAvatar.Load(ns, path, progress, token);

				if (avatar == null) {
					Logger.LogError($"Failed to load avatar from assets: {ns}:{path}");
					return null;
				}

				InvokeAdded(avatar);
				return avatar;
			} finally {
				Interlocked.Decrement(ref _currentLoadingCount);
				LoadingSemaphore.Release();
			}
		}
	}
}