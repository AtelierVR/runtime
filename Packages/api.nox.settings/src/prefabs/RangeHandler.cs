using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.settings.prefabs {
	public abstract class RangeHandler : IHandler {
		public abstract string[] GetPath();

		private Slider _range;
		private float  _min;
		private float  _max;
		private float  _value;
		private float  _step;

		private float Min
			=> Mathf.Ceil(_min / Step) * Step;

		private float Max
			=> Mathf.Floor(_max / Step) * Step;

		private float Value
			=> Mathf.Round(Mathf.Clamp(_value, Min, Max) / Step) * Step;

		private float Step
			=> _step > 0 ? _step : float.Epsilon;

		public GameObject GetContent(RectTransform transform) {
			var asset = Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/range.prefab");
			var go    = Object.Instantiate(asset, transform, false);
			_range = Reference.GetComponent<Slider>("range", go);
			_range.onValueChanged.AddListener(OnInternalValueChanged);
			UpdateSlider();
			return go;
		}

		private void OnInternalValueChanged(float value) {
			_value = value;
			OnValueChanged(Value);
		}

		public abstract void OnValueChanged(float value);

		public UniTask<GameObject> GetContentAsync(RectTransform transform)
			=> UniTask.FromResult(GetContent(transform));

		private void UpdateSlider() {
			if (!_range) return;
			_range.minValue     = Min;
			_range.maxValue     = Max;
			_range.wholeNumbers = _step % 1 == 0;
			_range.SetValueWithoutNotify(Value);
		}

		public virtual void SetStep(float step) {
			_step = step;
			UpdateSlider();
		}

		public virtual void SetRange(float min, float max) {
			_min = min;
			_max = max;
			UpdateSlider();
		}

		public virtual void SetValue(float value, bool notify = true) {
			_value = value;
			UpdateSlider();
			if (notify)
				OnValueChanged(Value);
		}
	}
}