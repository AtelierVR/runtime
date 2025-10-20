using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.VideoPlayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace api.nox.videoplayer.client {
	public class VideoPlayerComponent : MonoBehaviour {
		private VideoPlayerPage _page;

		public AspectRatioFitter ratio;
		public Image             video;
		public Slider            seek;
		public Slider            loaded;
		public TextLanguage      current;
		public TextLanguage      total;
		public Button            center;
		public Image             centerIcon;

		private float _targetSeekValue;

		// Variables pour gérer le seek par l'utilisateur
		private bool _isUserSeeking;
		private bool _wasPlayingBeforeSeek;

		public static (GameObject, VideoPlayerComponent) Generate(VideoPlayerPage page, RectTransform parent) {
			var content = Instantiate(Client.GetAsset<GameObject>("prefabs/split.prefab", "ui"), parent);

			var component = content.AddComponent<VideoPlayerComponent>();
			component._page = page;
			content.name    = $"[{page.GetKey()}_{content.GetInstanceID()}]";
			var splitContent = Reference.GetComponent<RectTransform>("content", content);

			// generate dashboard
			var container = Instantiate(Client.GetAsset<GameObject>("prefabs/container_full.prefab", "ui"), splitContent);
			var videoPlayer = Instantiate(
				Client.GetAsset<GameObject>("prefabs/video_player.prefab", "ui"),
				Reference.GetComponent<RectTransform>("content", container)
			);
			component.video          = Reference.GetComponent<Image>("video", videoPlayer);
			component.ratio          = Reference.GetComponent<AspectRatioFitter>("ratio", videoPlayer);
			component.video.material = Instantiate(Client.GetAsset<Material>("materials/video_texture.mat", "ui"));

			component.seek                = Reference.GetComponent<Slider>("seek", container);
			component.current             = Reference.GetComponent<TextLanguage>("current", container);
			component.total               = Reference.GetComponent<TextLanguage>("total", container);
			component.loaded              = Reference.GetComponent<Slider>("loaded", container);
			component.center              = Reference.GetComponent<Button>("center", videoPlayer);
			component.centerIcon          = Reference.GetComponent<Image>("image", component.center.gameObject);
			component.loaded.interactable = false;

			component.seek.onValueChanged.AddListener(component.OnSeekValueChanged);
			var trigger          = component.seek.gameObject.GetOrAddComponent<EventTrigger>();
			var pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
			pointerDownEntry.callback.AddListener(_ => component.OnSeekStart());
			trigger.triggers.Add(pointerDownEntry);
			var pointerUpEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
			pointerUpEntry.callback.AddListener(_ => component.OnSeekEnd());
			trigger.triggers.Add(pointerUpEntry);
			var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
			dragEntry.callback.AddListener(_ => component.OnSeekDrag());
			trigger.triggers.Add(dragEntry);
			component.center.onClick.AddListener(page.TogglePlayPause);

			return (content, component);
		}

		public void UpdateProgress(IVideoPlayer player, double progress) {
			if (player == null) return;
			if (!_isUserSeeking)
				seek.SetValueWithoutNotify((float)progress);
			loaded.value = 0;
			FormatTime(current, player.GetTime());
			FormatTime(total, player.GetDuration());
		}

		public void UpdatePlayStatus(IVideoPlayer player, bool isPlaying) {
			if (player == null) return;
			var iconName = isPlaying ? "icons/pause.png" : "icons/play_arrow.png";
			var icon     = Client.GetAsset<Sprite>(iconName, "ui");
			if (icon)
				centerIcon.sprite = icon;
		}

		private static void FormatTime(TextLanguage text, double time) {
			if (time < 0)
				time = 0;
			var ts = System.TimeSpan.FromSeconds(time);
			text.UpdateText(
				ts.Hours > 0 && LanguageManager.Has("video_player.progress.hours")
					? "video_player.progress.hours"
					: "video_player.progress",
				new[] {
					$"{ts.Hours:D2}",
					$"{ts.Minutes:D2}",
					$"{ts.Seconds:D2}",
					$"{ts.Milliseconds:D2}"
				}
			);
		}

		private void UpdateRender(IVideoPlayer player) {
			var render = player?.GetRender();
			if (!render) return;
			video.material.mainTexture = render;
			ratio.aspectRatio          = (float)render.width / render.height;
		}

		public void UpdateUI() {
			if (!gameObject.activeInHierarchy) return;
			var player = _page.GetSelectedPlayer();
			if (player == null) return;
			UpdateRender(player);
			UpdatePlayStatus(player, player.IsPlaying());
			UpdateProgress(player, (float)(player.GetTime() / player.GetDuration()));
		}

		private void OnSeekValueChanged(float value) {
			// Ne rien faire si le seek est en cours par l'utilisateur
			if (_isUserSeeking) return;

			var player = _page.GetSelectedPlayer();
			if (player == null) return;

			// Calculer le nouveau temps de lecture basé sur la valeur du seek
			var newSeek = player.GetDuration() * value;

			// Mettre à jour le seek du player
			player.SetSeek(newSeek);

			// Sauvegarder l'état de lecture avant le seek
			_wasPlayingBeforeSeek = player.IsPlaying();
			if (_wasPlayingBeforeSeek) {
				player.Pause();
			}
		}

		public void OnSeekStart() {
			_isUserSeeking = true;
		}

		public void OnSeekEnd() {
			_isUserSeeking = false;
			var player = _page.GetSelectedPlayer();
			if (player == null) return;
			player.SetSeek(seek.value * player.GetDuration());
			if (_wasPlayingBeforeSeek)
				player.Resume();
			UpdateProgress(player, seek.value);
		}

		public void OnSeekDrag() {
			var player = _page.GetSelectedPlayer();
			if (player == null) return;
			player.SetSeek(seek.value * player.GetDuration());
			UpdateProgress(player, seek.value);
		}
	}
}