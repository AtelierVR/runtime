using api.nox.ui.menus;
using Nox.CCK.Language;
using Nox.UI;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using UnityEngine.UI;

namespace api.nox.ui.layouts {
	public class Element : MonoBehaviour {
		[SerializeField] public NavigationData data;
		[SerializeField] public Menu           menu;

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
				PageManager.GetCoreAPI().EventAPI.Emit(data.execution, data.executionArguments);
			else if (data.executionType == NavigationExecution.Goto)
				PageManager.SendGoto(menu.GetId(), data.execution, data.executionArguments);
			else if (data.executionType == NavigationExecution.Action)
				PageManager.SendAction(menu.GetId(), data.execution);
		}

		private void Start()
			=> SetData(menu, data);

		public void SetData(Menu menu, NavigationData data) {
			this.data = data;
			this.menu = menu;
			if (data == null) {
				Logger.LogWarning($"Element.SetData: data is null for {gameObject.name}", gameObject);
				return;
			}

			if (!menu) {
				Logger.LogWarning($"Element.SetData: menu is null for {gameObject.name}", gameObject);
				return;
			}


			// clear content of customContainer
			if (customContainer)
				foreach (Transform child in customContainer.transform) {
					#if UNITY_EDITOR
					if (Application.isPlaying) Destroy(child.gameObject);
					else UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(child.gameObject);
					#else
						Destroy(child.gameObject);
					#endif
				}

			var custom = customContainer
				? data.getCustomContent?.Invoke(customContainer.transform)
				: null;

			if (custom && customContainer) {
				custom.transform.SetParent(customContainer.transform, false);
				custom.transform.localScale    = Vector3.one;
				custom.transform.localPosition = Vector3.one;
				customContainer?.SetActive(true);
				contentContainer?.SetActive(false);
			} else {
				text?.UpdateText(data.text, data.textArguments);
				textContainer?.SetActive(!string.IsNullOrEmpty(data.text));

				if (icon) {
					var texture = data.icon;
					if (!texture && !string.IsNullOrEmpty(data.iconPath)) {
						var splitPath = data.iconPath.Split(':');
						texture = splitPath.Length == 2
							? PageManager.GetAsset<Texture2D>(splitPath[1], splitPath[0])
							: PageManager.GetAsset<Texture2D>(data.iconPath);
					}

					icon.sprite = texture
						? Sprite.Create(
							texture,
							new Rect(0, 0, texture.width, texture.height),
							Vector2.zero
						)
						: null;
				}

				iconContainer?.SetActive(icon?.sprite);
				if (customContainer) customContainer.SetActive(false);
				contentContainer?.SetActive(true);
			}

			if (button) {
				button.onClick.RemoveListener(OnClick);
				button.interactable = data.flags.HasFlag(NavigationFlags.Interactive);
				if (button.interactable)
					button.onClick.AddListener(OnClick);
			}
		}
	}
}