using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Users;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.user.client {
	public class UserComponent : MonoBehaviour {
		public GameObject        withBanner;
		public GameObject        withoutBanner;
		public Image             banner;
		public Image             thumbnail;
		public TextLanguage      display;
		public TextLanguage      identifier;
		public UserPage          Page;
		public AspectRatioFitter fitter;

		public void UpdateContent(IUser user) {
			if (user == null) return;

			display.UpdateText("user.display", new[] { user.GetDisplay() });
			identifier.UpdateText(
				"user.identifier", new[] {
					user.ToIdentifier().ToString(),
					user.GetId().ToString(),
					user.GetUsername(),
					user.GetServerAddress()
				}
			);

			UpdateThumbnail(user).Forget();
			UpdateBanner(user).Forget();
		}

		private CancellationTokenSource _thumbnailTokenSource;
		private CancellationTokenSource _bannerTokenSource;

		private async UniTask UpdateThumbnail(IUser user) {
			if (_thumbnailTokenSource != null) {
				_thumbnailTokenSource?.Cancel();
				_thumbnailTokenSource?.Dispose();
			}

			_thumbnailTokenSource = new CancellationTokenSource();
			if (user?.GetThumbnailUrl() != null) {
				var texture = await Main.NetworkAPI
					.FetchTexture(user.GetThumbnailUrl())
					.AttachExternalCancellation(_thumbnailTokenSource.Token);
				thumbnail.sprite = texture
					? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero)
					: null;
			} else thumbnail.sprite = null;

			_thumbnailTokenSource = null;
		}

		private async UniTask UpdateBanner(IUser user) {
			if (_bannerTokenSource != null) {
				_bannerTokenSource?.Cancel();
				_bannerTokenSource?.Dispose();
			}

			_bannerTokenSource = new CancellationTokenSource();
			if (user?.GetBannerUrl() != null) {
				var texture = await Main.NetworkAPI
					.FetchTexture(user.GetBannerUrl())
					.AttachExternalCancellation(_bannerTokenSource.Token);
				if (texture && texture.height > 0) {
					banner.sprite      = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
					fitter.aspectRatio = (float)texture.width / texture.height;
					if (fitter.aspectRatio <= 1.333333333) // 4:3 minimum
						fitter.aspectRatio = 1.333333333f;
					withBanner.SetActive(true);
					withoutBanner.SetActive(false);
				} else {
					banner.sprite = null;
					withBanner.SetActive(false);
					withoutBanner.SetActive(true);
				}
			} else {
				banner.sprite = null;
				withBanner.SetActive(false);
				withoutBanner.SetActive(true);
			}

			_bannerTokenSource = null;
		}

		public void UpdateError(string error) {
			display.UpdateText("user.error");
			identifier.UpdateText("user.error");
			thumbnail.sprite = null;
			banner.sprite    = null;
			withBanner.SetActive(false);
			withoutBanner.SetActive(true);
		}

		public void UpdateLoading() {
			display.UpdateText("user.loading");
			identifier.UpdateText("user.loading");
			thumbnail.sprite = null;
			banner.sprite    = null;
			withBanner.SetActive(false);
			withoutBanner.SetActive(true);
		}

		public static (GameObject, UserComponent) Generate(UserPage userPage, RectTransform parent) {
			var content = Instantiate(Client.GetAsset<GameObject>("prefabs/split.prefab", "ui"), parent);

			var component = content.AddComponent<UserComponent>();
			component.Page = userPage;
			content.name   = $"[{userPage.GetKey()}_{content.GetInstanceID()}]";

			var splitContent   = Reference.GetComponent<RectTransform>("content", content);
			var containerAsset = Client.GetAsset<GameObject>("prefabs/container.prefab", "ui");

			// generate profile
			var container = Instantiate(containerAsset, splitContent);
			var profile = Instantiate(
				Client.GetAsset<GameObject>("prefabs/profile.prefab"),
				Reference.GetComponent<RectTransform>("content", container)
			);
			component.identifier    = Reference.GetComponent<TextLanguage>("identifier", profile);
			component.display       = Reference.GetComponent<TextLanguage>("display", profile);
			component.thumbnail     = Reference.GetComponent<Image>("thumbnail", profile);
			component.banner        = Reference.GetComponent<Image>("banner", profile);
			component.withBanner    = Reference.GetReference("with_banner", profile);
			component.withoutBanner = Reference.GetReference("without_banner", profile);
			component.fitter        = Reference.GetComponent<AspectRatioFitter>("banner_aspect", profile);

			// generate dashboard
			container = Instantiate(Client.GetAsset<GameObject>("prefabs/container_full.prefab", "ui"), splitContent);

			return (content, component);
		}
	}
}