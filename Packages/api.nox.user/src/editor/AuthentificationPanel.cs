using System.Collections.Generic;
using api.nox.user.network;
using Nox.CCK.Mods.Panels;
using UnityEngine.UIElements;

namespace api.nox.user {
	public class AuthentificationPanel : IEditorPanelBuilder {
		public string GetId()
			=> "auth";

		public string GetName()
			=> "User/Authentification";

		public string GetTitle()
			=> "Authentification";

		public bool IsHidden()
			=> Main.Instance.Network.CurrentUser != null;

		private readonly VisualElement     _root = new();
		public           AddressInput      Address;
		public           LoginInput        Login;
		public           VerificationInput Verification;

		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();

			var child = EditorUser.CoreAPI.AssetAPI
				.GetAsset<VisualTreeAsset>("auth.uxml")
				.CloneTree();
			_root.Add(child);

			Address      = new AddressInput(_root, this);
			Login        = new LoginInput(_root, this);
			Verification = new VerificationInput(_root, this);


			Address.SetActive(true);
			Login.SetActive(false);
			Verification.SetActive(false);

			return _root;
		}
	}
}