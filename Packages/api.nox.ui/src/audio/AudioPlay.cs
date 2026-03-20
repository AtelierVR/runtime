using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.UI.audio;
using UnityEngine;

namespace api.nox.ui.audio {
	internal class AudioPlay : IAudioPlay {
		private readonly GameObject _tempOwned;
		private bool _failed;
		private bool _started;

		internal AudioPlay(AudioSource source, GameObject tempOwned = null) {
			Source     = source;
			_tempOwned = tempOwned;
		}

		internal void MarkStarted() {
			_started = true;
			if (_tempOwned)
				AutoDestroyAsync().Forget();
		}

		internal void MarkFailed() {
			_failed = true;
			_tempOwned?.Destroy();
		}

		internal bool IsDisposed { get; private set; }
		internal AudioSource Source { get; }

		public bool IsEnded
			=> IsDisposed || _failed || (_started && !Source.isPlaying);

		public bool IsLoop
			=> !IsDisposed && _started && Source.loop;

		public void Dispose() {
			if (IsDisposed)
				return;
			IsDisposed = true;
			if (_started)
				Source.Stop();
			_tempOwned?.Destroy();
		}

		public async UniTask WhenDone(CancellationToken token = default)
			=> await UniTask.WaitUntil(() => IsEnded, cancellationToken: token);

		private async UniTaskVoid AutoDestroyAsync() {
			await WhenDone();
			_tempOwned?.Destroy();
		}
	}

	internal class NullAudioPlay : IAudioPlay {
		public bool IsEnded
			=> true;

		public bool IsLoop
			=> false;

		public void Dispose() { }

		public UniTask WhenDone(CancellationToken token = default)
			=> UniTask.CompletedTask;
	}
}