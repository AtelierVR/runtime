#if UNITY_EDITOR
using Nox.CCK.Jint;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.jint {
	[CustomEditor(typeof(JintScript))]
	public class JintScriptEditor : Editor {
		public override VisualElement CreateInspectorGUI() {
			var iconAsset = Resources.Load<Texture2D>("api.nox.jint.jintscript");
			if (iconAsset) EditorGUIUtility.SetIconForObject(target, iconAsset);
			var inspectorAsset = Resources.Load<VisualTreeAsset>("api.nox.jint.jintscript");
			if (!inspectorAsset) return new VisualElement();
			var root   = inspectorAsset.CloneTree();
			var script = (JintScript)target;
			if (!script) return root;
			var sc = root.Q<ObjectField>("script");
			sc.value = script.asset;
			sc.RegisterValueChangedCallback(
				evt => {
					if (evt.newValue is not JintScript newScript) return;
					script = newScript;
					Repaint();
				}
			);
			return root;
		}

		public override bool UseDefaultMargins()
			=> false;
	}
}
#endif