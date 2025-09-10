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

		public void AddReadyListener(UnityAction<IVideoPlayer> listener);

		public void RemoveReadyListener(UnityAction<IVideoPlayer> listener);

		public void AddStartListener(UnityAction<IVideoPlayer> listener);

		public void RemoveStartListener(UnityAction<IVideoPlayer> listener);

		public void AddEndListener(UnityAction<IVideoPlayer> listener);

		public void RemoveEndListener(UnityAction<IVideoPlayer> listener);

		public void AddErrorListener(UnityAction<IVideoPlayer, Exception> listener);

		public void RemoveErrorListener(UnityAction<IVideoPlayer, Exception> listener);

		// Nouveaux événements
		public void AddPlayListener(UnityAction<IVideoPlayer> listener);

		public void RemovePlayListener(UnityAction<IVideoPlayer> listener);

		public void AddPauseListener(UnityAction<IVideoPlayer> listener);

		public void RemovePauseListener(UnityAction<IVideoPlayer> listener);

		public void AddResumeListener(UnityAction<IVideoPlayer> listener);

		public void RemoveResumeListener(UnityAction<IVideoPlayer> listener);

		public void AddProgressListener(UnityAction<IVideoPlayer, double> listener);

		public void RemoveProgressListener(UnityAction<IVideoPlayer, double> listener);

		public void AddSeekListener(UnityAction<IVideoPlayer, double> listener);

		public void RemoveSeekListener(UnityAction<IVideoPlayer, double> listener);

		public void AddVolumeChangedListener(UnityAction<IVideoPlayer, float> listener);

		public void RemoveVolumeChangedListener(UnityAction<IVideoPlayer, float> listener);

		public void AddPlayStatusChangedListener(UnityAction<IVideoPlayer, bool> listener);

		public void RemovePlayStatusChangedListener(UnityAction<IVideoPlayer, bool> listener);
	}
}