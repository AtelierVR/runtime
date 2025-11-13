#if UNITY_EDITOR
using Nox.CCK.YantraJS;
using UnityEditor;
using UnityEngine;

namespace api.nox.yantra {
	[CustomEditor(typeof(YantraFile))]
	public class YantraFileEditor : Editor {
		public override void OnInspectorGUI() {
			var icon = Resources.Load<Texture2D>("yantra-icon");
			if (icon) EditorGUIUtility.SetIconForObject(target, icon);
			var file = (YantraFile)target;
			var text = EditorGUILayout.TextArea(file.text, GUILayout.MinHeight(200));
			if (text != file.text) {
				Undo.RecordObject(file, "Edit Yantra File");
				file.text = text;
			}
		}
	}
}
#endif

