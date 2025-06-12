using System.Collections.Generic;
using api.nox.user.network;
using Nox.CCK.Mods.Panels;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.user {
	public class AuthPanel : EditorPanelBuilder {
		public string GetId()
			=> "auth";

		public string GetName()
			=> "User/Auth";

		public bool IsHidden()
			=> Main.Instance.Network.CurrentUser != null;

		private readonly VisualElement _root = new();


		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();

			var child = EditorUser.CoreAPI.AssetAPI
				.GetAsset<VisualTreeAsset>("auth.uxml")
				.CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);
			_root.Q<Label>("version").text = "v" + EditorUser.CoreAPI.ModMetadata.GetVersion();

			var button     = _root.Q<Button>("login-button");
			var password   = _root.Q<TextField>("password");
			var identifier = _root.Q<TextField>("identifier");
			var server     = _root.Q<TextField>("server");
			// Add event listener for Enter key press
			server.RegisterCallback<KeyDownEvent>(
				evt => {
					if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;
					identifier.Focus();
					evt.StopPropagation();
				}
			);

			identifier.RegisterCallback<KeyDownEvent>(
				evt => {
					if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;
					password.Focus();
					evt.StopPropagation();
				}
			);

			password.RegisterCallback<KeyDownEvent>(
				evt => {
					if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;
					button.Focus();
					evt.StopPropagation();
				}
			);

			button.clickable.clicked += async () => {
				button.SetEnabled(false);
				password.isReadOnly   = true;
				identifier.isReadOnly = true;
				server.isReadOnly     = true;
				Logger.LogDebug(
					new LoginRequest {
						Identifier = identifier.value,
						Password   = password.value,
					}
				);
				var o = await Main.Instance.Network.Login(
					new LoginRequest {
						Identifier = identifier.value,
						Password   = password.value,
					}, server.value
				);
				button.SetEnabled(true);
				password.isReadOnly   = false;
				identifier.isReadOnly = false;
				server.isReadOnly     = false;
				if (Main.Instance.Network.CurrentUser != null) {
					EditorUser.CoreAPI.PanelAPI.SetActivePanel("profile");
					EditorUser.CoreAPI.PanelAPI.UpdatePanelList();
				} else Logger.Log(o);
			};


			return _root;
		}
	}
}