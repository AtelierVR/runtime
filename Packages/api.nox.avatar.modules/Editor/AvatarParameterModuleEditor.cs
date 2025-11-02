#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Avatars.Parameters;
using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.avatar.modules {
	[CustomEditor(typeof(AvatarParameterModule))]
	public class AvatarParameterModuleEditor : Editor {
		private AvatarParameterModule _target;
		private VisualElement         _root;
		private Label                 _infoLabel;
		private VisualElement         _parametersContainer;
		private PropertyField         _parametersProperty;

		private Dictionary<string, ParameterFieldTracker> _parameterFields = new();
		private bool                                      _isPlaying;

		// Classe pour tracker les champs de paramètres
		private class ParameterFieldTracker {
			public VisualElement Container;
			public VisualElement Field;
			public IParameter    Parameter;
			public object        LastValue;
			public bool          IsFocused;
		}

		public override VisualElement CreateInspectorGUI() {
			_target = (AvatarParameterModule)target;

			// Charger le UXML
			var visualTree = Resources.Load<VisualTreeAsset>("AvatarParameterModuleEditor");
			_root          = visualTree.CloneTree();

			// Récupérer les éléments
			_parametersProperty   = _root.Q<PropertyField>("parameters-property");
			_infoLabel            = _root.Q<Label>("info-label");
			_parametersContainer  = _root.Q<VisualElement>("parameters-container");

			// Bind la propriété
			_parametersProperty.BindProperty(serializedObject.FindProperty("parameters"));

			// Mettre à jour l'affichage initial
			UpdateRuntimeSection();

			// Planifier les mises à jour en mode Play
			if (Application.isPlaying) {
				_isPlaying = true;
				_root.schedule.Execute(UpdateParameterValues).Every(100); // Update toutes les 100ms
			}

			// S'abonner aux changements de mode Play
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			return _root;
		}

		private void OnDisable() {
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state) {
			if (state == PlayModeStateChange.EnteredPlayMode) {
				_isPlaying = true;
				UpdateRuntimeSection();
				_root?.schedule.Execute(UpdateParameterValues).Every(100);
			}
			else if (state == PlayModeStateChange.ExitingPlayMode) {
				_isPlaying = false;
				_parameterFields.Clear();
				UpdateRuntimeSection();
			}
		}

		private void UpdateRuntimeSection() {
			if (!Application.isPlaying) {
				_infoLabel.text  = "Entrez en mode Play pour voir et modifier les valeurs des paramètres.";
				_infoLabel.style.display = DisplayStyle.Flex;
				_parametersContainer.Clear();
				_parameterFields.Clear();
				return;
			}

			var runtimeParams = _target.GetParameters();

			if (runtimeParams.Length == 0) {
				_infoLabel.text  = "Aucun paramètre trouvé dans l'animateur.";
				_infoLabel.style.display = DisplayStyle.Flex;
				_parametersContainer.Clear();
				return;
			}

			var animator = _target.Runtime.GetDescriptor()?.GetAnimator();
			if (!animator) {
				_infoLabel.text  = "Aucun animateur trouvé sur l'avatar.";
				_infoLabel.style.display = DisplayStyle.Flex;
				_parametersContainer.Clear();
				return;
			}

			_infoLabel.text  = $"Paramètres runtime: {runtimeParams.Length}";
			_infoLabel.style.display = DisplayStyle.Flex;

			var isLocal = runtimeParams
					.FirstOrDefault(e => e.GetName().Contains("IsLocal"))
					?.Get()
					.ToBool()
				?? true;

			// Créer les champs pour chaque paramètre
			_parametersContainer.Clear();
			_parameterFields.Clear();

			foreach (var param in runtimeParams)
				CreateParameterField(param, isLocal);
		}

		private void CreateParameterField(IParameter param, bool isLocal) {
			var container = new VisualElement();
			container.AddToClassList("parameter-field");

			var editable = param.GetFlags().HasFlag(isLocal ? ParameterFlags.LocalEditable : ParameterFlags.RemoteEditable);
			var field    = CreateFieldForParameter(param, editable);

			if (field != null) {
				container.Add(field);

				if (!editable) {
					var readonlyLabel = new Label("(Lecture seule)");
					readonlyLabel.AddToClassList("readonly-label");
					container.Add(readonlyLabel);
				}

				_parametersContainer.Add(container);

				// Tracker ce champ
				var tracker = new ParameterFieldTracker {
					Container = container,
					Field     = field,
					Parameter = param,
					LastValue = GetParameterValue(param),
					IsFocused = false
				};

				_parameterFields[param.GetName()] = tracker;

				// Détecter le focus pour arrêter les updates
				field.RegisterCallback<FocusInEvent>(evt => {
					if (_parameterFields.TryGetValue(param.GetName(), out var t))
						t.IsFocused = true;
				});

				field.RegisterCallback<FocusOutEvent>(evt => {
					if (_parameterFields.TryGetValue(param.GetName(), out var t))
						t.IsFocused = false;
				});
			}
		}

		private VisualElement CreateFieldForParameter(IParameter param, bool editable) {
			var paramName = param.GetName();

			switch (param.GetValueType()) {
				case ParameterType.Bool:
					var toggleField = new Toggle(paramName);
					toggleField.SetEnabled(editable);
					toggleField.value = param.Get().ToBool();
					if (editable)
						toggleField.RegisterValueChangedCallback(evt => param.Set(evt.newValue));
					return toggleField;

				case ParameterType.Int:
					var intField = new IntegerField(paramName);
					intField.SetEnabled(editable);
					intField.value = param.Get().ToInt();
					if (editable)
						intField.RegisterValueChangedCallback(evt => param.Set(evt.newValue));
					return intField;

				case ParameterType.UInt:
					var uintField = new IntegerField(paramName);
					uintField.SetEnabled(editable);
					uintField.value = param.Get().ToUInt().ToInt();
					if (editable)
						uintField.RegisterValueChangedCallback(evt => param.Set((uint)evt.newValue));
					return uintField;

				case ParameterType.Long:
					var longField = new LongField(paramName);
					longField.SetEnabled(editable);
					longField.value = param.Get().ToLong();
					if (editable)
						longField.RegisterValueChangedCallback(evt => param.Set(evt.newValue));
					return longField;

				case ParameterType.ULong:
					var ulongField = new LongField(paramName);
					ulongField.SetEnabled(editable);
					ulongField.value = param.Get().ToULong().ToLong();
					if (editable)
						ulongField.RegisterValueChangedCallback(evt => param.Set((ulong)evt.newValue));
					return ulongField;

				case ParameterType.Byte:
					var byteField = new IntegerField(paramName);
					byteField.SetEnabled(editable);
					byteField.value = param.Get().ToByte();
					if (editable)
						byteField.RegisterValueChangedCallback(evt => param.Set((byte)evt.newValue));
					return byteField;

				case ParameterType.Short:
					var shortField = new IntegerField(paramName);
					shortField.SetEnabled(editable);
					shortField.value = param.Get().ToShort();
					if (editable)
						shortField.RegisterValueChangedCallback(evt => param.Set((short)evt.newValue));
					return shortField;

				case ParameterType.UShort:
					var ushortField = new IntegerField(paramName);
					ushortField.SetEnabled(editable);
					ushortField.value = param.Get().ToUShort();
					if (editable)
						ushortField.RegisterValueChangedCallback(evt => param.Set((ushort)evt.newValue));
					return ushortField;

				case ParameterType.Float:
					var floatField = new FloatField(paramName);
					floatField.SetEnabled(editable);
					floatField.value = param.Get().ToFloat();
					if (editable)
						floatField.RegisterValueChangedCallback(evt => param.Set(evt.newValue));
					return floatField;

				case ParameterType.Double:
					var doubleField = new DoubleField(paramName);
					doubleField.SetEnabled(editable);
					doubleField.value = param.Get().ToDouble();
					if (editable)
						doubleField.RegisterValueChangedCallback(evt => param.Set(evt.newValue));
					return doubleField;

				case ParameterType.String:
					var textField = new TextField(paramName);
					textField.SetEnabled(editable);
					textField.value = param.Get().ToString();
					if (editable)
						textField.RegisterValueChangedCallback(evt => param.Set(evt.newValue));
					return textField;

				case ParameterType.Vector3:
					var vector3Field = new Vector3Field(paramName);
					vector3Field.SetEnabled(editable);
					vector3Field.value = param.Get().ToVector3();
					if (editable)
						vector3Field.RegisterValueChangedCallback(evt => param.Set(evt.newValue));
					return vector3Field;

				case ParameterType.Quaternion:
					var quatField = new Vector3Field(paramName + " (Euler)");
					quatField.SetEnabled(editable);
					quatField.value = param.Get().ToQuaternion().eulerAngles;
					if (editable)
						quatField.RegisterValueChangedCallback(evt => param.Set(Quaternion.Euler(evt.newValue)));
					return quatField;

				case ParameterType.ByteArray:
					var byteArrayField = new TextField(paramName);
					byteArrayField.SetEnabled(editable);
					var byteArray = (byte[])param.Get();
					byteArrayField.value = byteArray != null ? System.Text.Encoding.UTF8.GetString(byteArray) : string.Empty;
					if (editable) {
						byteArrayField.RegisterValueChangedCallback(evt => {
							param.Set(!string.IsNullOrEmpty(evt.newValue)
								? System.Text.Encoding.UTF8.GetBytes(evt.newValue)
								: Array.Empty<byte>());
						});
					}
					return byteArrayField;

				default:
					var label = new Label($"{paramName}: Type {param.GetValueType()} non supporté");
					return label;
			}
		}

		private void UpdateParameterValues() {
			if (!_isPlaying || _parameterFields.Count == 0)
				return;

			foreach (var kvp in _parameterFields) {
				var tracker = kvp.Value;

				// Ne pas mettre à jour si le champ a le focus
				if (tracker.IsFocused)
					continue;

				var currentValue = GetParameterValue(tracker.Parameter);

				// Mettre à jour uniquement si la valeur a changé
				if (!ValuesAreEqual(tracker.LastValue, currentValue)) {
					UpdateFieldValue(tracker.Field, tracker.Parameter);
					tracker.LastValue = currentValue;
				}
			}
		}

		private object GetParameterValue(IParameter param) {
			switch (param.GetValueType()) {
				case ParameterType.Bool:       return param.Get().ToBool();
				case ParameterType.Int:        return param.Get().ToInt();
				case ParameterType.UInt:       return param.Get().ToUInt();
				case ParameterType.Long:       return param.Get().ToLong();
				case ParameterType.ULong:      return param.Get().ToULong();
				case ParameterType.Byte:       return param.Get().ToByte();
				case ParameterType.Short:      return param.Get().ToShort();
				case ParameterType.UShort:     return param.Get().ToUShort();
				case ParameterType.Float:      return param.Get().ToFloat();
				case ParameterType.Double:     return param.Get().ToDouble();
				case ParameterType.String:     return param.Get().ToString();
				case ParameterType.Vector3:    return param.Get().ToVector3();
				case ParameterType.Quaternion: return param.Get().ToQuaternion();
				case ParameterType.ByteArray:  return param.Get();
				default:                       return null;
			}
		}

		private bool ValuesAreEqual(object val1, object val2) {
			if (val1 == null && val2 == null) return true;
			if (val1 == null || val2 == null) return false;

			if (val1 is Vector3 v1 && val2 is Vector3 v2)
				return v1 == v2;

			if (val1 is Quaternion q1 && val2 is Quaternion q2)
				return q1 == q2;

			if (val1 is byte[] b1 && val2 is byte[] b2) {
				if (b1.Length != b2.Length) return false;
				for (int i = 0; i < b1.Length; i++)
					if (b1[i] != b2[i]) return false;
				return true;
			}

			return val1.Equals(val2);
		}

		private void UpdateFieldValue(VisualElement field, IParameter param) {
			switch (param.GetValueType()) {
				case ParameterType.Bool:
					((Toggle)field).SetValueWithoutNotify(param.Get().ToBool());
					break;
				case ParameterType.Int:
					((IntegerField)field).SetValueWithoutNotify(param.Get().ToInt());
					break;
				case ParameterType.UInt:
					((IntegerField)field).SetValueWithoutNotify(param.Get().ToUInt().ToInt());
					break;
				case ParameterType.Long:
					((LongField)field).SetValueWithoutNotify(param.Get().ToLong());
					break;
				case ParameterType.ULong:
					((LongField)field).SetValueWithoutNotify(param.Get().ToULong().ToLong());
					break;
				case ParameterType.Byte:
					((IntegerField)field).SetValueWithoutNotify(param.Get().ToByte());
					break;
				case ParameterType.Short:
					((IntegerField)field).SetValueWithoutNotify(param.Get().ToShort());
					break;
				case ParameterType.UShort:
					((IntegerField)field).SetValueWithoutNotify(param.Get().ToUShort());
					break;
				case ParameterType.Float:
					((FloatField)field).SetValueWithoutNotify(param.Get().ToFloat());
					break;
				case ParameterType.Double:
					((DoubleField)field).SetValueWithoutNotify(param.Get().ToDouble());
					break;
				case ParameterType.String:
					((TextField)field).SetValueWithoutNotify(param.Get().ToString());
					break;
				case ParameterType.Vector3:
					((Vector3Field)field).SetValueWithoutNotify(param.Get().ToVector3());
					break;
				case ParameterType.Quaternion:
					((Vector3Field)field).SetValueWithoutNotify(param.Get().ToQuaternion().eulerAngles);
					break;
				case ParameterType.ByteArray:
					var byteArray = (byte[])param.Get();
					((TextField)field).SetValueWithoutNotify(byteArray != null ? System.Text.Encoding.UTF8.GetString(byteArray) : string.Empty);
					break;
			}
		}
	}
}
#endif