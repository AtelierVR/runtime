#if UNITY_EDITOR
using Nox.CCK.Worlds.FellInVoid;
using UnityEditor;
using UnityEngine;

namespace Nox.Editor.Worlds.FellInVoidWorld {
	[CustomEditor(typeof(FellInVoidWorldModule))]
	public class FellInVoidWorldModuleEditor : UnityEditor.Editor {
		public FellInVoidWorldModule Module
			=> (FellInVoidWorldModule)target;

		public override void OnInspectorGUI() {
			DrawDefaultInspector();

			if (Module.Session == null) return;
			var local = Module.Session.GetAdapter().GetLocalPlayer();
			var pos   = local.GetPosition();
			GUILayout.Label($"Local Player Position: {pos}");
			
			Repaint();
		}
	}
}
#endif