using UnityEngine;

namespace Nox.Avatars.Voice {
	public interface IVoiceModule : IAvatarModule {
		public AudioSource GetSource();
	}
}