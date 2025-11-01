#if UNITY_EDITOR
using System;
using Nox.CCK.Avatars.Parameters;
using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using UnityEditor;
using UnityEngine;

namespace api.nox.avatar.modules {
	[CustomEditor(typeof(AvatarParameterModule))]
	public class AvatarParameterModuleEditor : Editor {
		private AvatarParameterModule _target;
		private SerializedProperty    _parametersProperty;

		private void OnEnable() {
			_target             = (AvatarParameterModule)target;
			_parametersProperty = serializedObject.FindProperty("parameters");
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			EditorGUILayout.Space();

			// En-tête avec style
			GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) {
				fontSize  = 14,
				alignment = TextAnchor.MiddleCenter
			};
			EditorGUILayout.LabelField("Avatar Parameter Module", headerStyle);

			EditorGUILayout.Space();

			// Référence AvatarParameters
			EditorGUILayout.PropertyField(_parametersProperty, new GUIContent("Avatar Parameters Asset"));

			EditorGUILayout.Space();

			// Valeurs actuelles modifiables
			DrawRuntimeValuesSection();

			serializedObject.ApplyModifiedProperties();

			if (Application.isPlaying)
				Repaint();
		}

		private void DrawRuntimeValuesSection() {
			if (!Application.isPlaying) {
				EditorGUILayout.HelpBox("Entrez en mode Play pour voir et modifier les valeurs des paramètres.", MessageType.Info);
				return;
			}

			var runtimeParams = _target.GetParameters();

			if (runtimeParams.Length == 0) {
				EditorGUILayout.HelpBox("Aucun paramètre trouvé dans l'animateur.", MessageType.Info);
				Repaint();
				return;
			}

			// Récupérer l'animateur depuis les paramètres runtime
			var animator = runtimeParams.Length > 0 ? _target.Descriptor?.GetAnimator() : null;
			if (!animator) {
				EditorGUILayout.HelpBox("Aucun animateur trouvé sur l'avatar.", MessageType.Warning);
				Repaint();
				return;
			}

			EditorGUILayout.Space(5);
			EditorGUILayout.LabelField($"Paramètres runtime: {runtimeParams.Length}", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			foreach (var param in runtimeParams)
				DrawRuntimeParameterField(param);
		}

		private void DrawRuntimeParameterField(IParameter param) {
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);

			EditorGUI.indentLevel++;

			// Affichage et modification de la valeur selon le type
			EditorGUI.BeginChangeCheck();

			var s        = $"{param.GetName()}/{param.GetHash()}";
			var readOnly = param.IsReadOnly();

			// Désactiver les contrôles GUI si le paramètre est en lecture seule
			EditorGUI.BeginDisabledGroup(readOnly);

			switch (param.GetValueType()) {
				case ParameterType.Bool:
					var boolValue    = param.Get().ToBool();
					var newBoolValue = EditorGUILayout.Toggle(s, boolValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newBoolValue);
					break;

				case ParameterType.Int:
					var intValue    = param.Get().ToInt();
					var newIntValue = EditorGUILayout.IntField(s, intValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newIntValue);
					break;

				case ParameterType.UInt:
					var uintValue    = param.Get().ToUInt();
					var newUIntValue = EditorGUILayout.IntField(s, uintValue.ToInt());
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newUIntValue.ToInt());
					break;

				case ParameterType.Long:
					var longValue    = param.Get().ToLong();
					var newLongValue = EditorGUILayout.LongField(s, longValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newLongValue);
					break;

				case ParameterType.ULong:
					var ulongValue    = param.Get().ToULong();
					var newULongValue = EditorGUILayout.LongField(s, ulongValue.ToLong());
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newULongValue.ToULong());
					break;

				case ParameterType.Byte:
					var byteValue    = param.Get().ToByte();
					var newByteValue = EditorGUILayout.IntField(s, byteValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newByteValue.ToByte());
					break;

				case ParameterType.Short:
					var shortValue    = param.Get().ToShort();
					var newShortValue = EditorGUILayout.IntField(s, shortValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newShortValue);
					break;

				case ParameterType.UShort:
					var ushortValue    = param.Get().ToUShort();
					var newUShortValue = EditorGUILayout.IntField(s, ushortValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newUShortValue);
					break;

				case ParameterType.Float:
					var floatValue    = param.Get().ToFloat();
					var newFloatValue = EditorGUILayout.FloatField(s, floatValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newFloatValue);
					break;

				case ParameterType.Double:
					var doubleValue    = param.Get().ToDouble();
					var newDoubleValue = EditorGUILayout.DoubleField(s, doubleValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newDoubleValue);
					break;

				case ParameterType.String:
					var stringValue    = param.Get().ToString();
					var newStringValue = EditorGUILayout.TextField(s, stringValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newStringValue);
					break;

				case ParameterType.Vector3:
					var vector3Value    = param.Get().ToVector3();
					var newVector3Value = EditorGUILayout.Vector3Field(s, vector3Value);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newVector3Value);
					break;

				case ParameterType.Quaternion:
					var quaternionValue = param.Get().ToQuaternion();
					var eulerAngles     = quaternionValue.eulerAngles;
					var newEulerAngles  = EditorGUILayout.Vector3Field(s + " (Euler)", eulerAngles);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(Quaternion.Euler(newEulerAngles));
					break;

				case ParameterType.ByteArray:
					var byteArrayValue = (byte[])param.Get();
					var newByteArrayValue = EditorGUILayout.TextField(
						s, byteArrayValue != null
							? System.Text.Encoding.UTF8.GetString(byteArrayValue)
							: string.Empty
					);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(
							!string.IsNullOrEmpty(newByteArrayValue)
								? System.Text.Encoding.UTF8.GetBytes(newByteArrayValue)
								: Array.Empty<byte>()
						);
					break;

				default:
					EditorGUILayout.LabelField(s, $"Type {param.GetValueType()} non supporté pour l'édition.");
					break;
			}

			EditorGUI.EndDisabledGroup();

			// Afficher une indication si le paramètre est en lecture seule
			if (readOnly) {
				EditorGUI.indentLevel++;
				EditorGUILayout.LabelField("", "(Lecture seule)", EditorStyles.miniLabel);
				EditorGUI.indentLevel--;
			}

			EditorGUI.indentLevel--;
			EditorGUILayout.EndVertical();
		}
	}
}
#endif