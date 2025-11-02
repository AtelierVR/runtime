using UnityEngine;
using UnityEngine.UI;

namespace Nox.CCK.Utils {
	public class UpdateLayout {
		public static void UpdateImmediate(GameObject go)
			=> UpdateImmediate(go.GetComponent<RectTransform>());

		public static void UpdateImmediate(RectTransform rect) {
			if (!rect || !rect.gameObject.activeInHierarchy) return;
			foreach (UnityEngine.Transform child in rect)
				UpdateImmediate(child.gameObject);
			foreach (var layout in rect.GetComponents<IUpdateLayout>())
				layout.UpdateLayout();
			LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
		}
	}

	/// <summary>
	/// Interface for components that need to update their layout.
	/// </summary>
	public interface IUpdateLayout {
		/// <summary>
		/// Updates the component's layout.
		/// </summary>
		void UpdateLayout();
	}
}