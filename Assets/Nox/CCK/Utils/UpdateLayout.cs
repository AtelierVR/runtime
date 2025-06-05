using UnityEngine;
using UnityEngine.UI;

namespace Nox.CCK.Utils {
	public class UpdateLayout {
		// ReSharper disable Unity.PerformanceAnalysis
		public static void UpdateManually(GameObject go)
			=> UpdateManually(go.GetComponent<RectTransform>());

		public static void UpdateManually(RectTransform rect) {
			if (!rect || !rect.gameObject.activeInHierarchy) return;
			foreach (UnityEngine.Transform child in rect)
				UpdateManually(child.gameObject);
			LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
		}
	}
}