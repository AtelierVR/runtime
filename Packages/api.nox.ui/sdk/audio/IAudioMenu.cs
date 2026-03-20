using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.UI.audio {
	public enum MenuSound {
		Click,
		Hover,
		Show,
		Hide,
		Error,
		Success
	}

	public interface IAudioMenu : IMenu {
		public IAudioPlay Play(ResourceIdentifier sound, AudioSource source = null, float delay = 0f);

		public IAudioPlay Play(MenuSound sound, AudioSource source = null, float delay = 0f);

		public IAudioPlay Play(AudioClip clip, AudioSource source = null, float delay = 0f);
	}
}