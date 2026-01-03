/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;
using Transform = UnityEngine.Transform;

namespace api.nox.session.client {
	public class SessionComponent : MonoBehaviour {
		public  Image                   labelIcon;
		public  TextLanguage            label;
		public  RectTransform           content;
		public  TMP_Dropdown            dropdown;
		public  GameObject              header;
		public  TextLanguage            title;
		public  TextLanguage            shortName;
		public  Image                   thumbnail;
		public  GameObject              withThumbnail;
		public  GameObject              withoutThumbnail;
		private CancellationTokenSource _thumbnailTokenSource;
		public  RectTransform           navigation;
		public  string                  currentHandler;
		public  GameObject              leftContainer;

		public SessionPage Page;

		public static (GameObject, SessionComponent) Generate(SessionPage sessionPage, RectTransform parent) {
			var iconAsset      = Client.GetAsset<GameObject>("prefabs/header_icon.prefab", "ui");
			var labelAsset     = Client.GetAsset<GameObject>("prefabs/header_label.prefab", "ui");
			var dropdownAsset  = Client.GetAsset<GameObject>("prefabs/header_dropdown.prefab", "ui");
			var withTitleAsset = Client.GetAsset<GameObject>("prefabs/with_title.prefab", "ui");
			var listAsset      = Client.GetAsset<GameObject>("prefabs/list.prefab", "ui");
			var scrollAsset    = Client.GetAsset<GameObject>("prefabs/scroll.prefab", "ui");
			var containerAsset = Client.GetAsset<GameObject>("prefabs/container.prefab", "ui");

			var content = Instantiate(Client.GetAsset<GameObject>("prefabs/split.prefab", "ui"), parent);

			var component = content.AddComponent<SessionComponent>();
			component.Page = sessionPage;
			content.name   = $"[{sessionPage.GetKey()}_{content.GetInstanceID()}]";

			var splitContent = Reference.GetComponent<RectTransform>("content", content);

			// left container

			component.leftContainer = Instantiate(containerAsset, splitContent);
			var profile = Instantiate(
				Client.GetAsset<GameObject>("prefabs/profile.prefab", "ui"),
				Reference.GetComponent<RectTransform>("content", component.leftContainer)
			);
			component.title            = Reference.GetComponent<TextLanguage>("title", profile);
			component.shortName        = Reference.GetComponent<TextLanguage>("identifier", profile);
			component.thumbnail        = Reference.GetComponent<Image>("thumbnail", profile);
			component.withThumbnail    = Reference.GetReference("with_thumbnail", profile);
			component.withoutThumbnail = Reference.GetReference("without_thumbnail", profile);
			var navigation = Instantiate(scrollAsset, Reference.GetComponent<RectTransform>("content", profile));

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
			var icon     = Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", component.header));
			var label    = Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", component.header));
			var dropdown = Instantiate(dropdownAsset, Reference.GetComponent<RectTransform>("after", component.header));

			component.labelIcon        = Reference.GetComponent<Image>("image", icon);
			component.label            = Reference.GetComponent<TextLanguage>("text", label);
			component.labelIcon.sprite = Client.GetAsset<Sprite>("icons/globe.png", "ui");
			component.dropdown         = Reference.GetComponent<TMP_Dropdown>("dropdown", dropdown);
			component.dropdown.onValueChanged.AddListener(component.OnChangeDropdown);

			var contentDash = Reference.GetComponent<RectTransform>("content", withTitle);
			// setup scroll + list
			var scroll = Instantiate(scrollAsset, contentDash);
			var list   = Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", scroll));
			component.content = Reference.GetComponent<RectTransform>("content", list);

			return (content, component);
		}

		public void UpdateDropdown() {
			dropdown.ClearOptions();
			var options = Main.Instance.GetSessions()
				.Select(
					session => new TMP_Dropdown.OptionData {
						text = session.GetAdapter().GetName()
							?? $"{session.GetAdapter().GetType().Name} ({session.GetId()})"
					}
				)
				.ToList();
			dropdown.AddOptions(options);
			if (Main.Instance.GetSessionCount() == 0) return;
			var current = Page.GetSession();
			if (current == null) return;
			var index = Main.Instance.GetSessions()
				.ToList()
				.IndexOf(current);
			if (index < 0) return;
			dropdown.SetValueWithoutNotify(index);
			dropdown.RefreshShownValue();

			UpdateLayout.UpdateImmediate(header);
		}

		private void OnChangeDropdown(int index) {
			var sessions = Main.Instance.GetSessions();
			if (index < 0 || index >= sessions.Length) return;
			var session = sessions[index];
			if (session == null) return;
			Page.SetSession(session);
		}

		public void UpdateTitles() {
			var session = Page.GetSession();
			if (session == null) {
				title.UpdateText("session.page.no_session.title");
				label.UpdateText("session.page.no_session.label");
				return;
			}

			title.UpdateText(
				"session.page.session.name", new[] {
					session.GetAdapter().GetName() ?? $"{session.GetAdapter().GetType().Name} ({session.GetId()})"
				}
			);

			label.UpdateText(
				"session.page.session.label", new[] {
					session.GetAdapter().GetName() ?? $"{session.GetAdapter().GetType().Name} ({session.GetId()})"
				}
			);

			shortName.UpdateText(
				"session.page.session.short_name", new[] {
					session.GetAdapter().GetShortName()
				}
			);

			UpdateLayout.UpdateImmediate(leftContainer);
		}

		public async UniTask UpdateThumbnail() {
			if (_thumbnailTokenSource != null) {
				_thumbnailTokenSource?.Cancel();
				_thumbnailTokenSource?.Dispose();
			}

			var session = Page.GetSession();

			_thumbnailTokenSource = new CancellationTokenSource();

			var texture = await session.GetAdapter()
				.GetThumbnail()
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

			_thumbnailTokenSource = null;
			UpdateLayout.UpdateImmediate(leftContainer);
		}

		public async UniTask UpdateNavigation() {
			var btn = await Client.GetAssetAsync<GameObject>("prefabs/btn_icon.prefab", "ui");

			var handlers = new List<(string, string, Texture2D)>();
			Main.Instance.CoreAPI.EventAPI.Emit(
				"session_handlers_request",
				Page.GetSession(),
				new Action<object[]>(
					data => {
						var id      = data.Length > 0 && data[0] is string s ? s : null;
						var display = data.Length > 1 && data[1] is string s2 ? s2 : id;
						var icon    = data.Length > 2 && data[2] is Texture2D t ? t : null;
						handlers.Add((id, display, icon));
					}
				)
			);

			foreach (Transform tf in navigation)
				Destroy(tf.gameObject);

			foreach (var handler in handlers) {
				if (string.IsNullOrEmpty(handler.Item1)) continue;
				var o = Instantiate(btn, navigation);

				var image          = Reference.GetComponent<Image>("image", o);
				var imageContainer = Reference.GetComponent<RectTransform>("image_container", o);
				if (handler.Item3) {
					image.sprite = Sprite.Create(
						handler.Item3,
						new Rect(0, 0, handler.Item3.width, handler.Item3.height),
						new Vector2(0.5f, 0.5f)
					);
					imageContainer.gameObject.SetActive(true);
				} else imageContainer.gameObject.SetActive(false);

				var text = Reference.GetComponent<TextLanguage>("text", o);
				text.UpdateText("session.navigation.handler", new[] { handler.Item2 });

				var button = o.GetComponent<Button>();
				button.onClick.AddListener(() => OnChangeHandler(handler.Item1));

				o.name = $"{handler.Item1}_{o.GetInstanceID()}";
				o.SetActive(true);
			}

			if (handlers.All(h => h.Item1 != currentHandler))
				OnChangeHandler(handlers.FirstOrDefault().Item1);

			UpdateLayout.UpdateImmediate(leftContainer);
		}

		private void OnChangeHandler(string handler) {
			if (string.IsNullOrEmpty(handler)) return;
		}
	}
}*/