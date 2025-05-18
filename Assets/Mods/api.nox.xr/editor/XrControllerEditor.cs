using api.nox.xr;
using UnityEditor;

namespace Mods.api.nox.xr.editor {
	[CustomEditor(typeof(XRProxy))]
	public class XRProxyEditor : Editor {
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();
			var controller = (XRProxy)target;
			if (!controller) {
				EditorGUILayout.LabelField("Controller is null");
				return;
			}

			var abilities = controller.GetAbilities();
			if (abilities == null || abilities.Count == 0) {
				EditorGUILayout.LabelField("No abilities found");
			} else {
				EditorGUILayout.LabelField($"Abilities ({abilities.Count})");
				foreach (var ability in abilities)
					EditorGUILayout.TextField(
						$" - {ability.Key}",
						ability.Value.ToString()
					);
			}
		}
		
		public override bool RequiresConstantRepaint() {
			return true;
		}
	}
}