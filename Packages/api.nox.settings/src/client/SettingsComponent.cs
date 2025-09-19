using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;
using Transform = UnityEngine.Transform;

namespace api.nox.settings.client {
	public class SettingsComponent : MonoBehaviour {
		public  Image                   labelIcon;
		public  RectTransform           content;
		public  GameObject              header;
		public  TextLanguage            title;
		private CancellationTokenSource _thumbnailTokenSource;
		public  RectTransform           navigation;
		public  string                  currentHandler;
		public  GameObject              leftContainer;

		public SettingsPage Page;

		public static (GameObject, SettingsComponent) Generate(SettingsPage settingsPage, RectTransform parent) {
			var iconAsset      = Client.GetAsset<GameObject>("prefabs/header_icon.prefab", "ui");
			var labelAsset     = Client.GetAsset<GameObject>("prefabs/header_label.prefab", "ui");
			var withTitleAsset = Client.GetAsset<GameObject>("prefabs/with_title.prefab", "ui");
			var listAsset      = Client.GetAsset<GameObject>("prefabs/list.prefab", "ui");
			var scrollAsset    = Client.GetAsset<GameObject>("prefabs/scroll.prefab", "ui");
			var containerAsset = Client.GetAsset<GameObject>("prefabs/container.prefab", "ui");

			var content = Instantiate(Client.GetAsset<GameObject>("prefabs/split.prefab", "ui"), parent);

			var component = content.AddComponent<SettingsComponent>();
			component.Page = settingsPage;
			content.name   = $"[{settingsPage.GetKey()}_{content.GetInstanceID()}]";

			var splitContent = Reference.GetComponent<RectTransform>("content", content);

			// left container

			component.leftContainer = Instantiate(containerAsset, splitContent);

			var navigation = Instantiate(
				scrollAsset,
				Reference.GetComponent<RectTransform>("content", component.leftContainer)
			);

			component.navigation = Reference.GetComponent<RectTransform>(
				"content", Instantiate(
					listAsset,
					Reference.GetComponent<RectTransform>("content", navigation)
				)
			);

			// container
			var container = Instantiate(Client.GetAsset<GameObject>("prefabs/container_full.prefab", "ui"), splitContent);
			var withTitle = Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));

			component.header = Reference.GetReference("header", withTitle);
			var icon  = Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", component.header));
			var title = Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", component.header));

			component.labelIcon        = Reference.GetComponent<Image>("image", icon);
			component.title            = Reference.GetComponent<TextLanguage>("text", title);
			component.labelIcon.sprite = Client.GetAsset<Sprite>("icons/globe.png", "ui");

			var contentDash = Reference.GetComponent<RectTransform>("content", withTitle);
			// setup scroll + list
			var scroll = Instantiate(scrollAsset, contentDash);
			var list   = Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", scroll));
			component.content = Reference.GetComponent<RectTransform>("content", list);

			return (content, component);
		}


		public void UpdateTitles() {
			var settings = Page.GetCategory();
			if (settings == null) {
				title.UpdateText("settings.no_settings.title");
				return;
			}

			title.UpdateText(settings.GetTitle());
		}

		public async UniTask UpdateNavigation() {
			var btn = await Client.GetAssetAsync<GameObject>("prefabs/btn_icon.prefab", "ui");

			var page = Main.Handlers
				.Select(hand => hand.GetPath().FirstOrDefault())
				.Distinct()
				.Select(p => Page.GetCategory(p));

			foreach (Transform tf in navigation)
				Destroy(tf.gameObject);

			foreach (var p in page) {
				var o = Instantiate(btn, navigation);
				UpdateIcon(p, o).Forget();
				var text = Reference.GetComponent<TextLanguage>("text", o);
				text.UpdateText(p.GetLabel());

				var button = o.GetComponent<Button>();
				button.onClick.AddListener(() => OnChangePage(p.GetId()));

				o.name = $"{p.GetId()}_{o.GetInstanceID()}";
				o.SetActive(true);
			}

			UpdateLayout.UpdateImmediate(leftContainer);
		}

		private static async UniTask UpdateIcon(CategoryDetails category, GameObject o) {
			var image          = Reference.GetComponent<Image>("image", o);
			var imageContainer = Reference.GetComponent<RectTransform>("image_container", o);
			if (!image.sprite)
				imageContainer.gameObject.SetActive(false);
			var icon = await category.GetIcon();
			if (icon) {
				image.sprite = Sprite.Create(
					icon,
					new Rect(0, 0, icon.width, icon.height),
					new Vector2(0.5f, 0.5f)
				);
				imageContainer.gameObject.SetActive(true);
			} else imageContainer.gameObject.SetActive(false);

			UpdateLayout.UpdateImmediate(o);
		}

		internal async UniTask UpdateContent() {
			var box  = await Client.GetAssetAsync<GameObject>("prefabs/box.prefab", "ui");
			var list = await Client.GetAssetAsync<GameObject>("prefabs/list.prefab", "ui");

			foreach (Transform tf in content)
				Destroy(tf.gameObject);

			var details = Page.GetCategory();
			if (details == null) {
				Debug.LogWarning("No category selected.");
				return;
			}

			var groups = Page.GetGroups(details.GetId());

			foreach (var group in groups) {
				var groupBox = (await InstantiateAsync(box, content)).FirstOrDefault();
				if (!groupBox) continue;
				var cont    = Reference.GetComponent<RectTransform>("content", groupBox);
				var listBox = (await InstantiateAsync(box, cont)).FirstOrDefault();
				if (!listBox) continue;
				cont = Reference.GetComponent<RectTransform>("content", listBox);
				foreach (var handler in group.Handlers) {
					var handlerBox = await handler.GetContentAsync(cont) ?? handler.GetContent(cont);
					if (!handlerBox) {
						Debug.LogWarning($"Handler {handler.ToID()} does not have content.");
						continue;
					}

					handlerBox.name = $"{handler.GetPath().LastOrDefault()}_{handlerBox.GetInstanceID()}";
					handlerBox.SetActive(true);
				}
			}

			UpdateLayout.UpdateImmediate(content);
		}


		private void OnChangePage(string page) {
			if (string.IsNullOrEmpty(page)) return;
		}
	}
}