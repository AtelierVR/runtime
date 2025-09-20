using api.nox.settings.prefabs;
using Nox.CCK.Utils;

namespace api.nox.settings.handlers {
	public sealed class Brightness : RangeHandler {
		public override string[] GetPath()
			=> new[] { "accessibility", "visual", "brightness" };

		public Brightness() {
			SetRange(0.2f, 1f);
			SetStep(0.001f);
			SetValue(Value);
			SetLabelKey($"settings.entry.{string.Join(".", GetPath())}.label");
			SetValueKey("settings.range.value.percent");
		}

		public float Value {
			get
				=> Config.Load()
					.Get(
						new[] {
							"settings", "accessibility", "brightness"
						}, 1f
					);
			set {
				var config = Config.Load();
				config.Set(
					new[] {
						"settings", "accessibility", "brightness"
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