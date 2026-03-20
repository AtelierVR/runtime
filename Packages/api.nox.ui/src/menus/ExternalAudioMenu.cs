using System;
using api.nox.ui.audio;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.UI.audio;
using UnityEngine;
using UnityEngine.Audio;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui.menus {
	public class ExternalAudioMenu : MonoBehaviour {
		[Header("Default Source")]
		public AudioSource defaultSource;

		[Header("Generic Sounds")]
		public AudioClip clickSound;
		public AudioClip hoverSound;
		public AudioClip showSound;
		public AudioClip hideSound;
		public AudioClip errorSound;
		public AudioClip successSound;

		private AudioClip GetClip(MenuSound sound)
			=> sound switch {
				MenuSound.Click   => clickSound,
				MenuSound.Hover   => hoverSound,
				MenuSound.Show    => showSound,
				MenuSound.Hide    => hideSound,
				MenuSound.Error   => errorSound,
				MenuSound.Success => successSound,
				_                 => null
			};

		// Returns a source guaranteed to be on an active GameObject.
		// When the desired source is inactive, a temporary root-level
		// AudioSource is created; ownership is conveyed via tempOwned.
		private AudioSource ResolveSource(AudioSource requested, out GameObject tempOwned) {
			tempOwned = null;
			var candidate = requested ? requested : defaultSource;
			if (candidate && candidate.isActiveAndEnabled)
				return candidate;

			// Detach: create a root-level AudioSource that survives deactivation
			var go = new GameObject("[AudioMenu] Detached");
			
			go.transform.position   = gameObject.transform.position;
			go.transform.rotation   = gameObject.transform.rotation;
			
			go.DontDestroyOnLoad();
			var src = go.AddComponent<AudioSource>();
			if (candidate) {
				src.outputAudioMixerGroup = candidate.outputAudioMixerGroup;
				src.volume                = candidate.volume;
				src.pitch                 = candidate.pitch;
				src.spatialBlend          = 0f; // 2D for UI sounds
			}
			tempOwned = go;
			return src;
		}

		public IAudioPlay Play(ResourceIdentifier sound, AudioSource source = null, float delay = 0f) {
			var src = ResolveSource(source, out var temp);
			if (!src) {
				Debug.LogWarning("[ExternalAudioMenu] No AudioSource available.");
				return new NullAudioPlay();
			}
			var play = new AudioPlay(src, temp);
			PlayResourceAsync(play, sound, delay).Forget();
			return play;
		}

		public IAudioPlay Play(MenuSound sound, AudioSource source = null, float delay = 0f) {
			var clip = GetClip(sound);
			if (!clip)
				return new NullAudioPlay();
			var src = ResolveSource(source, out var temp);
			if (!src) {
				Debug.LogWarning("[ExternalAudioMenu] No AudioSource available.");
				return new NullAudioPlay();
			}
			src.clip = clip;
			if (delay > 0f)
				src.PlayDelayed(delay);
			else
				src.Play();
			var play = new AudioPlay(src, temp);
			play.MarkStarted();
			return play;
		}

		public IAudioPlay Play(AudioClip clip, AudioSource source = null, float delay = 0f) {
			if (!clip)
				return new NullAudioPlay();
			var src = ResolveSource(source, out var temp);
			if (!src) {
				Debug.LogWarning("[ExternalAudioMenu] No AudioSource available.");
				return new NullAudioPlay();
			}
			src.clip = clip;
			if (delay > 0f)
				src.PlayDelayed(delay);
			else
				src.Play();
			var play = new AudioPlay(src, temp);
			play.MarkStarted();
			return play;
		}

		private async UniTaskVoid PlayResourceAsync(AudioPlay play, ResourceIdentifier sound, float delay) {
			try {
				var clip = await PageManager.GetAssetAsync<AudioClip>(sound);
				if (play.IsDisposed || !clip) {
					play.MarkFailed();
					return;
				}
				play.Source.clip = clip;
				if (delay > 0f)
					play.Source.PlayDelayed(delay);
				else
					play.Source.Play();
				play.MarkStarted();
			} catch (Exception e) {
				Logger.LogError(e);
				play.MarkFailed();
			}
		}
	}
}