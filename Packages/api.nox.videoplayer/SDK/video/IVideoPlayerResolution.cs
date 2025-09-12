using UnityEngine;
using UnityEngine.Events;

namespace Nox.VideoPlayer {
	public interface IVideoPlayerResolution {
		public UnityEvent<IVideoPlayer, Vector2Int> OnResolutionChangedEvent();

		public Vector2Int GetResolution();
	}
}