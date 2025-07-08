using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.ui.defaults {
	public class HomePage : IPage {
		public static string GetStaticKey()
			=> "home";

		private readonly int        _mId;
		private readonly object[]   _context;
		private          GameObject _content;

		private RectTransform _notificationContent;
		private RectTransform _dashboardContent;
		private RectTransform _friendsContent;

		public static IPage OnGotoAction(IMenu menu, object[] o)
			=> new HomePage(menu.GetId(), o);

		private HomePage(int mId, object[] context) {
			_mId     = mId;
			_context = context;
		}

		public string GetKey()
			=> GetStaticKey();

		public object[] GetContext()
			=> _context;

		public UniTask<GameObject> GetContentAsync(RectTransform parent)
			=> UniTask.FromResult(GetContent(parent));

		public IMenu GetMenu()
			=> Client.Instance.Get<IMenu>(_mId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			_content      = Object.Instantiate(PageManager.GetAsset<GameObject>("prefabs/split.prefab"), parent);
			_content.name = $"[{GetStaticKey()}_{_content.GetInstanceID()}]";
			var splitContent   = Reference.GetComponent<RectTransform>("content", _content);
			var containerAsset = PageManager.GetAsset<GameObject>("prefabs/container.prefab");
			var withTitleAsset = PageManager.GetAsset<GameObject>("prefabs/with_title.prefab");
			var iconAsset      = PageManager.GetAsset<GameObject>("prefabs/header_icon.prefab");
			var labelAsset     = PageManager.GetAsset<GameObject>("prefabs/header_label.prefab");
			var scrollAsset    = PageManager.GetAsset<GameObject>("prefabs/scroll.prefab");

			// generate background containers

			// generate notification
			var container = Object.Instantiate(containerAsset, splitContent);
			var withTitle = Object.Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));
			var header    = Reference.GetReference("header", withTitle);
			var icon      = Object.Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			var label     = Object.Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			Reference.GetComponent<Image>("image", icon).sprite = PageManager.GetAsset<Sprite>("icons/notifications.png");
			Reference.GetComponent<TextLanguage>("text", label).UpdateText("notifications.title");
			_notificationContent = Reference.GetComponent<RectTransform>("content", withTitle);

			// generate dashboard
			container = Object.Instantiate(PageManager.GetAsset<GameObject>("prefabs/container_full.prefab"), splitContent);
			withTitle = Object.Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));
			header    = Reference.GetReference("header", withTitle);
			icon      = Object.Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			label     = Object.Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			Reference.GetComponent<Image>("image", icon).sprite = PageManager.GetAsset<Sprite>("icons/dashboard.png");
			Reference.GetComponent<TextLanguage>("text", label).UpdateText("dashboard.title");

			container         = Object.Instantiate(scrollAsset, Reference.GetComponent<RectTransform>("content", withTitle));
			_dashboardContent = Reference.GetComponent<RectTransform>("content", container);


			// generate friends
			container = Object.Instantiate(containerAsset, splitContent);
			withTitle = Object.Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));
			header    = Reference.GetReference("header", withTitle);
			icon      = Object.Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			label     = Object.Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			Reference.GetComponent<Image>("image", icon).sprite = PageManager.GetAsset<Sprite>("icons/friend.png");
			Reference.GetComponent<TextLanguage>("text", label).UpdateText("friends.title");
			_friendsContent = Reference.GetComponent<RectTransform>("content", withTitle);

			return _content;
		}

		public void OnRemove() {
			if (_content) {
				Object.Destroy(_content);
				_content = null;
			}

			if (_notificationContent) {
				Object.Destroy(_notificationContent.gameObject);
				_notificationContent = null;
			}

			if (_dashboardContent) {
				Object.Destroy(_dashboardContent.gameObject);
				_dashboardContent = null;
			}

			if (_friendsContent) {
				Object.Destroy(_friendsContent.gameObject);
				_friendsContent = null;
			}
		}

		public override string ToString()
			=> $"{GetType().Name}[Key={GetKey()}, MenuId={_mId}, Context=[{string.Join(", ", _context)}]]";
	}
}