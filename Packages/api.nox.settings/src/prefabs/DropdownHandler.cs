using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.settings.prefabs {
	public abstract class DropdownHandler : IHandler {
		private (string, string)[] _options;
		private int                _defaultIndex = 0;

		public abstract string[] GetPath();

		private Dropdown _dropdown;

		public virtual GameObject GetContent(RectTransform transform) {
			var asset = Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");
			var go    = Object.Instantiate(asset, transform, false);
			_dropdown = Reference.GetComponent<Dropdown>("dropdown", go);
			UpdateOptions();
			UpdateValue();
			return go;
		}

		private void UpdateValue() {
			if (!_dropdown) return;
			if (_options.Length == 0) return;
			var index = Mathf.Clamp(_defaultIndex, 0, _options.Length - 1);
			_dropdown.SetValueWithoutNotify(index);
		}

		private void UpdateOptions() {
			if (!_dropdown) return;
			_dropdown.ClearOptions();
			var list = new System.Collections.Generic.List<Dropdown.OptionData>();
			foreach (var (label, value) in _options)
				list.Add(new Dropdown.OptionData(label));
			_dropdown.AddOptions(list);
			_dropdown.SetValueWithoutNotify(Mathf.Clamp(_defaultIndex, 0, _options.Length - 1));
			_dropdown.onValueChanged.RemoveListener(OnInternalValueChanged);
			_dropdown.onValueChanged.AddListener(OnInternalValueChanged);
		}

		private void OnInternalValueChanged(int index) {
			if (index < 0 || index >= _options.Length) return;
			OnValueChanged(_options[index].Item2);
		}


		public virtual UniTask<GameObject> GetContentAsync(RectTransform transform)
			=> UniTask.FromResult(GetContent(transform));

		public virtual void OnValueChanged(string value) { }

		public virtual void SetValue(string value, bool notify = true) {
			if (_options == null || _options.Length == 0) return;
			for (var i = 0; i < _options.Length; i++) {
				if (_options[i].Item2 != value) continue;
				_defaultIndex = i;
				UpdateValue();
				if (notify)
					OnInternalValueChanged(i);
				return;
			}
		}

		public virtual void SetOptions((string, string)[] options) {
			_options = options;
			UpdateOptions();
			UpdateValue();
		}
	}
}