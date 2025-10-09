using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Settings;
using UnityEngine;

namespace api.nox.microphone.settings {
	public sealed class CurrentMicSetting : DropdownHandler {
		public override string[] GetPath()
			=> new[] { "audio", "microphone", "current" };

		protected override GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings", "prefabs/dropdown.prefab");

		public CurrentMicSetting() {
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			SetOptions(GetOptions());
			SetValue(Main.Instance.Manager.CurrentConfigName, false);
		}

		private static (string, string)[] GetOptions() {
			var devices = Main.Instance.Manager.Microphones;
			if (devices.Count == 0)
				return Array.Empty<(string, string)>();
			var defaultMic = Main.Instance.Manager.DefaultMicrophone;
			var list       = new (string, string)[devices.Count + 1];
			list[0] = (LanguageManager.Get("audio.microphone.default", new object[] { defaultMic.GetName() }), "default");
			for (var i = 0; i < devices.Count; i++)
				list[i + 1] = (devices[i].GetName(), devices[i].GetName());
			return list;
		}

		public override void OnValueChanged(string value)
			=> Main.Instance.Manager.CurrentConfigName = value;
	}
}