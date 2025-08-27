#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Nox.CCK.Avatars.StateMachines;
using Nox.Avatars.Parameters;
using System.Text;

namespace api.nox.avatar.modules {
	[CustomEditor(typeof(SetParameter))]
	public class SetParameterEditor : Editor {
		private SerializedProperty keyProperty;
		private SerializedProperty typeProperty;
		private SerializedProperty actionProperty;
		private SerializedProperty valueProperty;
		
		private string stringValue = "";
		private bool boolValue = false;
		private byte byteValue = 0;
		private short shortValue = 0;
		private ushort ushortValue = 0;
		private int intValue = 0;
		private uint uintValue = 0;
		private long longValue = 0;
		private ulong ulongValue = 0;
		private float floatValue = 0f;
		private double doubleValue = 0.0;
		private Vector3 vector3Value = Vector3.zero;
		private Quaternion quaternionValue = Quaternion.identity;

		private void OnEnable() {
			keyProperty = serializedObject.FindProperty("key");
			typeProperty = serializedObject.FindProperty("type");
			actionProperty = serializedObject.FindProperty("action");
			valueProperty = serializedObject.FindProperty("value");
			
			// Charger la valeur actuelle
			LoadCurrentValue();
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			
			EditorGUILayout.PropertyField(keyProperty, new GUIContent("Parameter Key"));
			EditorGUILayout.PropertyField(actionProperty, new GUIContent("Action"));
			EditorGUILayout.PropertyField(typeProperty, new GUIContent("Type"));
			
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Value", EditorStyles.boldLabel);
			
			var parameterType = (ParameterType)typeProperty.enumValueIndex;
			DrawValueField(parameterType);
			
			if (GUI.changed) {
				UpdateValueBytes(parameterType);
				serializedObject.ApplyModifiedProperties();
			}
		}

		private void DrawValueField(ParameterType parameterType) {
			switch (parameterType) {
				case ParameterType.Bool:
					boolValue = EditorGUILayout.Toggle("Boolean Value", boolValue);
					break;
				case ParameterType.Byte:
					byteValue = (byte)EditorGUILayout.IntSlider("Byte Value", byteValue, 0, 255);
					break;
				case ParameterType.Short:
					shortValue = (short)EditorGUILayout.IntField("Short Value", shortValue);
					break;
				case ParameterType.UShort:
					ushortValue = (ushort)EditorGUILayout.IntField("Unsigned Short Value", ushortValue);
					break;
				case ParameterType.Int:
					intValue = EditorGUILayout.IntField("Int Value", intValue);
					break;
				case ParameterType.UInt:
					uintValue = (uint)EditorGUILayout.LongField("Unsigned Int Value", uintValue);
					break;
				case ParameterType.Long:
					longValue = EditorGUILayout.LongField("Long Value", longValue);
					break;
				case ParameterType.ULong:
					ulongValue = (ulong)EditorGUILayout.LongField("Unsigned Long Value", (long)ulongValue);
					break;
				case ParameterType.Float:
					floatValue = EditorGUILayout.FloatField("Float Value", floatValue);
					break;
				case ParameterType.Double:
					doubleValue = EditorGUILayout.DoubleField("Double Value", doubleValue);
					break;
				case ParameterType.String:
					stringValue = EditorGUILayout.TextField("String Value", stringValue);
					break;
				case ParameterType.Vector3:
					vector3Value = EditorGUILayout.Vector3Field("Vector3 Value", vector3Value);
					break;
				case ParameterType.Quaternion:
					quaternionValue = Quaternion.Euler(EditorGUILayout.Vector3Field("Quaternion (Euler)", quaternionValue.eulerAngles));
					break;
				case ParameterType.ByteArray:
					EditorGUILayout.HelpBox("Byte Array editing not supported in inspector. Use code to set values.", MessageType.Info);
					break;
			}
		}

		private void UpdateValueBytes(ParameterType parameterType) {
			var setParameter = target as SetParameter;
			if (setParameter == null) return;

			switch (parameterType) {
				case ParameterType.Bool:
					setParameter.SetValue(boolValue);
					break;
				case ParameterType.Byte:
					setParameter.SetValue(byteValue);
					break;
				case ParameterType.Short:
					setParameter.SetValue(shortValue);
					break;
				case ParameterType.UShort:
					setParameter.SetValue(ushortValue);
					break;
				case ParameterType.Int:
					setParameter.SetValue(intValue);
					break;
				case ParameterType.UInt:
					setParameter.SetValue(uintValue);
					break;
				case ParameterType.Long:
					setParameter.SetValue(longValue);
					break;
				case ParameterType.ULong:
					setParameter.SetValue(ulongValue);
					break;
				case ParameterType.Float:
					setParameter.SetValue(floatValue);
					break;
				case ParameterType.Double:
					setParameter.SetValue(doubleValue);
					break;
				case ParameterType.String:
					setParameter.SetValue(stringValue);
					break;
				case ParameterType.Vector3:
					setParameter.SetValue(vector3Value);
					break;
				case ParameterType.Quaternion:
					setParameter.SetValue(quaternionValue);
					break;
			}
		}

		private void LoadCurrentValue() {
			var setParameter = target as SetParameter;
			if (setParameter == null || setParameter.value == null || setParameter.value.Length == 0)
				return;

			try {
				switch (setParameter.type) {
					case ParameterType.Bool:
						boolValue = setParameter.GetValue<bool>();
						break;
					case ParameterType.Byte:
						byteValue = setParameter.GetValue<byte>();
						break;
					case ParameterType.Short:
						shortValue = setParameter.GetValue<short>();
						break;
					case ParameterType.UShort:
						ushortValue = setParameter.GetValue<ushort>();
						break;
					case ParameterType.Int:
						intValue = setParameter.GetValue<int>();
						break;
					case ParameterType.UInt:
						uintValue = setParameter.GetValue<uint>();
						break;
					case ParameterType.Long:
						longValue = setParameter.GetValue<long>();
						break;
					case ParameterType.ULong:
						ulongValue = setParameter.GetValue<ulong>();
						break;
					case ParameterType.Float:
						floatValue = setParameter.GetValue<float>();
						break;
					case ParameterType.Double:
						doubleValue = setParameter.GetValue<double>();
						break;
					case ParameterType.String:
						stringValue = setParameter.GetValue<string>() ?? "";
						break;
					case ParameterType.Vector3:
						vector3Value = setParameter.GetValue<Vector3>();
						break;
					case ParameterType.Quaternion:
						quaternionValue = setParameter.GetValue<Quaternion>();
						break;
				}
			} catch {
				// Si la désérialisation échoue, utiliser les valeurs par défaut
			}
		}
	}
}
#endif