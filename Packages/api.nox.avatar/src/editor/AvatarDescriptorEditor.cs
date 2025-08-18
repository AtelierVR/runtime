#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.CCK.Avatars;
using UnityEngine;
using UnityEditor;

namespace api.nox.avatar.editor {
	[CustomEditor(typeof(AvatarDescriptor))]
	public class AvatarDescriptorEditor : UnityEditor.Editor {
		private AvatarDescriptor module
			=> (AvatarDescriptor)target;

		public override void OnInspectorGUI() {
			if (!Application.isPlaying)
				module.Modules = module.FindModules();

			if (module.Modules.Length == 0) {
				EditorGUILayout.HelpBox("No modules found. Please add at least one module to the avatar.", MessageType.Warning);
			} else {
				EditorGUILayout.LabelField("Modules", EditorStyles.boldLabel);
				foreach (var m in module.Modules)
					EditorGUILayout.ObjectField((Object)m, m.GetType(), true);
			}
		}
	}
}
#endif