#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.CCK.Avatars.EyeLooks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.avatar.modules {
	[CustomEditor(typeof(EyeLookAvatarModule))]
	public class EyeLookAvatarModuleEditor : Editor {
		private VisualElement _root;
		private ListView      _eyeLookList;
		private ToolbarMenu   _addMenu;

		private EyeLookAvatarModule module
			=> target as EyeLookAvatarModule;

		private IAvatarDescriptor GetDescriptor()
			=> module.GetComponentInParent<IAvatarDescriptor>();

		private static Dictionary<string, Type> GetLookTypes() {
			var types = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(assembly => assembly.GetTypes())
				.Where(type => type.IsSubclassOf(typeof(BaseEyeLook)) && !type.IsAbstract)
				.ToList();
			var lookTypes = new Dictionary<string, Type>();
			foreach (var type in types) {
				var lookName = type.GetCustomAttributes(typeof(TooltipAttribute), false)
					.FirstOrDefault() is TooltipAttribute tooltipAttribute
					? tooltipAttribute.tooltip
					: type.Name;
				lookTypes[lookName] = type;
			}

			return lookTypes;
		}

		public override VisualElement CreateInspectorGUI() {
			// Charger le fichier UXML principal
			var visualTree = Resources.Load<VisualTreeAsset>("EyeLookAvatarModule");
			if (!visualTree) {
				Logger.LogError("Could not load EyeLookAvatarModule.uxml from Resources folder");
				return new Label("Error: Could not load UI template");
			}

			_root        = visualTree.CloneTree();
			_eyeLookList = _root.Q<ListView>("eyelook-list");
			_addMenu     = _root.Q<ToolbarMenu>("add-menu");
			if (_addMenu == null || _eyeLookList == null) {
				Logger.LogError("Could not find required UI elements in UXML template");
				return new Label("Error: UI template is missing required elements");
			}

			_addMenu.text = "Add Eye Look";
			var lookTypes = GetLookTypes();
			foreach (var lookType in lookTypes)
				_addMenu.menu.AppendAction(lookType.Key, (action) => AddEyeLook(lookType.Value));
			_eyeLookList.makeItem     =  MakeEyeLookItem;
			_eyeLookList.bindItem     =  BindEyeLookItem;
			_eyeLookList.itemsSource  =  module.eyeLooks?.ToList() ?? new List<BaseEyeLook>();
			_eyeLookList.itemsRemoved += OnEyeLooksRemoved;
			return _root;
		}

		private VisualElement MakeEyeLookItem() {
			var itemTemplate = Resources.Load<VisualTreeAsset>("EyeLookAvatarModule.ListItem");
			if (!itemTemplate) return new Label("Error: Could not load item template");
			return itemTemplate.CloneTree();
		}

		private void BindEyeLookItem(VisualElement element, int index) {
			if (index >= module.eyeLooks.Length) return;
			var eyeLook = module.eyeLooks[index];
			if (eyeLook == null) return;
			var typeLabel        = element.Q<Label>("type-label");
			var deleteButton     = element.Q<Button>("delete-button");
			var contentContainer = element.Q<VisualElement>("content-container");
			typeLabel.text       =  GetLookTypes().FirstOrDefault(kv => kv.Value == eyeLook.GetType()).Key ?? eyeLook.GetType().Name;
			deleteButton.clicked -= null;
			deleteButton.clicked += () => RemoveEyeLook(index);
			contentContainer.Clear();
			var context = new EyeLookContext {
				Descriptor = GetDescriptor(),
				Module     = module
			};
			var inspectorGUI = eyeLook.CreateInspectorGUI(context);
			if (inspectorGUI == null) return;
			RegisterDirtyCallbacks(inspectorGUI);
			contentContainer.Add(inspectorGUI);
		}

		private void RegisterDirtyCallbacks(VisualElement element) {
			// Recursively register callbacks on all UI elements to mark as dirty
			foreach (var child in element.Children()) {
				switch (child) {
					case Vector4Field vector4Field:
						vector4Field.RegisterValueChangedCallback(_ => EditorUtility.SetDirty(module));
						break;
					case ObjectField objectField:
						objectField.RegisterValueChangedCallback(_ => EditorUtility.SetDirty(module));
						break;
					case TextField textField:
						textField.RegisterValueChangedCallback(_ => EditorUtility.SetDirty(module));
						break;
					case FloatField floatField:
						floatField.RegisterValueChangedCallback(_ => EditorUtility.SetDirty(module));
						break;
					case Slider slider:
						slider.RegisterValueChangedCallback(_ => EditorUtility.SetDirty(module));
						break;
					case MinMaxSlider minMaxSlider:
						minMaxSlider.RegisterValueChangedCallback(_ => EditorUtility.SetDirty(module));
						break;
					case DropdownField dropdownField:
						dropdownField.RegisterValueChangedCallback(_ => EditorUtility.SetDirty(module));
						break;
				}

				// Recursively handle nested elements
				RegisterDirtyCallbacks(child);
			}
		}

		private void AddEyeLook(Type type) {
			if (Activator.CreateInstance(type) is not BaseEyeLook newEyeLook) return;

			var old = module.eyeLooks.ToList();
			old.Add(newEyeLook);
			module.eyeLooks          = old.ToArray();
			_eyeLookList.itemsSource = module.eyeLooks;
			_eyeLookList.RefreshItems();
			EditorUtility.SetDirty(module);
		}

		private void RemoveEyeLook(int index) {
			if (index < 0 || index >= module.eyeLooks.Length) return;
			var oldLooks = module.eyeLooks.ToList();
			if (index >= oldLooks.Count) return;
			oldLooks.RemoveAt(index);
			module.eyeLooks          = oldLooks.ToArray();
			_eyeLookList.itemsSource = module.eyeLooks;
			_eyeLookList.RefreshItems();
			EditorUtility.SetDirty(module);
		}

		private void OnEyeLooksRemoved(IEnumerable<int> indices) {
			foreach (var index in indices.OrderByDescending(i => i))
				if (index >= 0 && index < module.eyeLooks.Length)
					RemoveEyeLook(index);
			_eyeLookList.itemsSource = module.eyeLooks;
			EditorUtility.SetDirty(module);
		}
	}
}
#endif