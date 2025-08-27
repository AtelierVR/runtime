#if UNITY_EDITOR
using System;
using Nox.CCK.Avatars.Parameters;
using Nox.Avatars.Parameters;
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

			var s = $"{param.GetName()}";
			var readOnly = param.IsReadOnly();

			// Désactiver les contrôles GUI si le paramètre est en lecture seule
			EditorGUI.BeginDisabledGroup(readOnly);

			switch (param.GetValueType()) {
				case ParameterType.Bool:
					var boolValue    = (bool)param.Get();
					var newBoolValue = EditorGUILayout.Toggle(s, boolValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newBoolValue);
					break;

				case ParameterType.Int:
					var intValue    = (int)param.Get();
					var newIntValue = EditorGUILayout.IntField(s, intValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newIntValue);
					break;

				case ParameterType.UInt:
					var uintValue    = (uint)param.Get();
					var newUIntValue = EditorGUILayout.IntField(s, (int)uintValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set((uint)newUIntValue);
					break;

				case ParameterType.Long:
					var longValue    = (long)param.Get();
					var newLongValue = EditorGUILayout.LongField(s, longValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newLongValue);
					break;

				case ParameterType.ULong:
					var ulongValue    = (ulong)param.Get();
					var newULongValue = EditorGUILayout.LongField(s, (long)ulongValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set((ulong)newULongValue);
					break;

				case ParameterType.Byte:
					var byteValue    = (byte)param.Get();
					var newByteValue = EditorGUILayout.IntField(s, byteValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set((byte)newByteValue);
					break;

				case ParameterType.Short:
					var shortValue    = (short)param.Get();
					var newShortValue = EditorGUILayout.IntField(s, shortValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newShortValue);
					break;

				case ParameterType.UShort:
					var ushortValue    = (ushort)param.Get();
					var newUShortValue = EditorGUILayout.IntField(s, ushortValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newUShortValue);
					break;

				case ParameterType.Float:
					var floatValue    = (float)param.Get();
					var newFloatValue = EditorGUILayout.FloatField(s, floatValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newFloatValue);
					break;

				case ParameterType.Double:
					var doubleValue    = (double)param.Get();
					var newDoubleValue = EditorGUILayout.DoubleField(s, doubleValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newDoubleValue);
					break;

				case ParameterType.String:
					var stringValue    = (string)param.Get();
					var newStringValue = EditorGUILayout.TextField(s, stringValue);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newStringValue);
					break;

				case ParameterType.Vector3:
					var vector3Value    = param.Get() is Vector3 v3 ? v3 : Vector3.zero;
					var newVector3Value = EditorGUILayout.Vector3Field(s, vector3Value);
					if (EditorGUI.EndChangeCheck() && !readOnly)
						param.Set(newVector3Value);
					break;

				case ParameterType.Quaternion:
					var quaternionValue = param.Get() is Quaternion q ? q : Quaternion.identity;
					var eulerAngles = quaternionValue.eulerAngles;
					var newEulerAngles = EditorGUILayout.Vector3Field(s + " (Euler)", eulerAngles);
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