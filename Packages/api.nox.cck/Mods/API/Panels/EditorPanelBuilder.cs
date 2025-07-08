using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Nox.CCK.Mods.Panels {
	public interface IEditorPanelBuilder {
		public string GetId();
		public string GetName();

		public string GetTitle()
			=> null;

		public VisualElement[] GetHeaders()
			=> Array.Empty<VisualElement>();

		public bool IsHidden();

		public VisualElement Make(Dictionary<string, object> data);

		public void OnUpdate() { }

		public void OnHidden() { }

		public void OnVisible() { }
	}
}