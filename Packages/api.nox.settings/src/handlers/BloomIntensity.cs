using api.nox.settings.prefabs;
using Nox.CCK.Utils;

namespace api.nox.settings.handlers {
	public sealed class BloomIntensity : RangeHandler {
		public override string[] GetPath()
			=> new[] { "accessibility", "visual", "bloom_intensity" };

		public BloomIntensity() {
			SetRange(0f, 1f);
			SetStep(0.1f);
			SetValue(Value);
		}

		public float Value {
			get
				=> Config.Load()
					.Get(
						new[] {
							"settings", "accessibility", "bloom_intensity"
						}, 0f
					);
			set {
				var config = Config.Load();
				config.Set(
					new[] {
						"settings", "accessibility", "bloom_intensity"
					}, value
				);
				config.Save();
			}
		}

		public override void OnValueChanged(float value) {
			Value = value;
		}
	}
}