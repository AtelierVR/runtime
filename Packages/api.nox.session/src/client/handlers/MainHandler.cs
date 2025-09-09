using Nox.CCK.Language;
using UnityEngine;

namespace api.nox.session.client.handlers {
	public class MainHandler : MonoBehaviour {
		public static string GetId()
			=> "main";

		public static string GetDisplay()
			=> LanguageManager.Get("session.main.title");

		public static Texture2D GetIcon()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<Texture2D>("icons/dashboard.png", "ui");
	}
}