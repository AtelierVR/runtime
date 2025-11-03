#if UNITY_EDITOR
using Nox.CCK.Worlds.FellInVoid;
using UnityEditor;
using UnityEngine;

namespace Nox.Editor.Worlds.FellInVoidWorld {
	[CustomEditor(typeof(FellInVoidWorldModule))]
	public class FellInVoidWorldModuleEditor : UnityEditor.Editor {
		private FellInVoidWorldModule Module
			=> (FellInVoidWorldModule)target;

		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			var pos = Module.LocalPlayer?.GetPosition();
			GUILayout.Label($"Local Player Position: {pos}");
			Repaint();
		}
	}
}
#endif