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
		private AvatarParameters Target
			=> (AvatarParameters)target;

		private MultiColumnListView  _listView;
		private List<ParameterEntry> _parametersList;

		public override VisualElement CreateInspectorGUI() {
			var root = new VisualElement();

			// Initialize parameters list
			_parametersList = new List<ParameterEntry>();
			if (Target.parameters != null)
				_parametersList.AddRange(Target.parameters);

			// Create MultiColumnListView
			CreateMultiColumnListView();
			root.Add(_listView);

			return root;
		}

		private void CreateMultiColumnListView() {
			// Create columns collection
			var columns = new Columns {
				// Name column
				new Column {
					name        = "name",
					title       = "Name",
					stretchable = true,
					sortable    = true,
					makeCell    = () => MakeContainer(new TextField()),
					bindCell = (element, index) => {
						var textField = element.Q<TextField>();
						if (textField == null || index < 0 || index >= _parametersList.Count || _parametersList[index] == null) return;
						textField.value = _parametersList[index].name ?? "";
						textField.UnregisterValueChangedCallback(OnNameChanged);
						textField.userData = index;
						textField.RegisterValueChangedCallback(OnNameChanged);
					}
				},
				// Type column
				new Column {
					name     = "type",
					title    = "Type",
					sortable = true,
					makeCell = () => MakeContainer(new EnumField()),
					bindCell = (element, index) => {
						var enumField = element.Q<EnumField>();
						if (enumField == null || index < 0 || index >= _parametersList.Count || _parametersList[index] == null) return;
						enumField.Init(ParameterType.Bool);
						enumField.value = _parametersList[index].type;
						enumField.UnregisterValueChangedCallback(OnTypeChanged);
						enumField.userData = index;
						enumField.RegisterValueChangedCallback(OnTypeChanged);
					}
				},
				// Default Value column
				new Column {
					name     = "defaultValue",
					title    = "Default Value",
					makeCell = () => MakeContainer(CreateDefaultValueControl()),
					bindCell = (element, index) => {
						if (index < 0 || index >= _parametersList.Count || _parametersList[index] == null) return;
						BindDefaultValueControl(element, index);
					}
				},
				// Synced column
				new Column {
					name     = "synced",
					title    = "Synced",
					sortable = true,
					makeCell = () => MakeContainer(new Toggle()),
					bindCell = (element, index) => {
						var toggle = element.Q<Toggle>();
						if (toggle == null || index < 0 || index >= _parametersList.Count || _parametersList[index] == null) return;
						toggle.value = _parametersList[index].synced;
						toggle.UnregisterValueChangedCallback(OnSyncedChanged);
						toggle.userData = index;
						toggle.RegisterValueChangedCallback(OnSyncedChanged);
					}
				},
				// Savable column
				new Column {
					name     = "savable",
					title    = "Savable",
					sortable = true,
					makeCell = () => MakeContainer(new Toggle()),
					bindCell = (element, index) => {
						var toggle = element.Q<Toggle>();
						if (toggle == null || index < 0 || index >= _parametersList.Count || _parametersList[index] == null) return;
						toggle.value = _parametersList[index].savable;
						toggle.UnregisterValueChangedCallback(OnSavableChanged);
						toggle.userData = index;
						toggle.RegisterValueChangedCallback(OnSavableChanged);
					}
				}
			};

			_listView = new MultiColumnListView(columns) {
				fixedItemHeight               = 25,
				itemsSource                   = _parametersList,
				showAlternatingRowBackgrounds = AlternatingRowBackground.All,
				showBorder                    = true,
				showFoldoutHeader             = true,
				showAddRemoveFooter           = true,
				headerTitle                   = "Parameters",
				sortingMode                   = ColumnSortingMode.Default,
				reorderable                   = true,
				reorderMode                   = ListViewReorderMode.Animated,
				style = {
					flexGrow  = 1,
					marginTop = 5
				},
				onAdd    = _ => OnAddParameter(),
				onRemove = OnRemoveParameter
			};
			
			// Register callback for item reordering
			_listView.itemsSourceChanged += SaveChangesInternal;
		}

		private static VisualElement MakeContainer(VisualElement element) {
			var container = new VisualElement {
				style = {
					flexDirection = FlexDirection.Row,
					alignItems    = Align.Center,
					marginRight   = 4,
					marginTop     = 2,
				}
			};
			element.style.flexGrow = 1;
			container.Add(element);
			return container;
		}

		// Separate callback methods to avoid closure issues
		private void OnNameChanged(ChangeEvent<string> evt) {
			if (evt.target is not TextField { userData: int index } || index >= _parametersList.Count) return;
			_parametersList[index].name = evt.newValue;
			SaveChangesInternal();
		}

		private void OnTypeChanged(ChangeEvent<Enum> evt) {
			if (evt.target is not EnumField { userData: int index } || index >= _parametersList.Count) return;
			var oldType = _parametersList[index].type;
			var newType = (ParameterType)evt.newValue;

			_parametersList[index].type = newType;

			if (oldType != newType) {
				_parametersList[index].defaultValue = Array.Empty<byte>();
				_listView.RefreshItems();
			}

			SaveChangesInternal();
		}

		private void OnSyncedChanged(ChangeEvent<bool> evt) {
			if (evt.target is not Toggle { userData: int index } || index >= _parametersList.Count) return;
			_parametersList[index].synced = evt.newValue;
			SaveChangesInternal();
		}

		private void OnSavableChanged(ChangeEvent<bool> evt) {
			if (evt.target is not Toggle { userData: int index } || index >= _parametersList.Count) return;
			_parametersList[index].savable = evt.newValue;
			SaveChangesInternal();
		}

		// Callbacks pour les différents types de contrôles
		private void OnBoolValueChanged(ChangeEvent<bool> evt) {
			if (evt.target is not VisualElement { userData: int index } || index >= _parametersList.Count) return;
			_parametersList[index].SetDefaultValue(evt.newValue);
			SaveChangesInternal();
		}

		private void OnIntegerValueChanged(ChangeEvent<long> evt) {
			if (evt.target is not VisualElement { userData: int index } || index >= _parametersList.Count) return;
			var parameter = _parametersList[index];

			try {
				switch (parameter.type) {
					case ParameterType.Byte:
						parameter.SetDefaultValue((byte)Math.Clamp(evt.newValue, byte.MinValue, byte.MaxValue));
						break;
					case ParameterType.Short:
						parameter.SetDefaultValue((short)Math.Clamp(evt.newValue, short.MinValue, short.MaxValue));
						break;
					case ParameterType.UShort:
						parameter.SetDefaultValue((ushort)Math.Clamp(evt.newValue, ushort.MinValue, ushort.MaxValue));
						break;
					case ParameterType.Int:
						parameter.SetDefaultValue((int)Math.Clamp(evt.newValue, int.MinValue, int.MaxValue));
						break;
					case ParameterType.UInt:
						parameter.SetDefaultValue((uint)Math.Clamp(evt.newValue, uint.MinValue, uint.MaxValue));
						break;
					case ParameterType.Long:
						parameter.SetDefaultValue(evt.newValue);
						break;
					case ParameterType.ULong:
						parameter.SetDefaultValue((ulong)Math.Max(0, evt.newValue));
						break;
				}

				SaveChangesInternal();
			} catch (Exception ex) {
				Debug.LogWarning($"Failed to set integer value {evt.newValue} for type {parameter.type}: {ex.Message}");
			}
		}

		private void OnFloatValueChanged(ChangeEvent<double> evt) {
			if (evt.target is not VisualElement { userData: int index } || index >= _parametersList.Count) return;
			var parameter = _parametersList[index];

			try {
				switch (parameter.type) {
					case ParameterType.Float:
						parameter.SetDefaultValue((float)evt.newValue);
						break;
					case ParameterType.Double:
						parameter.SetDefaultValue(evt.newValue);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				SaveChangesInternal();
			} catch (Exception ex) {
				Debug.LogWarning($"Failed to set float value {evt.newValue} for type {parameter.type}: {ex.Message}");
			}
		}

		private void OnStringValueChanged(ChangeEvent<string> evt) {
			if (evt.target is not VisualElement { userData: int index } || index >= _parametersList.Count) return;
			_parametersList[index].SetDefaultValue(evt.newValue ?? "");
			SaveChangesInternal();
		}

		private void OnVector3ValueChanged(ChangeEvent<Vector3> evt) {
			if (evt.target is not VisualElement { userData: int index } || index >= _parametersList.Count) return;
			_parametersList[index].SetDefaultValue(evt.newValue);
			SaveChangesInternal();
		}

		private void OnQuaternionValueChanged(ChangeEvent<Vector3> evt) {
			if (evt.target is not VisualElement { userData: int index } || index >= _parametersList.Count) return;
			var quat = Quaternion.Euler(evt.newValue);
			_parametersList[index].SetDefaultValue(quat);
			SaveChangesInternal();
		}

		private void OnByteArrayValueChanged(ChangeEvent<string> evt) {
			if (evt.target is not VisualElement element || element.userData is not int index || index >= _parametersList.Count) return;

			try {
				var bytes = string.IsNullOrEmpty(evt.newValue) ? Array.Empty<byte>() : Convert.FromBase64String(evt.newValue);
				_parametersList[index].SetDefaultValue(bytes);
				SaveChangesInternal();
			} catch (Exception ex) {
				Debug.LogWarning($"Failed to parse Base64 string: {ex.Message}");
			}
		}

		// Callbacks pour ajouter et supprimer des paramètres
		private void OnAddParameter() {
			var newParameter = new ParameterEntry {
				name = "New Parameter",
				type = ParameterType.Bool,
				defaultValue = Array.Empty<byte>(),
				synced = true,
				savable = false
			};

			_parametersList.Add(newParameter);
			_listView.RefreshItems();
			SaveChangesInternal();
		}

		private void OnRemoveParameter(BaseListView listView) {
			var selectedIndices = listView.selectedIndices.ToList();
			if (selectedIndices.Count == 0) return;

			// Trier les indices en ordre décroissant pour éviter les problèmes d'index lors de la suppression
			selectedIndices.Sort((a, b) => b.CompareTo(a));

			foreach (var index in selectedIndices.Where(index => index >= 0 && index < _parametersList.Count)) 
				_parametersList.RemoveAt(index);

			_listView.RefreshItems();
			SaveChangesInternal();
		}

		private void SaveChangesInternal() {
			Target.parameters = _parametersList.ToArray();
			EditorUtility.SetDirty(Target);
		}

		private static VisualElement CreateDefaultValueControl()
			=> new() {
				style = {
					flexDirection = FlexDirection.Row,
					alignItems    = Align.Center
				}
			};


		private void BindDefaultValueControl(VisualElement element, int index) {
			var parameter = _parametersList[index];
			element.Clear();

			var control = parameter.type switch {
				ParameterType.Bool                                                                                                                                        => CreateBoolControl(parameter, index),
				ParameterType.Byte or ParameterType.Short or ParameterType.UShort or ParameterType.Int or ParameterType.UInt or ParameterType.Long or ParameterType.ULong => CreateIntegerControl(parameter, index),
				ParameterType.Float or ParameterType.Double                                                                                                               => CreateFloatControl(parameter, index),
				ParameterType.String                                                                                                                                      => CreateStringControl(parameter, index),
				ParameterType.Vector3                                                                                                                                     => CreateVector3Control(parameter, index),
				ParameterType.Quaternion                                                                                                                                  => CreateQuaternionControl(parameter, index),
				ParameterType.ByteArray                                                                                                                                   => CreateByteArrayControl(parameter, index),
				_                                                                                                                                                         => CreateStringControl(parameter, index) // Fallback vers string
			};

			element.Add(control);
		}

		private VisualElement CreateBoolControl(ParameterEntry parameter, int index) {
			var toggle = new Toggle {
				style = { flexGrow = 1 }
			};

			try {
				toggle.value = parameter.defaultValue?.Length > 0
					&& parameter.GetDefaultValue<bool>();
			} catch {
				toggle.value = false;
			}

			toggle.userData = index;
			toggle.RegisterValueChangedCallback(OnBoolValueChanged);
			return toggle;
		}

		private VisualElement CreateIntegerControl(ParameterEntry parameter, int index) {
			var field = new LongField {
				style = { flexGrow = 1 }
			};
			try {
				var value = parameter.type switch {
					ParameterType.Byte   => parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<byte>() : 0,
					ParameterType.Short  => parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<short>() : 0,
					ParameterType.UShort => parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<ushort>() : 0,
					ParameterType.Int    => parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<int>() : 0,
					ParameterType.UInt   => parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<uint>() : 0,
					ParameterType.Long   => parameter.defaultValue?.Length > 0 ? parameter.GetDefaultValue<long>() : 0,
					ParameterType.ULong  => parameter.defaultValue?.Length > 0 ? (long)parameter.GetDefaultValue<ulong>() : 0,
					_                    => 0
				};
				field.value = value;
			} catch {
				field.value = 0;
			}

			field.userData = index;
			field.RegisterValueChangedCallback(OnIntegerValueChanged);
			return field;
		}

		private VisualElement CreateFloatControl(ParameterEntry parameter, int index) {
			VisualElement field;
			
			if (parameter.type == ParameterType.Float) {
				var floatField = new FloatField {
					style = { flexGrow = 1 }
				};
				
				try {
					floatField.value = parameter.defaultValue?.Length > 0 
						? parameter.GetDefaultValue<float>() 
						: 0.0f;
				} catch {
					floatField.value = 0.0f;
				}
				
				floatField.userData = index;
				floatField.RegisterValueChangedCallback(evt => {
					if (evt.target is not FloatField { userData: int idx } || idx >= _parametersList.Count) return;
					_parametersList[idx].SetDefaultValue(evt.newValue);
					SaveChangesInternal();
				});
				field = floatField;
			} else {
				var doubleField = new DoubleField {
					style = { flexGrow = 1 }
				};
				
				try {
					doubleField.value = parameter.defaultValue?.Length > 0 
						? parameter.GetDefaultValue<double>() 
						: 0.0;
				} catch {
					doubleField.value = 0.0;
				}
				
				doubleField.userData = index;
				doubleField.RegisterValueChangedCallback(evt => {
					if (evt.target is not DoubleField { userData: int idx } || idx >= _parametersList.Count) return;
					_parametersList[idx].SetDefaultValue(evt.newValue);
					SaveChangesInternal();
				});
				field = doubleField;
			}

			return field;
		}

		private VisualElement CreateStringControl(ParameterEntry parameter, int index) {
			var field = new TextField {
				style = { flexGrow = 1 }
			};

			try {
				field.value = parameter.defaultValue?.Length > 0
					? parameter.GetDefaultValue<string>()
					: "";
			} catch {
				field.value = "";
			}

			field.userData = index;
			field.RegisterValueChangedCallback(OnStringValueChanged);
			return field;
		}

		private VisualElement CreateVector3Control(ParameterEntry parameter, int index) {
			var field = new Vector3Field {
				style = { flexGrow = 1 }
			};

			try {
				field.value = parameter.defaultValue?.Length > 0
					? parameter.GetDefaultValue<Vector3>()
					: Vector3.zero;
			} catch {
				field.value = Vector3.zero;
			}

			field.userData = index;
			field.RegisterValueChangedCallback(OnVector3ValueChanged);
			return field;
		}

		private VisualElement CreateQuaternionControl(ParameterEntry parameter, int index) {
			var field = new Vector3Field {
				style = { flexGrow = 1 },
			};

			try {
				var quat = parameter.defaultValue?.Length > 0
					? parameter.GetDefaultValue<Quaternion>()
					: Quaternion.identity;
				field.value = quat.eulerAngles;
			} catch {
				field.value = Vector3.zero;
			}

			field.userData = index;
			field.RegisterValueChangedCallback(OnQuaternionValueChanged);
			return field;
		}

		private VisualElement CreateByteArrayControl(ParameterEntry parameter, int index) {
			var field = new TextField {
				multiline = true,
				style     = { flexGrow = 1 }
			};

			try {
				field.value = parameter.defaultValue?.Length > 0
					? Convert.ToBase64String(parameter.defaultValue)
					: "";
			} catch {
				field.value = "";
			}

			field.userData = index;
			field.RegisterValueChangedCallback(OnByteArrayValueChanged);
			return field;
		}
	}
}
#endif
