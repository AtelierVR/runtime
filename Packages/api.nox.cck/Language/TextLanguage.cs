using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Language {
	public class TextLanguage : MonoBehaviour {
		public string   key;
		public string[] arguments;

		private string text
			=> LanguageManager.Get(key, arguments);

		private void Start() {
			LanguageManager.OnLanguageChanged.AddListener(OnUpdateText);
			UpdateText();
		}

		private void OnDestroy()
			=> LanguageManager.OnLanguageChanged.RemoveListener(OnUpdateText);


		private void OnValidate()
			=> UpdateText();

		private void OnUpdateText(string lang)
			=> UpdateText();

		public void UpdateText() {
			if (GetComponent<TMPro.TextMeshProUGUI>() is { } textMeshProUGUI)
				textMeshProUGUI.text = text;
			else if (GetComponent<UnityEngine.UI.Text>() is { } textUI)
				textUI.text = text;
		}

		public void UpdateText(string[] args) {
			arguments = args;
			UpdateText();
		}

		public void UpdateText(string k, string[] args = null) {
			key = k;
			if (args != null)
				arguments = args;
			UpdateText();
		}
	}
}