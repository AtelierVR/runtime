using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace api.nox.ui.modals {
	public class ModalBuilder : IModalBuilder {
		public Func<RectTransform, GameObject> Generator;
		public string[]                        Title    = { "modal.title" };
		public string[]                        Content  = { "modal.content" };
		public bool                            Closable = true;
		public Dictionary<string, string[]>    Options  = new();
		public Action<string>                  OnValueChanged;
		public IModalMenu                      Menu;

		public ModalBuilder(IModalMenu menu)
			=> Menu = menu;

		public IModal Build() {
			var asset    = Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/modal.prefab");
			var instance = Object.Instantiate(asset, Menu.GetModalContainer());
			var modal    = instance.GetOrAddComponent<BaseModal>();
			modal.Attach(Menu);
			instance.name = $"[Modal] {modal.GetInstanceID()}";
			var container = Reference.GetComponent<RectTransform>("content", instance);

			if (Generator != null)
				modal.content = Generator(container);

			if (modal.content)
				return modal;

			asset = Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>(
				Closable
					? Options.Count > 0
						? "prefabs/closable_options_message_modal.prefab"
						: "prefabs/closable_message_modal.prefab"
					: Options.Count > 0
						? "prefabs/options_message_modal.prefab"
						: "prefabs/message_modal.prefab"
			);

			modal.content = Object.Instantiate(asset, container);
			var close = Reference.GetComponent<Button>("close", modal.content);
			close?.onClick.AddListener(modal.OnCloseClicked);
			var title = Reference.GetComponent<TextLanguage>("title", modal.content);
			title?.UpdateText(Title[0], Title.Skip(1).ToArray());
			var text = Reference.GetComponent<TextLanguage>("message", modal.content);
			text?.UpdateText(Content[0], Content.Skip(1).ToArray());

			if (Options.Count > 0) {
				var optionsContainer   = Reference.GetComponent<RectTransform>("options", modal.content);
				var optionButtonPrefab = Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/btn_icon.prefab");
				foreach (var option in Options) {
					var optionInstance = Object.Instantiate(optionButtonPrefab, optionsContainer);
					Reference.GetReference("image_container", optionInstance)
						?.SetActive(false);
					Reference.GetComponent<TextLanguage>("text", optionInstance)
						?.UpdateText(option.Value[0], option.Value.Skip(1).ToArray());
					Reference.GetComponent<Button>("button", optionInstance)
						?.onClick.AddListener(
							() => {
								OnValueChanged?.Invoke(option.Key);
								if (Closable)
									modal.Close();
							}
						);
				}
			}

			return modal;
		}

		public void SetTitle(string text, params string[] args)
			=> Title = new[] { text ?? "empty" }.Concat(args).ToArray();

		public void SetClosable(bool closable)
			=> Closable = closable;
		
		public bool IsClosable()
			=> Closable;

		public void SetContent(string text, params string[] args)
			=> Content = new[] { text ?? "empty" }.Concat(args).ToArray();

		public void SetOptions(Action<string> onValue, Dictionary<string, string[]> options) {
			Options = options ?? new Dictionary<string, string[]>();
			foreach (var key in Options.Keys.ToArray()) {
				if (Options[key] != null && Options[key].Length > 0) continue;
				Options[key] = new[] { "modal.option." + key };
			}

			OnValueChanged = onValue;
		}

		public void SetContent(Func<RectTransform, GameObject> generator)
			=> Generator = generator;
	}
}