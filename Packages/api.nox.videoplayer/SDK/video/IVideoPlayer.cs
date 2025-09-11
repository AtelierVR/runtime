using System;
using UnityEngine;
using UnityEngine.Events;

namespace Nox.VideoPlayer {
	public interface IVideoPlayer {
		public bool IsPlaying();

		public void Play(string query);

		public void Play(string query, bool loop);

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

		public UnityEvent<IVideoPlayer> OnReadyEvent();

		public UnityEvent<IVideoPlayer> OnStartEvent();

		public UnityEvent<IVideoPlayer> OnPlayEvent();

		public UnityEvent<IVideoPlayer> OnPauseEvent();

		public UnityEvent<IVideoPlayer> OnResumeEvent();

		public UnityEvent<IVideoPlayer> OnEndEvent();

		public UnityEvent<IVideoPlayer, double> OnProgressEvent();

		public UnityEvent<IVideoPlayer, double> OnSeekEvent();

		public UnityEvent<IVideoPlayer, float> OnVolumeChangedEvent();

		public UnityEvent<IVideoPlayer, bool> OnPlayStatusChangedEvent();

		public UnityEvent<IVideoPlayer, Exception> OnErrorEvent();
	}
}