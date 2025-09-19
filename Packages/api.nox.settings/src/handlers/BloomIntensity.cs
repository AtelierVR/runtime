using api.nox.settings.prefabs;
using Nox.CCK.Utils;

namespace api.nox.settings.handlers {
	public sealed class Brightness : RangeHandler {
		public override string[] GetPath()
			=> new[] { " accessibility", "visual", "brightness" };

		public Brightness() {
			SetRange(0f, 2f);
			SetStep(0.1f);
			SetValue(Value);
		}

		public float Value {
			get
				=> Config.Load()
					.Get(
						new[] {
							"settings", "accessibility", "brightness"
						}, 0.5f
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