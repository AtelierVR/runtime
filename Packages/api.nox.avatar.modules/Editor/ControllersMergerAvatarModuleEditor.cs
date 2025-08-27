#if UNITY_EDITOR
using AnimationControllers;
using Nox.Avatars;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.avatar.modules {
	[CustomEditor(typeof(ControllersMergerAvatarModule))]
	public class ControllersMergerAvatarModuleEditor : Editor {
		private VisualElement _root;

		private PropertyField _globalParameters;
		private PropertyField _controllers;
		private Button        _compile;

		private ControllersMergerAvatarModule module
			=> target as ControllersMergerAvatarModule;

		private IAvatarDescriptor GetDescriptor()
			=> module.GetComponentInParent<IAvatarDescriptor>();

		public override VisualElement CreateInspectorGUI() {
			// Charger le fichier UXML principal
			var visualTree = Resources.Load<VisualTreeAsset>("ControllersMergerAvatarModule");
			if (!visualTree) {
				Logger.LogError("Could not load ControllersMergerAvatarModule.uxml from Resources folder");
				return new Label("Error: Could not load UI template");
			}

			_root = visualTree.CloneTree();

			_globalParameters = _root.Q<PropertyField>("globalParameters");
			_controllers      = _root.Q<PropertyField>("controllers");
			_globalParameters.BindProperty(serializedObject.FindProperty("globalParameters"));
			_controllers.BindProperty(serializedObject.FindProperty("controllers"));
			_compile = _root.Q<Button>("compile");
			_compile.clicked += () => {
				module.SetDescriptor(GetDescriptor());
				Logger.LogDebug("Compiling ControllersMergerAvatarModule");
				module.Compile();
				EditorUtility.SetDirty(module);
				AssetDatabase.SaveAssets();
			};

			return _root;
		}
	}
}
#endif