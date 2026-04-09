using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Panels;
using UnityEngine.UIElements;

namespace api.nox.user {
	public class ProfilePanel : IEditorPanelBuilder {
		public string GetId()
			=> "profile";

		public string GetName()
			=> "User/Profile";

		public bool IsHidden()
			=> Main.Instance?.Network?.CurrentUser == null;

		private readonly VisualElement _root = new();


		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();

			var child = EditorUser.CoreAPI.AssetAPI
				.GetAsset<VisualTreeAsset>("profile.uxml")
				.CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);
			_root.Q<Label>("version").text = "v" + EditorUser.CoreAPI.ModMetadata.GetVersion();

			var user = Main.Instance.Network.CurrentUser;

			_root.Q<UnsignedIntegerField>("id").value = user.Id;
			_root.Q<TextField>("server").value        = user.Server;
			_root.Q<TextField>("display").value       = user.Display;
			_root.Q<TextField>("username").value      = user.Username;
			_root.Q<TextField>("email").value         = user.Email;

			var banner    = user.Banner;
			var thumbnail = user.Thumbnail;

			var withoutVisual = _root.Q<VisualElement>("without-banner");
			var withVisual    = _root.Q<VisualElement>("with-banner");
			if (string.IsNullOrEmpty(banner)) // without-banner
			{
				withoutVisual.style.display = DisplayStyle.Flex;
				withVisual.style.display    = DisplayStyle.None;

				var displayName = withoutVisual.Q<Label>("display_name");
				displayName.text = user.Display;

				var thumbnailImage = withoutVisual.Q<Image>("thumbnail");

				if (!thumbnailImage.image)
					UpdateImage(thumbnailImage, thumbnail).Forget();
			} else // with-banner
			{
				withoutVisual.style.display = DisplayStyle.None;
				withVisual.style.display    = DisplayStyle.Flex;

				var displayName = withVisual.Q<Label>("display_name");
				displayName.text = user.Display;

				var bannerImage    = withVisual.Q<Image>("banner");
				var thumbnailImage = withVisual.Q<Image>("thumbnail");

				if (!bannerImage.image)
					UpdateImage(bannerImage, banner).Forget();
				if (!thumbnailImage.image)
					UpdateImage(thumbnailImage, thumbnail).Forget();
			}

			var logoutButton = _root.Q<Button>("logout-button");
			logoutButton.clicked += async () => {
				logoutButton.SetEnabled(false);
				var success = await Main.Instance.Network.Logout();
				if (success) {
					EditorUser.CoreAPI.PanelAPI.SetActivePanel(EditorUser.Auth.GetId());
					EditorUser.CoreAPI.PanelAPI.UpdatePanelList();
				} else logoutButton.SetEnabled(true);
			};

			return _root;
		}


		private async UniTask UpdateImage(Image image, string url) {
			if (string.IsNullOrEmpty(url)) {
				image.image = null;
				return;
			}

			image.image = await Main.NetworkAPI.FetchTexture(url);
		}
	}
}