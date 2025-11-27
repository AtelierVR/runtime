using UnityEngine.Events;

namespace Nox.VideoPlayer {
	public interface IVideoPlayerResolver {
		/// <summary>
		/// Invoked when the resolver has result(s).
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="results"></param>
		public void OnResolve(IFetchOptions initial, IResult[] results);

		/// <summary>
		/// Invoked when starting to resolve.
		/// Is used to send resolving to the resolver.
		/// </summary>
		public UnityEvent<IVideoPlayer, IFetchOptions> OnResolving { get; }

		/// <summary>
		/// Invoked when the resolver has resolved result(s).
		/// Is used when the player has received resolved results.
		/// </summary>
		public UnityEvent<IVideoPlayer, IFetchOptions, IResult[]> OnResolved { get; }
	}
}