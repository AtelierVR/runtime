using UnityEngine;

namespace Nox.Entities {
	public interface IAudioEntity {
		public AudioClip GetAudio();

		public void SetAudio(AudioClip clip);
	}
}