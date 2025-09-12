#if UNITY_EDITOR
using Nox.CCK.Worlds;
using UnityEngine;
using UnityEditor;

namespace api.nox.world.editor {
	[CustomEditor(typeof(WorldDescriptor))]
	public class WorldDescriptorEditor : UnityEditor.Editor {
		private WorldDescriptor module
			=> (WorldDescriptor)target;

		public override void OnInspectorGUI() {
			if (!Application.isPlaying)
				module.Modules = module.FindModules();

			if (module.Modules.Length == 0) {
				EditorGUILayout.HelpBox("No modules found. Please add at least one module to the world.", MessageType.Warning);
			} else {
				EditorGUILayout.LabelField("Modules", EditorStyles.boldLabel);
				foreach (var m in module.Modules)
					EditorGUILayout.ObjectField((Object)m, m.GetType(), true);
			}
		}
	}
}
#endif