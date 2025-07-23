using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Worlds;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world.cache {
	public class Caching : ICaching {
		private          float                   _progress;
		public readonly  string                  Identifier;
		public readonly  uint                    AssetId;
		public readonly  string                  Hash;
		private          CancellationTokenSource _cts;
		private readonly Cache                   _cache;

		public Caching(Cache cache, string identifier, uint assetId, string hash = null) {
			Identifier = identifier;
			_cache     = cache;
			AssetId    = assetId;
			Hash       = hash;
			_progress  = 0f;
		}

		public readonly UnityEvent<float> OnProgress = new();

		private void SetProgress(float value) {
			_progress = value;
			SendEvent();
			OnProgress.Invoke(_progress);
		}

		private void SendEvent()
			=> Main.Instance.CoreAPI.EventAPI.Emit("world_cache_download", Identifier, IsRunning(), GetProgress());

		public bool IsRunning()
			=> _cts is { IsCancellationRequested: false };

		public void Cancel() {
			if (_cts is not { IsCancellationRequested: false }) {
				Logger.Log("Cancellation requested but not running.");
				return;
			}

			_cts.Cancel();
			_cts.Dispose();
			_cts = null;
			SendEvent();
		}

		public async UniTask Wait() {
			if (!IsRunning()) return;
			Logger.Log($"Waiting for download to complete: {Identifier} (AssetId: {AssetId}, Hash: {Hash})");
			await UniTask.WaitUntil(() => _cts is { IsCancellationRequested: true } || _progress >= 1f);
		}

		public UnityEvent<float> GetProgressEvent()
			=> OnProgress;

		public float GetProgress()
			=> _progress;

		public async UniTask Start() {
			if (IsRunning()) {
				Logger.Log("Already running.");
				return;
			}

			_cts = new CancellationTokenSource();
			_cache.Caching.Add(this);
			Logger.Log($"Starting download for {Identifier} (AssetId: {AssetId}, Hash: {Hash})");

			try {
				SetProgress(0f);

				// Download
				var path = await Main.Instance.Network
					.DownloadAssetFile(Identifier, AssetId, Hash, null, SetProgress)
					.AttachExternalCancellation(_cts.Token);

				if (_cts.IsCancellationRequested) {
					SetProgress(0f);
					throw new OperationCanceledException("Download was cancelled.");
				}

				if (string.IsNullOrEmpty(path)) {
					SetProgress(0f);
					throw new Exception("Download failed: No data received.");
				}

				// Save to cache
				Logger.Log($"Download completed for {Identifier} (AssetId: {AssetId}, Hash: {Hash}). Saving to cache...");
				Main.Instance.Cache.Save(Hash, path);

				SetProgress(1f);
			} catch (Exception e) {
				// Handle other exceptions
				Logger.LogException(e);
				SetProgress(0f);
			}

			_cache.Caching.Remove(this);
			_cts.Dispose();
			_cts = null;

			SendEvent();
		}
	}
}