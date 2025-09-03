using System;
using UnityEngine.Events;

namespace Nox.VideoPlayer {
	public interface IVideoPlayerEvents {
		public void AddReadyListener(UnityAction listener);

		public void RemoveReadyListener(UnityAction listener);

		public void AddStartListener(UnityAction listener);

		public void RemoveStartListener(UnityAction listener);

		public void AddEndListener(UnityAction listener);

		public void RemoveEndListener(UnityAction listener);

		public void AddErrorListener(UnityAction<Exception> listener);

		public void RemoveErrorListener(UnityAction<Exception> listener);

		// Nouveaux événements
		public void AddPlayListener(UnityAction listener);

		public void RemovePlayListener(UnityAction listener);

		public void AddPauseListener(UnityAction listener);

		public void RemovePauseListener(UnityAction listener);

		public void AddResumeListener(UnityAction listener);

		public void RemoveResumeListener(UnityAction listener);

		public void AddProgressListener(UnityAction<double> listener);

		public void RemoveProgressListener(UnityAction<double> listener);

		public void AddSeekListener(UnityAction<double> listener);

		public void RemoveSeekListener(UnityAction<double> listener);

		public void AddVolumeChangedListener(UnityAction<float> listener);

		public void RemoveVolumeChangedListener(UnityAction<float> listener);

		public void AddPlayStatusChangedListener(UnityAction<bool> listener);

		public void RemovePlayStatusChangedListener(UnityAction<bool> listener);
	}
}