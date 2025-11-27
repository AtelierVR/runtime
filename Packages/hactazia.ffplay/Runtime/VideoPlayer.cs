using FFmpeg.Unity;
using Hactazia.FFPlay;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hactazia.FFPlay {
	public class VideoPlayer : MonoBehaviour {
		public Player player;
		public string url;
		public bool   autoPlay = true;

		private void Start() {
			Initializer.EnsureInitialized();
			if (autoPlay)
				Play();
		}

		public void Play(string u)
			=> player.Play(u);

		[ContextMenu(nameof(Play))]
		public void Play() {
			if (string.IsNullOrEmpty(url))
				return;
			Debug.Log("Start");
			Play(url);
			Debug.Log("Done");
		}
	}
}