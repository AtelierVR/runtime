#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Parameters;

namespace api.nox.avatar.modules {
	[CustomEditor(typeof(AvatarParameters))]
	public class AvatarParametersEditor : Editor {
		private VisualElement        _root;
		private ListView             _listView;
		private AvatarParameters     targetParameters;
		private List<ParameterEntry> parametersList;

		public override VisualElement CreateInspectorGUI() {
			targetParameters = (AvatarParameters)target;
			parametersList   = targetParameters.parameters?.ToList() ?? new List<ParameterEntry>();
			_root            = Resources.Load<VisualTreeAsset>("AvatarParametersEditor").CloneTree();
			_listView        = _root.Q<ListView>("parameters");
			SetupListView();
			RefreshUI();
			return _root;
		}

		private void SetupListView() {
			_listView.itemTemplate     =  Resources.Load<VisualTreeAsset>("AvatarParametersEntryEditor");
			_listView.bindItem         =  BindParameterElement;
			_listView.itemsAdded       += OnItemsAdded;
			_listView.itemsRemoved     += OnItemsRemoved;
			_listView.itemIndexChanged += OnItemIndexChanged;
		}

		private void BindParameterElement(VisualElement element, int index) {
			if (index >= parametersList.Count) return;

			var parameter = parametersList[index];

			// Récupérer les références des champs
			var nameField             = element.Q<TextField>("name");
			var typeField             = element.Q<EnumField>("type");
			var syncedToggle          = element.Q<Toggle>("synced");
			var savableToggle         = element.Q<Toggle>("savable");
			var defaultValueContainer = element.Q<VisualElement>("default-value");

			// Nettoyer les anciens événements (seulement s'ils existent)
			if (nameField.userData is EventCallback<ChangeEvent<string>> nameCallback)
				nameField.UnregisterValueChangedCallback(nameCallback);

			if (typeField.userData is EventCallback<ChangeEvent<Enum>> typeCallback)
				typeField.UnregisterValueChangedCallback(typeCallback);

			if (syncedToggle.userData is EventCallback<ChangeEvent<bool>> syncedCallback)
				syncedToggle.UnregisterValueChangedCallback(syncedCallback);

			if (savableToggle.userData is EventCallback<ChangeEvent<bool>> savableCallback)
				savableToggle.UnregisterValueChangedCallback(savableCallback);

			// Initialiser les valeurs
			nameField.value     = parameter.name;
			typeField.value     = parameter.type;
			syncedToggle.value  = parameter.synced;
			savableToggle.value = parameter.savable;

			// Configurer le conteneur de valeur par défaut
			SetupDefaultValueField(defaultValueContainer, parameter, index);

			// Configurer les événements avec capture d'index
			EventCallback<ChangeEvent<string>> newNameCallback = evt => {
				Undo.RecordObject(targetParameters, "Change Parameter Name");
				parametersList[index] = new ParameterEntry {
					name         = evt.newValue,
					type         = parametersList[index].type,
					defaultValue = parametersList[index].defaultValue,
					synced       = parametersList[index].synced,
					savable      = parametersList[index].savable
				};
				targetParameters.parameters = parametersList.ToArray();
				EditorUtility.SetDirty(targetParameters);
			};

			EventCallback<ChangeEvent<Enum>> newTypeCallback = evt => {
				Undo.RecordObject(targetParameters, "Change Parameter Type");
				parametersList[index] = new ParameterEntry {
					name         = parametersList[index].name,
					type         = (ParameterType)evt.newValue,
					defaultValue = Array.Empty<byte>(),
					synced       = parametersList[index].synced,
					savable      = parametersList[index].savable
				};
				targetParameters.parameters = parametersList.ToArray();
				EditorUtility.SetDirty(targetParameters);

				// Recréer le champ de valeur par défaut avec le nouveau type
				SetupDefaultValueField(defaultValueContainer, parametersList[index], index);
			};

			EventCallback<ChangeEvent<bool>> newSyncedCallback = evt => {
				Undo.RecordObject(targetParameters, "Change Parameter Synced");
				parametersList[index] = new ParameterEntry {
					name         = parametersList[index].name,
					type         = parametersList[index].type,
					defaultValue = parametersList[index].defaultValue,
					synced       = evt.newValue,
					savable      = parametersList[index].savable
				};
				targetParameters.parameters = parametersList.ToArray();
				EditorUtility.SetDirty(targetParameters);
			};

			EventCallback<ChangeEvent<bool>> newSavableCallback = evt => {
				Undo.RecordObject(targetParameters, "Change Parameter Savable");
				parametersList[index] = new ParameterEntry {
					name         = parametersList[index].name,
					type         = parametersList[index].type,
					defaultValue = parametersList[index].defaultValue,
					synced       = parametersList[index].synced,
					savable      = evt.newValue
				};
				targetParameters.parameters = parametersList.ToArray();
				EditorUtility.SetDirty(targetParameters);
			};

			nameField.RegisterValueChangedCallback(newNameCallback);
			typeField.RegisterValueChangedCallback(newTypeCallback);
			syncedToggle.RegisterValueChangedCallback(newSyncedCallback);
			savableToggle.RegisterValueChangedCallback(newSavableCallback);

			// Stocker les callbacks pour les nettoyer plus tard
			nameField.userData     = newNameCallback;
			typeField.userData     = newTypeCallback;
			syncedToggle.userData  = newSyncedCallback;
			savableToggle.userData = newSavableCallback;
		}

		private void SetupDefaultValueField(VisualElement container, ParameterEntry parameter, int index) {
			container.Clear();

			switch (parameter.type) {
				case ParameterType.Bool:
					var boolField = new Toggle {
						value = parameter.defaultValue?.Length > 0 && parameter.GetDefaultValue<bool>()
					};
					boolField.AddToClassList("large");
					boolField.RegisterValueChangedCallback(
						evt => {
							Undo.RecordObject(targetParameters, "Change Parameter Default Value");
							var updatedParameter = parametersList[index];
							updatedParameter.SetDefaultValue(evt.newValue);
							parametersList[index]       = updatedParameter;
							targetParameters.parameters = parametersList.ToArray();
							EditorUtility.SetDirty(targetParameters);
						}
					);
					container.Add(boolField);
					break;

				case ParameterType.Byte:
					var byteField = new IntegerField {
						value = parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<byte>() : 0
					};
					byteField.AddToClassList("large");
					byteField.RegisterValueChangedCallback(
						evt => {
							Undo.RecordObject(targetParameters, "Change Parameter Default Value");
							var updatedParameter = parametersList[index];
							updatedParameter.SetDefaultValue((byte)Mathf.Clamp(evt.newValue, 0, 255));
							parametersList[index]       = updatedParameter;
							targetParameters.parameters = parametersList.ToArray();
							EditorUtility.SetDirty(targetParameters);
						}
					);
					container.Add(byteField);
					break;

				case ParameterType.Short:
					var shortField = new IntegerField {
						value = parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<short>() : 0
					};
					shortField.AddToClassList("large");
					shortField.RegisterValueChangedCallback(
						evt => {
							Undo.RecordObject(targetParameters, "Change Parameter Default Value");
							var updatedParameter = parametersList[index];
							updatedParameter.SetDefaultValue((short)evt.newValue);
							parametersList[index]       = updatedParameter;
							targetParameters.parameters = parametersList.ToArray();
							EditorUtility.SetDirty(targetParameters);
						}
					);
					container.Add(shortField);
					break;

				case ParameterType.UShort:
					var ushortField = new IntegerField {
						value = parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<ushort>() : 0
					};
					ushortField.AddToClassList("large");
					ushortField.RegisterValueChangedCallback(
						evt => {
							Undo.RecordObject(targetParameters, "Change Parameter Default Value");
							var updatedParameter = parametersList[index];
							updatedParameter.SetDefaultValue((ushort)Mathf.Clamp(evt.newValue, 0, ushort.MaxValue));
							parametersList[index]       = updatedParameter;
							targetParameters.parameters = parametersList.ToArray();
							EditorUtility.SetDirty(targetParameters);
						}
					);
					container.Add(ushortField);
					break;

				case ParameterType.Int:
					var intField = new IntegerField {
						value = parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<int>() : 0
					};
					intField.AddToClassList("large");
					intField.RegisterValueChangedCallback(
						evt => {
							Undo.RecordObject(targetParameters, "Change Parameter Default Value");
							var updatedParameter = parametersList[index];
							updatedParameter.SetDefaultValue(evt.newValue);
							parametersList[index]       = updatedParameter;
							targetParameters.parameters = parametersList.ToArray();
							EditorUtility.SetDirty(targetParameters);
						}
					);
					container.Add(intField);
					break;

				case ParameterType.UInt:
					var uintField = new LongField {
						value = parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<uint>() : 0
					};
					uintField.AddToClassList("large");
					uintField.RegisterValueChangedCallback(
						evt => {
							Undo.RecordObject(targetParameters, "Change Parameter Default Value");
							var updatedParameter = parametersList[index];
							updatedParameter.SetDefaultValue((uint)Mathf.Clamp(evt.newValue, 0, uint.MaxValue));
							parametersList[index]       = updatedParameter;
							targetParameters.parameters = parametersList.ToArray();
							EditorUtility.SetDirty(targetParameters);
						}
					);
					container.Add(uintField);
					break;

				case ParameterType.Long:
					var longField = new LongField {
						value = parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<long>() : 0
					};
					longField.AddToClassList("large");
					longField.RegisterValueChangedCallback(
						evt => {
							Undo.RecordObject(targetParameters, "Change Parameter Default Value");
							var updatedParameter = parametersList[index];
							updatedParameter.SetDefaultValue(evt.newValue);
							parametersList[index]       = updatedParameter;
							targetParameters.parameters = parametersList.ToArray();
							EditorUtility.SetDirty(targetParameters);
						}
					);
					container.Add(longField);
					break;

				case ParameterType.ULong:
					var ulongField = new LongField {
						value = (long)(parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<ulong>() : 0)
					};
					ulongField.AddToClassList("large");
					ulongField.RegisterValueChangedCallback(
						evt => {
							Undo.RecordObject(targetParameters, "Change Parameter Default Value");
							var updatedParameter = parametersList[index];
							updatedParameter.SetDefaultValue((ulong)Math.Max(0, evt.newValue));
							parametersList[index]       = updatedParameter;
							targetParameters.parameters = parametersList.ToArray();
							EditorUtility.SetDirty(targetParameters);
						}
					);
					container.Add(ulongField);
					break;

				case ParameterType.Float:
					var floatField = new FloatField {
						value = parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<float>() : 0f
					};
					floatField.AddToClassList("large");
					floatField.RegisterValueChangedCallback(
						evt => {
							Undo.RecordObject(targetParameters, "Change Parameter Default Value");
							var updatedParameter = parametersList[index];
							updatedParameter.SetDefaultValue(evt.newValue);
							parametersList[index]       = updatedParameter;
							targetParameters.parameters = parametersList.ToArray();
							EditorUtility.SetDirty(targetParameters);
						}
					);
					container.Add(floatField);
					break;

				case ParameterType.Double:
					var doubleField = new DoubleField {
						value = parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<double>() : 0.0
					};
					doubleField.AddToClassList("large");
					doubleField.RegisterValueChangedCallback(
						evt => {
							Undo.RecordObject(targetParameters, "Change Parameter Default Value");
							var updatedParameter = parametersList[index];
							updatedParameter.SetDefaultValue(evt.newValue);
							parametersList[index]       = updatedParameter;
							targetParameters.parameters = parametersList.ToArray();
							EditorUtility.SetDirty(targetParameters);
						}
					);
					container.Add(doubleField);
					break;

				case ParameterType.String:
					var stringField = new TextField {
						value = parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<string>() : ""
					};
					stringField.AddToClassList("large");
					stringField.RegisterValueChangedCallback(
						evt => {
							Undo.RecordObject(targetParameters, "Change Parameter Default Value");
							var updatedParameter = parametersList[index];
							updatedParameter.SetDefaultValue(evt.newValue ?? "");
							parametersList[index]       = updatedParameter;
							targetParameters.parameters = parametersList.ToArray();
							EditorUtility.SetDirty(targetParameters);
						}
					);
					container.Add(stringField);
					break;

				case ParameterType.ByteArray:
					container.Add(new Label("Byte Array editing not supported in UI"));
					break;

				default:
					container.Add(new Label($"Unknown type: {parameter.type}"));
					break;
			}
		}

		private void OnItemsAdded(IEnumerable<int> indices) {
			targetParameters.parameters = parametersList.ToArray();
			EditorUtility.SetDirty(targetParameters);
		}

		private void OnItemsRemoved(IEnumerable<int> indices) {
			targetParameters.parameters = parametersList.ToArray();
			EditorUtility.SetDirty(targetParameters);
		}

		private void OnItemIndexChanged(int srcIndex, int dstIndex) {
			Undo.RecordObject(targetParameters, "Reorder Parameters");
			targetParameters.parameters = parametersList.ToArray();
			EditorUtility.SetDirty(targetParameters);
		}

		private void RefreshUI() {
			parametersList        = targetParameters.parameters?.ToList() ?? new List<ParameterEntry>();
			_listView.itemsSource = parametersList;
			_listView.RefreshItems();
		}
	}
}
#endif