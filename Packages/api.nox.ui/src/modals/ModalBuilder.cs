using System;
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
					? "prefabs/closable_message_modal.prefab"
					: "prefabs/message_modal.prefab"
			);
			modal.content = Object.Instantiate(asset, container);
			var close = Reference.GetComponent<Button>("close", modal.content);
			close?.onClick.AddListener(modal.OnCloseClicked);
			var title = Reference.GetComponent<TextLanguage>("title", modal.content);
			title?.UpdateText(Title[0], Title.Skip(1).ToArray());
			var text = Reference.GetComponent<TextLanguage>("message", modal.content);
			text?.UpdateText(Content[0], Content.Skip(1).ToArray());

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

		public void SetContent(Func<RectTransform, GameObject> generator)
			=> Generator = generator;
	}
}