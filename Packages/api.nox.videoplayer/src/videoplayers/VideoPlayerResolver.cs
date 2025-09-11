using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.VideoPlayer;

namespace api.nox.videoplayer {
	public class VideoPlayerResolver {
		public static void Listen() {
			VideoPlayerManager.OnRegistered.AddListener(OnRegistered);
			VideoPlayerManager.OnUnRegistered.AddListener(OnUnRegistered);
			foreach (var player in VideoPlayerManager.VideoPlayers)
				OnRegistered(player);
		}

		public static void UnListen() {
			VideoPlayerManager.OnRegistered.RemoveListener(OnRegistered);
			VideoPlayerManager.OnUnRegistered.RemoveListener(OnUnRegistered);
			foreach (var player in VideoPlayerManager.VideoPlayers)
				OnUnRegistered(player);
		}

		private static void OnUnRegistered(IVideoPlayer arg0) {
			var player = arg0.GetResolver();
			if (player == null) return;
			Logger.LogDebug($"Unregistered video player {arg0} from resolving");
			player.OnResolvingEvent().RemoveListener(OnResolving);
		}

		private static void OnRegistered(IVideoPlayer arg0) {
			var player = arg0.GetResolver();
			if (player == null) return;
			Logger.LogDebug($"Registered video player {arg0} for resolving");
			player.OnResolvingEvent().AddListener(OnResolving);
		}

		private static void OnResolving(IVideoPlayer arg0, IFetchOptions arg1)
			=> OnResolvingAsync(arg0, arg1).Forget();

		private static async UniTask OnResolvingAsync(IVideoPlayer arg0, IFetchOptions arg1) {
			var player = arg0.GetResolver();
			if (player == null) {
				Logger.LogWarning($"Video player {arg0} does not implement IVideoPlayerResolver");
				return;
			}

			Logger.LogDebug($"Resolving video for player {arg0} with options {arg1}");

			var handlers = Main.Handlers;
			var results  = new List<IResult>();

			await UniTask.SwitchToMainThread();
			await UniTask.WhenAll(Enumerable.Select(handlers, Resolver));

			player.OnResolve(arg1, results.ToArray());

			return;

			async UniTask Resolver(IHandler handler) {
				var res = await handler.Fetch(arg1);
				if (res == null || res.Length == 0) return;
				results.AddRange(res);
			}
		}
	}
}