#if UNITY_EDITOR
using Nox.CCK.Jint;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;

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
					if (evt.newValue is not JintFile newScript) return;
					script.asset = newScript;
					Repaint();
				}
			);
			return root;
		}

		public override bool UseDefaultMargins()
			=> false;

		[MenuItem("Assets/Create/Nox/Jint Script", false, 1)]
		public static void CreateJintScript() {
			var path = EditorUtility.SaveFilePanelInProject(
				"Create Jint Script",
				"NewJintScript.js", "js",
				"Please enter a file name to save the Jint script to."
			);
			if (string.IsNullOrEmpty(path)) return;

			var resource = Resources.Load<JintFile>("jint_example");
			// copy content from the example file
			if (!resource) {
				Nox.CCK.Utils.Logger.LogError("Could not find example Jint file in Resources.");
				return;
			}

			var content = resource.text;
			System.IO.File.WriteAllText(path, content);
			AssetDatabase.ImportAsset(path);
		}
	}
}
#endif