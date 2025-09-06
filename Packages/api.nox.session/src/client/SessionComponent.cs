using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.session.client {
	public class SessionComponent : MonoBehaviour {
		public Image         labelIcon;
		public TextLanguage  label;
		public RectTransform content;
		public TMP_Dropdown  dropdown;
		public GameObject    header;

		public SessionPage Page;

		public static (GameObject, SessionComponent) Generate(SessionPage sessionPage, RectTransform parent) {
			var iconAsset      = Client.GetAsset<GameObject>("prefabs/header_icon.prefab", "ui");
			var labelAsset     = Client.GetAsset<GameObject>("prefabs/header_label.prefab", "ui");
			var dropdownAsset  = Client.GetAsset<GameObject>("prefabs/header_dropdown.prefab", "ui");
			var withTitleAsset = Client.GetAsset<GameObject>("prefabs/with_title.prefab", "ui");
			var listAsset      = Client.GetAsset<GameObject>("prefabs/list.prefab", "ui");
			var scrollAsset    = Client.GetAsset<GameObject>("prefabs/scroll.prefab", "ui");

			var content = Instantiate(Client.GetAsset<GameObject>("prefabs/split.prefab", "ui"), parent);

			var component = content.AddComponent<SessionComponent>();
			component.Page = sessionPage;
			content.name   = $"[{sessionPage.GetKey()}_{content.GetInstanceID()}]";

			var splitContent   = Reference.GetComponent<RectTransform>("content", content);
			var containerAsset = Client.GetAsset<GameObject>("prefabs/container.prefab", "ui");

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
			UpdateLayout.UpdateImmediate(header);
			if (Main.Instance.GetSessionCount() == 0) return;
			var current = Page.GetSession();
			if (current == null) return;
			var index = Main.Instance.GetSessions()
				.ToList()
				.IndexOf(current);
			if (index < 0) return;
			dropdown.value = index;
			dropdown.RefreshShownValue();
		}

		private void OnChangeDropdown(int index) {
			var sessions = Main.Instance.GetSessions();
			UpdateLayout.UpdateImmediate(header);
			if (index < 0 || index >= sessions.Length) return;
			var session = sessions[index];
			if (session == null) return;
			Page.SetSession(session);
		}
	}
}