using api.nox.ui.menus;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.UI;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using UnityEngine.UI;
using Transform = UnityEngine.Transform;

namespace api.nox.ui.layouts {
	public class Element : MonoBehaviour {
		[SerializeField]
		public NavigationData data;

		[SerializeField]
		public Menu menu;

		public TextLanguage text;
		public GameObject   textContainer;
		public Image        icon;
		public GameObject   iconContainer;
		public Button       button;

		public GameObject contentContainer;
		public GameObject customContainer;

		public NavigationData GetData()
			=> data;

		private void OnClick() {
			if (data.executionType == NavigationExecution.Event)
				PageManager.GetCoreAPI().EventAPI.Emit(data.execution, data.ExecutionArguments);
			else if (data.executionType == NavigationExecution.Goto)
				PageManager.SendGoto(menu.GetId(), data.execution, data.ExecutionArguments);
			else if (data.executionType == NavigationExecution.Action)
				PageManager.SendAction(menu.GetId(), data.execution);
		}

		private void Start()
			=> SetData(menu, data).Forget();

		public async UniTask SetData(Menu m, NavigationData d) {
			data = d;
			menu = m;

			if (d == null) {
				Logger.LogWarning($"Data is null for {gameObject.name}", gameObject);
				return;
			}

			if (!m) {
				Logger.LogWarning($"Menu is null for {gameObject.name}", gameObject);
				return;
			}


			// clear content of customContainer
			if (customContainer)
				foreach (Transform child in customContainer.transform)
					child.gameObject.Destroy();

			var custom = customContainer && d.GetCustomContent != null
				? await d.GetCustomContent.Invoke(customContainer.transform)
				: null;

			if (custom && customContainer) {
				custom.transform.SetParent(customContainer.transform, false);
				custom.transform.localScale    = Vector3.one;
				custom.transform.localPosition = Vector3.one;
				customContainer?.SetActive(true);
				contentContainer?.SetActive(false);
			} else {
				text?.UpdateText(d.text, d.textArguments);
				textContainer?.SetActive(!string.IsNullOrEmpty(d.text));
				if (icon) UpdateTexture(icon, d).Forget();
				iconContainer?.SetActive(icon?.sprite);
				if (customContainer) customContainer.SetActive(false);
				contentContainer?.SetActive(true);
			}

			if (button) {
				button.onClick.RemoveListener(OnClick);
				button.interactable = d.flags.HasFlag(NavigationFlags.Interactive);
				if (button.interactable)
					button.onClick.AddListener(OnClick);
			}
		}

		private async UniTaskVoid UpdateTexture(Image image, NavigationData d) {
			var texture = d.icon;
			if (texture) {
				image.sprite = Sprite.Create(
					texture,
					new Rect(0, 0, texture.width, texture.height),
					new Vector2(0.5f, 0.5f)
				);
				image.gameObject.SetActive(image.sprite);
				return;
			}

			if (!texture && d.iconPath.IsValid()) {
				image.sprite = await PageManager.GetAssetAsync<Sprite>(d.iconPath);
				image.gameObject.SetActive(image.sprite);
				if (image.sprite) return;
			}

			Logger.LogWarning($"Failed to load icon for navigation element: {d.text}", gameObject);
		}
	}
}