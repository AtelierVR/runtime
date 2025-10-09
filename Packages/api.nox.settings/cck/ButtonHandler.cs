using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Settings;
using Nox.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Nox.CCK.Settings {
	public abstract class ButtonHandler : IHandler {
		public abstract string[] GetPath();

		public virtual bool IsActive()
			=> true;

		public virtual void OnUpdated(IHandler handler) { }

		public virtual int CompareTo(IHandler other)
			=> 0;

		private Button       _button;
		private TextLanguage _textLabel;
		private string       _keyLabel;
		private string       _keyButtonText;
		private bool         _interactable = true;


		private TextLanguage _buttonText;

		public abstract GameObject GetPrefab();

		public virtual GameObject GetContent(RectTransform transform, IMenu menu) {
			var asset = GetPrefab();
			var go    = Object.Instantiate(asset, transform, false);
			_button     = Reference.GetComponent<Button>("button", go);
			_textLabel  = Reference.GetComponent<TextLanguage>("label", go);
			_buttonText = Reference.GetComponent<TextLanguage>("button_text", go);

			if (_button)
				_button.onClick.AddListener(() => OnClick(menu));

			SetLabelKey(_keyLabel);
			SetButtonTextKey(_keyButtonText);
			SetInteractable(_interactable);
			return go;
		}

		public void SetLabelKey(string key) {
			_keyLabel = key;
			if (_textLabel)
				_textLabel.UpdateText(key);
		}

		public void SetButtonTextKey(string key) {
			_keyButtonText = key;
			if (_buttonText)
				_buttonText.UpdateText(key);
		}

		public abstract void OnClick(IMenu menu);

		public UniTask<GameObject> GetContentAsync(RectTransform transform, IMenu menu)
			=> UniTask.FromResult(GetContent(transform, menu));

		public virtual void SetInteractable(bool interactable) {
			_interactable = interactable;
			if (_button)
				_button.interactable = interactable;
		}
	}
}