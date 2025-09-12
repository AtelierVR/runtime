using System;
using Nox.VideoPlayer;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Hactazia.VideoPlayer {
	public class AspectRatioUpdater : MonoBehaviour {
		public NhVideoPlayer     player;
		public AspectRatioFitter fitter;

		private static bool TryResolution(IVideoPlayer p, out IVideoPlayerResolution resolution) {
			if (p is IVideoPlayerResolution res) {
				resolution = res;
				return true;
			}

			resolution = null;
			return false;
		}

		private void Awake() {
			player ??= GetComponent<NhVideoPlayer>();
			fitter ??= GetComponent<AspectRatioFitter>();
			if (!player) {
				Logger.LogError("AspectRatioUpdater requires a NhVideoPlayer component.");
				enabled = false;
				return;
			}

			if (!fitter) {
				Logger.LogError("AspectRatioUpdater requires an AspectRatioFitter component.");
				enabled = false;
				return;
			}

			if (TryResolution(player, out var resolution)) {
				resolution.OnResolutionChangedEvent().AddListener(OnResolutionChange);
				OnResolutionChange(player, resolution.GetResolution());
				return;
			}

			player.OnStartEvent().AddListener(OnPlayerStart);
		}

		private void OnResolutionChange(IVideoPlayer _, Vector2Int resolution) {
			if (resolution.x == 0 || resolution.y == 0) {
				fitter.aspectRatio = 1f;
				return;
			}

			fitter.aspectRatio = (float)resolution.x / resolution.y;
		}

		private void OnPlayerStart(IVideoPlayer _) {
			if (TryResolution(player, out var resolution)) {
				OnResolutionChange(player, resolution.GetResolution());
				return;
			}

			var tex = player.GetRender();
			if (tex)
				fitter.aspectRatio = (float)tex.width / tex.height;
		}

		private void OnDestroy() {
			if (TryResolution(player, out var resolution))
				resolution.OnResolutionChangedEvent().RemoveListener(OnResolutionChange);
			player.OnStartEvent().RemoveListener(OnPlayerStart);
		}
	}
}