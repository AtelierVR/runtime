using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace api.nox.user.client {
	public class UserPage : IPage {
		internal static string GetStaticKey()
			=> "user";

		public string GetKey()
			=> GetStaticKey();

		private int            _mId;
		private object[]       _context;
		private GameObject     _content;
		private UserIdentifier _identifier;
		private User           _user;


		public void OnRefresh()
			=> Refresh().Forget();

		private static bool T<T>(object[] o, int index, out T value) {
			if (o.Length > index && o[index] is T t) {
				value = t;
				return true;
			}

			value = default;
			return false;
		}

		internal static IPage OnGotoAction(IMenu menu, object[] context) {
			if (!T(context, 0, out string type)) return null;
			switch (type) {
				case "id_server" when T(context, 1, out uint id0) && T(context, 2, out string ser0):
					return OnPageByIdentifier(menu, context, new UserIdentifier(id0, ser0));
				case "identifier" when T(context, 1, out string id2):
					return OnPageByIdentifier(menu, context, UserIdentifier.FromString(id2));
				case "user" when T(context, 1, out User usr3):
					return OnPageByUser(menu, context, usr3);
			}

			return null;
		}

		private static UserPage OnPageByIdentifier(IMenu menu, object[] context, UserIdentifier identifier) {
			var page = new UserPage {
				_mId        = menu.GetId(),
				_context    = context,
				_identifier = identifier,
				_user       = null
			};
			page.Refresh().Forget();
			return page;
		}

		private static UserPage OnPageByUser(IMenu menu, object[] context, User user) {
			return new UserPage {
				_mId        = menu.GetId(),
				_context    = context,
				_identifier = user.ToInternalIdentifier(),
				_user       = user
			};
		}

		private async UniTask Refresh() { }

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(_mId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			_content      = Object.Instantiate(Client.GetAsset<GameObject>("prefabs/split.prefab", "ui"), parent);
			_content.name = $"[{GetStaticKey()}_{_content.GetInstanceID()}]";
			var splitContent   = Reference.GetComponent<RectTransform>("content", _content);
			var containerAsset = Client.GetAsset<GameObject>("prefabs/container.prefab", "ui");
			var iconAsset      = Client.GetAsset<GameObject>("prefabs/header_icon.prefab", "ui");
			var labelAsset     = Client.GetAsset<GameObject>("prefabs/header_label.prefab", "ui");
			var scrollAsset    = Client.GetAsset<GameObject>("prefabs/scroll.prefab", "ui");
			var infoAsset      = Client.GetAsset<GameObject>("prefabs/infobox.prefab", "ui");
			var listAsset      = Client.GetAsset<GameObject>("prefabs/list.prefab", "ui");

			// generate background containers

			// generate notification
			var container = Object.Instantiate(containerAsset, splitContent);
			var withTitle = Object.Instantiate(
				Client.GetAsset<GameObject>("prefabs/with_title.prefab", "ui"),
				Reference.GetComponent<RectTransform>("content", container)
			);
			var header = Reference.GetReference("header", withTitle);
			var icon   = Object.Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			var label  = Object.Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			Reference.GetComponent<Image>("image", icon).sprite = Client.GetAsset<Sprite>("icons/search.png", "ui");
			Reference.GetComponent<TextLanguage>("text", label).UpdateText("search.title");

			var handlers = Object.Instantiate(
				scrollAsset,
				Reference.GetComponent<RectTransform>("content", withTitle)
			);
			var listsHandler = Reference.GetComponent<RectTransform>(
				"content",
				Object.Instantiate(
					listAsset,
					Reference.GetComponent<RectTransform>(
						"content", handlers
					)
				)
			);
			var box = Object.Instantiate(Client.GetAsset<GameObject>("prefabs/box.prefab", "ui"), listsHandler);
			Reference.GetComponent<TextLanguage>("title", box).UpdateText("search.info.title");
			var handleInfo = Object.Instantiate(
				infoAsset,
				Reference.GetComponent<RectTransform>("content", withTitle)
			);


			// generate dashboard
			container = Object.Instantiate(Client.GetAsset<GameObject>("prefabs/container_full.prefab", "ui"), splitContent);
			withTitle = Object.Instantiate(
				Client.GetAsset<GameObject>("prefabs/with_search.prefab", "ui"),
				Reference.GetComponent<RectTransform>("content", container)
			);
			header = Reference.GetReference("header", withTitle);
			var content = Reference.GetComponent<RectTransform>("content", withTitle);
			return _content;
		}
	}
}