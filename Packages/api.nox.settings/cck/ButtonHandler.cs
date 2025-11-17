using System;
using System.Linq;
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
		private string[]     _keyLabel;
		private string[]     _keyButtonText;
		private bool         _interactable = true;


		private TextLanguage _buttonText;

		protected abstract GameObject GetPrefab();

		public virtual GameObject GetContent(RectTransform transform, IMenu menu) {
			var asset = GetPrefab();
			var go    = Object.Instantiate(asset, transform, false);
			_button     = Reference.GetComponent<Button>("button", go);
			_textLabel  = Reference.GetComponent<TextLanguage>("label", go);
			_buttonText = Reference.GetComponent<TextLanguage>("button_text", go);

			if (_button)
				_button.onClick.AddListener(() => OnClick(menu));

			if (_keyLabel != null)
				SetLabel(_keyLabel[0], _keyLabel.Skip(1).ToArray());
			else SetLabel(null);

			if (_keyButtonText != null)
				SetButtonText(_keyButtonText[0], _keyButtonText.Skip(1).ToArray());
			else SetButtonText(null);

			SetInteractable(_interactable);
			return go;
		}

		public void SetLabel(string key, params string[] @params) {
			key       ??= "label.default";
			@params   ??= Array.Empty<string>();
			_keyLabel =   new[] { key }.Concat(@params).ToArray();
			if (_textLabel)
				_textLabel.UpdateText(key);
		}

		public void SetButtonText(string key, params string[] @params) {
			key            ??= "button.default";
			@params        ??= Array.Empty<string>();
			_keyButtonText =   new[] { key }.Concat(@params).ToArray();
			if (_buttonText)
				_buttonText.UpdateText(key, @params);
		}

		public abstract void OnClick(IMenu menu);

		public virtual UniTask<GameObject> GetContentAsync(RectTransform transform, IMenu menu)
			=> UniTask.FromResult(GetContent(transform, menu));

		public virtual void SetInteractable(bool interactable) {
			_interactable = interactable;
			if (!_button) return;
			_button.interactable = interactable;
		}
	}
}