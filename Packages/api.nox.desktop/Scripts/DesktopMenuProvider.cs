using System;
using Cysharp.Threading.Tasks;
using Nox.UI;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
namespace api.nox.desktop {
	public class DesktopMenuProvider : MonoBehaviour, IMenuProvider, IDisposable {
		public RectTransform Container;
		public IMenu Menu;
		public DesktopPlayer Player;

		RectTransform IMenuProvider.Container
			=> Container;

		public GameObject Parent;

		public bool Active {
			get => Parent.activeSelf;
			set => Parent.SetActive(value);
		}

		public async UniTask<bool> Generate() {
			Menu = await Client.UiAPI.Make(this);

			if (Menu == null) {
				Logger.LogError("Failed to create XR proxy menu");
				return false;
			}

			Menu.SetActive(false);

			Keybindings.KeyEvent.AddListener(OnKey);

			return true;
		}

		private void OnKey(string key, float @new, float old) {
			switch (key) {
				case "main" when @new > 0 && old == 0:
					ToggleMenu();
					break;
			}
		}

		public void Dispose() {
			Keybindings.KeyEvent.RemoveListener(OnKey);
			Menu?.Dispose();
			Menu = null;
		}

		private void ToggleMenu() {
			if (Menu == null)
				return;

			var isMenuVisible = Menu.GetActive();

			if (!isMenuVisible) {
				// Menu is being opened
				Cursor.lockState   = CursorLockMode.None;
				Cursor.visible     = true;
				Player.useMovement = false; // Block movement inputs
			} else {
				// Menu is being closed
				Cursor.lockState   = CursorLockMode.Locked;
				Cursor.visible     = false;
				Player.useMovement = true; // Re-enable movement inputs
			}

			Menu.SetActive(!isMenuVisible);
		}
	}
}