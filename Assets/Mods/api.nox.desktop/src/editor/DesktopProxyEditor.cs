#if UNITY_EDITOR
using Nox.CCK.Players;
using UnityEditor;
using UnityEngine;

namespace api.nox.desktop {
	[CustomEditor(typeof(DesktopController))]
	public class DesktopControllerEditor : Editor {
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();

			var proxy = (DesktopController)target;
			if (!proxy) {
				EditorGUILayout.LabelField("Proxy is null");
				return;
			}

			var height = EditorGUILayout.FloatField("Height", proxy.Height);
			if (height < 0) {
				EditorGUILayout.HelpBox("Height cannot be negative", MessageType.Error);
			} else if (!Mathf.Approximately(height, proxy.Height)) {
				proxy.Height = height;
				EditorUtility.SetDirty(proxy);
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
			EditorGUILayout.Vector3Field("Keys", proxy.inputMovement);
			EditorGUILayout.Vector3Field("Velocity", proxy.bodyController.velocity);


			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Abilities", EditorStyles.boldLabel);
			foreach (var value in proxy.GetAbilities())
				switch (value.Value) {
					case bool b:
						var nb = EditorGUILayout.Toggle(value.Key, b);
						if (nb != b) {
							proxy.SetAbilities(value.Key, nb);
							EditorUtility.SetDirty(proxy);
						}

						break;
					case float f:
						var nf = EditorGUILayout.FloatField(value.Key, f);
						if (!Mathf.Approximately(nf, f)) {
							proxy.SetAbilities(value.Key, nf);
							EditorUtility.SetDirty(proxy);
						}

						break;
					case int i:
						var ni = EditorGUILayout.IntField(value.Key, i);
						if (ni != i) {
							proxy.SetAbilities(value.Key, ni);
							EditorUtility.SetDirty(proxy);
						}

						break;
					case string s:
						var ns = EditorGUILayout.TextField(s);
						if (ns != s) {
							proxy.SetAbilities(value.Key, ns);
							EditorUtility.SetDirty(proxy);
						}

						break;
					default:
						EditorGUILayout.LabelField(value.Key, value.Value?.ToString() ?? "null");
						break;
				}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Parts", EditorStyles.boldLabel);
			foreach (var part in proxy.GetParts())
				EditorGUILayout.ObjectField(
					System.Enum.IsDefined(typeof(PlayerRig), part.Key)
						? System.Enum.GetName(typeof(PlayerRig), part.Key)
						: $"[{part.Key}]", part.Value, typeof(Transform), true
				);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Keys", EditorStyles.boldLabel);
			foreach (var key in Keybindings.Keys)
				EditorGUILayout.LabelField($"{key.Item1}.{key.Item2}", key.Item5.ToString("F2"));
		}
	}
}
#endif