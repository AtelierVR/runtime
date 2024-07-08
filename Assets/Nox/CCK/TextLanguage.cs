using UnityEngine;

namespace Nox.CCK
{
    public class TextLanguage : MonoBehaviour
    {
        public string key;
        public string[] arguments;

        public string text
        {
            get
            {
                string text = LanguageManager.Get(key);
                if (arguments != null)
                    text = string.Format(text, arguments);
                return text;
            }
        }

        void OnValidate() => UpdateText();

        public void UpdateText()
        {
            if (GetComponent<TMPro.TextMeshProUGUI>() is TMPro.TextMeshProUGUI textMeshProUGUI)
                textMeshProUGUI.text = text;
            else if (GetComponent<UnityEngine.UI.Text>() is UnityEngine.UI.Text textUI)
                textUI.text = text;
        }

        public void UpdateText(string[] arguments)
        {
            this.arguments = arguments;
            UpdateText();
        }

        public void UpdateText(string key, string[] arguments = null)
        {
            this.key = key;
            if (arguments != null)
                this.arguments = arguments;
            UpdateText();
        }
    }
}