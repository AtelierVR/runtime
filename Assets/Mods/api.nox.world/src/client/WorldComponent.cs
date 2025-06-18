using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Worlds;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.world.client {
	public class WorldComponent : MonoBehaviour {
		public  GameObject              withThumbnail;
		public  GameObject              withoutThumbnail;
		public  Image                   thumbnail;
		public  TextLanguage            title;
		public  TextLanguage            identifier;
		public  WorldPage               Page;
		private CancellationTokenSource _thumbnailTokenSource;

		public void UpdateError(string error) {
			title.UpdateText("world.error");
			identifier.UpdateText("world.error");
			thumbnail.sprite = null;
			thumbnail.sprite = null;
			withThumbnail.SetActive(false);
			withoutThumbnail.SetActive(true);
		}

		public void UpdateLoading() {
			title.UpdateText("world.loading");
			identifier.UpdateText("world.loading");
			thumbnail.sprite = null;
			thumbnail.sprite = null;
			withThumbnail.SetActive(false);
			withoutThumbnail.SetActive(true);
		}

		public void UpdateContent(IWorld world) {
			if (world == null) return;

			title.UpdateText("world.title", new[] { world.GetTitle() });
			identifier.UpdateText(
				"world.identifier", new[] {
					world.ToIdentifier().ToString(),
					world.GetId().ToString(),
					world.GetServerAddress()
				}
			);

			UpdateThumbnail(world).Forget();
			UpdateThumbnail(world).Forget();
		}

		private async UniTask UpdateThumbnail(IWorld world) {
			if (_thumbnailTokenSource != null) {
				_thumbnailTokenSource?.Cancel();
				_thumbnailTokenSource?.Dispose();
			}

			_thumbnailTokenSource = new CancellationTokenSource();
			if (world?.GetThumbnailUrl() != null) {
				var texture = await Main.Instance.NetworkAPI
					.FetchTexture(world.GetThumbnailUrl())
					.AttachExternalCancellation(_thumbnailTokenSource.Token);
				if (texture) {
					thumbnail.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
					withThumbnail.SetActive(true);
					withoutThumbnail.SetActive(false);
				} else {
					thumbnail.sprite = null;
					withThumbnail.SetActive(false);
					withoutThumbnail.SetActive(true);
				}
			} else {
				thumbnail.sprite = null;
				withThumbnail.SetActive(false);
				withoutThumbnail.SetActive(true);
			}

			_thumbnailTokenSource = null;
		}

		public static (GameObject, WorldComponent) Generate(WorldPage worldPage, RectTransform parent) {
			var content = Instantiate(Client.GetAsset<GameObject>("prefabs/split.prefab", "ui"), parent);

			var component = content.AddComponent<WorldComponent>();
			component.Page = worldPage;
			content.name   = $"[{worldPage.GetKey()}_{content.GetInstanceID()}]";

			var splitContent   = Reference.GetComponent<RectTransform>("content", content);
			var containerAsset = Client.GetAsset<GameObject>("prefabs/container.prefab", "ui");

			// generate profile
			var container = Instantiate(containerAsset, splitContent);
			var profile = Instantiate(
				Client.GetAsset<GameObject>("prefabs/profile.prefab"),
				Reference.GetComponent<RectTransform>("content", container)
			);
			component.identifier       = Reference.GetComponent<TextLanguage>("identifier", profile);
			component.title            = Reference.GetComponent<TextLanguage>("title", profile);
			component.thumbnail        = Reference.GetComponent<Image>("thumbnail", profile);
			component.withThumbnail    = Reference.GetReference("with_thumbnail", profile);
			component.withoutThumbnail = Reference.GetReference("without_thumbnail", profile);

			// generate dashboard
			container = Instantiate(Client.GetAsset<GameObject>("prefabs/container_full.prefab", "ui"), splitContent);

			return (content, component);
		}
	}
}