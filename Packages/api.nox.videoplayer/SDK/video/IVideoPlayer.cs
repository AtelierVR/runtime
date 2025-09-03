using UnityEngine;

namespace Nox.VideoPlayer {
	public interface IVideoPlayer {
		public bool IsPlaying();

		public void Play(string url, bool loop = false);

		public void Stop();

		public void Pause();

		public void Resume();

		public float GetVolume();

		public void SetVolume(float volume);

		public void SetSeek(double time);

		public double GetTime();

		public double GetDuration();
		
		public double GetProgress();
		
		public bool IsLooping();

		public void SetLooping(bool loop);

		public RenderTexture GetRender();
	}
}