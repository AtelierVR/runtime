using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Servers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace api.nox.server.client {
	public class ServerComponent : MonoBehaviour {
		public  Image                   icon;
		public  TextLanguage            title;
		public  TextLanguage            identifier;
		public  TextLanguage            label;
		public  Image                   labelIcon;
		public  RectTransform           content;
		public  ServerPage              Page;
		private CancellationTokenSource _thumbnailTokenSource;
		public  GameObject              descriptionContainer;
		public  TextLanguage            descriptionText;

		public void UpdateError(string error) {
			title.UpdateText("server.error");
			identifier.UpdateText("server.error");
			label.UpdateText("server.error");
			icon.sprite = null;
			icon.sprite = null;
			descriptionContainer.SetActive(false);
		}

		public void UpdateLoading() {
			title.UpdateText("server.loading");
			identifier.UpdateText("server.loading");
			label.UpdateText("server.loading");
			icon.sprite = null;
			icon.sprite = null;
			descriptionContainer.SetActive(false);
		}

		public void UpdateContent(IServer server) {
			if (server == null) return;

			title.UpdateText("server.title", new[] { server.Metadata?.Title });
			label.UpdateText("server.about.title", new[] { server.Metadata?.Title ?? server.Address });
			identifier.UpdateText("server.identifier", new[] { server.Address });

			if (!string.IsNullOrEmpty(server.Metadata?.Description)) {
				descriptionText.SetMarkdown(server.Metadata?.Description);
				descriptionContainer.SetActive(true);
			} else descriptionContainer.SetActive(false);

			UpdateIcon(server).Forget();

			UpdateLayout.UpdateImmediate(content);
		}

		private async UniTask UpdateIcon(IServer world) {
			if (_thumbnailTokenSource != null) {
				_thumbnailTokenSource?.Cancel();
				_thumbnailTokenSource?.Dispose();
			}

			_thumbnailTokenSource = new CancellationTokenSource();

			if (world?.Metadata?.Icon != null) {
				var texture = await Client.NetworkAPI.FetchTexture(world.Metadata.Icon, token: _thumbnailTokenSource.Token);
				icon.sprite = texture
					? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero)
					: null;
			}

			_thumbnailTokenSource = null;
		}

		public static (GameObject, ServerComponent) Generate(ServerPage worldPage, RectTransform parent) {
			var content        = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/split.prefab"), parent);
			var iconAsset      = Client.GetAsset<GameObject>("ui:prefabs/header_icon.prefab");
			var labelAsset     = Client.GetAsset<GameObject>("ui:prefabs/header_label.prefab");
			var withTitleAsset = Client.GetAsset<GameObject>("ui:prefabs/with_title.prefab");
			var listAsset      = Client.GetAsset<GameObject>("ui:prefabs/list.prefab");
			var scrollAsset    = Client.GetAsset<GameObject>("ui:prefabs/scroll.prefab");
			var boxAsset       = Client.GetAsset<GameObject>("ui:prefabs/box.prefab");

			var component = content.AddComponent<ServerComponent>();
			component.Page = worldPage;
			content.name   = $"[{worldPage.GetKey()}_{content.GetEntityId().GetHashCode()}]";

			var splitContent   = Reference.GetComponent<RectTransform>("content", content);
			var containerAsset = Client.GetAsset<GameObject>("ui:prefabs/container.prefab");

			// generate profile
			var container = Instantiate(containerAsset, splitContent);
			var profile = Instantiate(
				Client.GetAsset<GameObject>("prefabs/profile.prefab"),
				Reference.GetComponent<RectTransform>("content", container)
			);
			component.identifier = Reference.GetComponent<TextLanguage>("identifier", profile);
			component.title      = Reference.GetComponent<TextLanguage>("title", profile);
			component.icon       = Reference.GetComponent<Image>("icon", profile);

			// generate dashboard
			container = Instantiate(Client.GetAsset<GameObject>("ui:prefabs/container_full.prefab"), splitContent);
			var withTitle = Instantiate(
				withTitleAsset,
				Reference.GetComponent<RectTransform>("content", container)
			);

			var header = Reference.GetReference("header", withTitle);
			var icon   = Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			var label  = Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			component.labelIcon        = Reference.GetComponent<Image>("image", icon);
			component.label            = Reference.GetComponent<TextLanguage>("text", label);
			component.labelIcon.sprite = Client.GetAsset<Sprite>("ui:icons/globe.png");

			var contentDash = Reference.GetComponent<RectTransform>("content", withTitle);
			// setup scroll + list
			var scroll = Instantiate(scrollAsset, contentDash);
			var list   = Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", scroll));
			component.content = Reference.GetComponent<RectTransform>("content", list);

			// add box description
			component.descriptionContainer = Instantiate(boxAsset, component.content);
			Reference.GetComponent<TextLanguage>("text", component.descriptionContainer).UpdateText("server.about.description");
			component.descriptionText = Reference.GetComponent<TextLanguage>(
				"text", Instantiate(
					Client.GetAsset<GameObject>("ui:prefabs/text.prefab"),
					Reference.GetComponent<RectTransform>("content", component.descriptionContainer)
				)
			);

			return (content, component);
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private static void SetupEvents(EventTrigger eventTrigger, Action click, Action enter, Action exit) {
			if (!eventTrigger) return;
			var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
			entry.callback.AddListener(_ => click());
			eventTrigger.triggers.Add(entry);
			entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
			entry.callback.AddListener(_ => enter());
			eventTrigger.triggers.Add(entry);
			entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
			entry.callback.AddListener(_ => exit());
			eventTrigger.triggers.Add(entry);
		}
	}
}