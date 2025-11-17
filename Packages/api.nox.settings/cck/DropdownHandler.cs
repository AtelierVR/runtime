using System.Collections.Generic;
using System.Linq;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Settings {
	public abstract class DropdownHandler : ButtonHandler {
		private Dictionary<string, string[]> _options = new();

		protected abstract IModalBuilder GetModalBuilder(IMenu menu);

		public override GameObject GetContent(RectTransform transform, IMenu menu) {
			var go = base.GetContent(transform, menu);
			SetInteractable(menu is IModalMenu);
			return go;
		}

		public override void OnClick(IMenu menu) {
			var builder = GetModalBuilder(menu);
			if (builder == null) return;
			builder.SetTitle($"settings.entry.{string.Join(".", GetPath())}.label");
			builder.SetClosable(true);
			builder.SetOptions(e => SetValue(e), _options);
			builder.SetContent("empty");
			var modal = builder.Build();
			modal.OnClose.AddListener(() => modal.Dispose());
			modal.Show();
		}

		protected virtual void OnValueChanged(string value) { }

		protected virtual void SetValue(string value, bool notify = true) {
			if (_options.Count == 0) return;
			if (!_options.ContainsKey(value)) return;
			Logger.LogDebug($"Setting value to {value}");
			SetButtonText(_options[value][0], _options[value].Skip(1).ToArray());
			if (!notify) return;
			OnValueChanged(value);
		}

		protected virtual void SetOptions(Dictionary<string, string[]> options)
			=> _options = options;
	}
}