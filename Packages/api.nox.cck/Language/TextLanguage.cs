using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Language {
	public class TextLanguage : MonoBehaviour {
		public string   key;
		public string[] arguments;

		private string text
			=> LanguageManager.Get(key, arguments);

		private void Start() {
			LanguageManager.OnLanguageChanged += UpdateText;
			UpdateText();
		}

		private void OnDestroy()
			=> LanguageManager.OnLanguageChanged -= UpdateText;


		private void OnValidate()
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