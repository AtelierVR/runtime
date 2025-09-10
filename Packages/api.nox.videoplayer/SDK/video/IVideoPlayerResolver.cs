using UnityEngine.Events;

namespace Nox.VideoPlayer {
	public interface IVideoPlayerResolver {
		public void OnResolve(IFetchOptions initial, IResult[] results);

		public void AddResolvingListener(UnityAction<IVideoPlayer, IFetchOptions> listener);

		public void RemoveResolvingListener(UnityAction<IVideoPlayer, IFetchOptions> listener);

		public void AddResolvedListener(UnityAction<IVideoPlayer, IFetchOptions, IResult[]> listener);

		public void RemoveResolvedListener(UnityAction<IVideoPlayer, IFetchOptions, IResult[]> listener);
	}
}