using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nox.UI.audio {
	public interface IAudioPlay {
		/// <summary>
		/// Whether the audio has finished playing.
		/// Note that this may not be accurate for looped audio.
		/// </summary>
		public bool IsEnded { get; }

		/// <summary>
		/// Indicate that the audio is played in loop
		/// </summary>
		public bool IsLoop { get; }

		/// <summary>
		/// Stop the audio and release resources.
		/// </summary>
		public void Dispose();

		/// <summary>
		/// Wait until the audio is done playing.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public UniTask WhenDone(CancellationToken token = default);
	}
}