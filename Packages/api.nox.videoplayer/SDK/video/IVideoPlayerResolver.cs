using UnityEngine.Events;

namespace Nox.VideoPlayer {
	public interface IVideoPlayerResolver {
		public void OnResolve(IFetchOptions initial, IResult[] results);

		public UnityEvent<IVideoPlayer, IFetchOptions> OnResolvingEvent();

		public UnityEvent<IVideoPlayer, IFetchOptions, IResult[]> OnResolvedEvent();
	}
}