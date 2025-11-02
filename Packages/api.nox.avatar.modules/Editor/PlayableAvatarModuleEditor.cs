#if UNITY_EDITOR
using System.Collections.Generic;
using Nox.CCK.Avatars.Playable;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace api.nox.avatar.modules {
	[CustomEditor(typeof(PlayableAvatarModule))]
	public class PlayableAvatarModuleEditor : Editor {
		private PlayableAvatarModule _module;
		private VisualElement        _root;
		private Label                _infoLabel;
		private VisualElement        _playablesContainer;
		private PropertyField        _controllersProperty;

		private Dictionary<string, LayerTracker> _layerTrackers = new();
		private bool                             _isPlaying;

		// Structure pour tracker les valeurs des layers
		private class LayerTracker {
			public Label  WeightLabel;
			public Label  StateLabel;
			public float  LastWeight;
			public int    LastStateHash;
		}

		public override VisualElement CreateInspectorGUI() {
			_module = (PlayableAvatarModule)target;

			// Charger le UXML
			var visualTree = Resources.Load<VisualTreeAsset>("PlayableAvatarModuleEditor");
			
			if (visualTree != null) {
				_root = visualTree.CloneTree();
				
				// Récupérer les éléments
				_controllersProperty = _root.Q<PropertyField>("controllers-property");
				_infoLabel           = _root.Q<Label>("info-label");
				_playablesContainer  = _root.Q<VisualElement>("playables-container");
			}
			else {
				// Fallback: créer l'interface manuellement si UXML non disponible
				_root = CreateManualUI();
			}

			// Bind la propriété
			_controllersProperty.BindProperty(serializedObject.FindProperty("controllers"));

			// Mettre à jour l'affichage initial
			UpdateRuntimeSection();

			// Planifier les mises à jour en mode Play
			if (Application.isPlaying) {
				_isPlaying = true;
				_root.schedule.Execute(UpdatePlayableValues).Every(100); // Update toutes les 100ms
			}

			// S'abonner aux changements de mode Play
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			return _root;
		}

		private VisualElement CreateManualUI() {
			var root = new VisualElement();
			root.style.flexGrow = 1;

			// Header
			var header = new VisualElement();
			header.style.backgroundColor = new Color(0, 0, 0, 0.1f);
			header.style.paddingTop = header.style.paddingBottom = header.style.paddingLeft = header.style.paddingRight = 10;
			header.style.marginBottom = 10;
			header.style.borderTopLeftRadius = header.style.borderTopRightRadius = 
				header.style.borderBottomLeftRadius = header.style.borderBottomRightRadius = 5;

			var headerLabel = new Label("Playable Avatar Module");
			headerLabel.style.fontSize = 14;
			headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			headerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
			header.Add(headerLabel);
			root.Add(header);
			
			// add mini text for warn about manual ui
			var warnLabel = new Label("UXML resource not found. Using manual UI layout.");
			warnLabel.style.fontSize = 10;
			warnLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
			warnLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
			warnLabel.style.marginBottom = 10;
			root.Add(warnLabel);

			// Content container
			var content = new VisualElement();
			content.style.paddingTop = content.style.paddingBottom = content.style.paddingLeft = content.style.paddingRight = 10;

			// Controllers property
			_controllersProperty = new PropertyField();
			_controllersProperty.name = "controllers-property";
			_controllersProperty.label = "Controllers";
			content.Add(_controllersProperty);

			// Runtime section
			var runtimeSection = new VisualElement();
			runtimeSection.style.marginTop = 10;

			// Info label
			_infoLabel = new Label();
			_infoLabel.style.backgroundColor = new Color(0.39f, 0.59f, 1f, 0.1f);
			_infoLabel.style.borderTopColor = _infoLabel.style.borderBottomColor = 
				_infoLabel.style.borderLeftColor = _infoLabel.style.borderRightColor = new Color(0.39f, 0.59f, 1f, 0.5f);
			_infoLabel.style.borderTopWidth = _infoLabel.style.borderBottomWidth = 
				_infoLabel.style.borderLeftWidth = _infoLabel.style.borderRightWidth = 1;
			_infoLabel.style.borderTopLeftRadius = _infoLabel.style.borderTopRightRadius = 
				_infoLabel.style.borderBottomLeftRadius = _infoLabel.style.borderBottomRightRadius = 3;
			_infoLabel.style.paddingTop = _infoLabel.style.paddingBottom = _infoLabel.style.paddingLeft = _infoLabel.style.paddingRight = 8;
			_infoLabel.style.marginTop = _infoLabel.style.marginBottom = 5;
			runtimeSection.Add(_infoLabel);

			// Playables container
			_playablesContainer = new ScrollView();
			_playablesContainer.name = "playables-container";
			_playablesContainer.style.marginTop = 10;
			runtimeSection.Add(_playablesContainer);

			content.Add(runtimeSection);
			root.Add(content);

			return root;
		}

		private void OnDisable() {
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state) {
			if (state == PlayModeStateChange.EnteredPlayMode) {
				_isPlaying = true;
				UpdateRuntimeSection();
				_root?.schedule.Execute(UpdatePlayableValues).Every(100);
			}
			else if (state == PlayModeStateChange.ExitingPlayMode) {
				_isPlaying = false;
				_layerTrackers.Clear();
				UpdateRuntimeSection();
			}
		}

		private void UpdateRuntimeSection() {
			if (!Application.isPlaying) {
				_infoLabel.text              = "Entrez en mode Play pour voir les Controller Playables.";
				_infoLabel.style.display     = DisplayStyle.Flex;
				_playablesContainer.Clear();
				_layerTrackers.Clear();
				return;
			}

			var desc     = _module.Descriptor;
			var animator = desc?.GetAnimator();

			if (!animator) {
				_infoLabel.text          = "Aucun animateur disponible.";
				_infoLabel.style.display = DisplayStyle.Flex;
				_playablesContainer.Clear();
				return;
			}

			if (_module.ControllerPlayables is not { Length: > 0 }) {
				_infoLabel.text          = "Aucun Controller Playable disponible.";
				_infoLabel.style.display = DisplayStyle.Flex;
				_playablesContainer.Clear();
				return;
			}

			_infoLabel.text          = $"Controller Playables: {_module.ControllerPlayables.Length}";
			_infoLabel.style.display = DisplayStyle.Flex;

			// Créer l'interface pour chaque controller
			_playablesContainer.Clear();
			_layerTrackers.Clear();

			for (var ctrlIndex = 0; ctrlIndex < _module.ControllerPlayables.Length; ctrlIndex++) {
				var controllerPlayable = _module.ControllerPlayables[ctrlIndex];
				if (!controllerPlayable.IsValid())
					continue;

				CreateControllerBox(controllerPlayable, ctrlIndex);
			}
		}

		private void CreateControllerBox(AnimatorControllerPlayable controllerPlayable, int ctrlIndex) {
			var controllerBox = new VisualElement();
			controllerBox.AddToClassList("controller-box");

			// Récupérer le nom du controller
			var controllerName = "Unknown";
			if (ctrlIndex < _module.controllers.Length && _module.controllers[ctrlIndex] != null) {
				controllerName = _module.controllers[ctrlIndex].name;
			}

			var header = new Label($"#{ctrlIndex} {controllerName}");
			header.AddToClassList("controller-header");
			controllerBox.Add(header);

			var layerCount = controllerPlayable.GetLayerCount();

			for (var layerIndex = 0; layerIndex < layerCount; layerIndex++)
				CreateLayerRow(controllerBox, controllerPlayable, ctrlIndex, layerIndex);

			_playablesContainer.Add(controllerBox);
		}

		private void CreateLayerRow(VisualElement parent, AnimatorControllerPlayable controllerPlayable, int ctrlIndex, int layerIndex) {
			var row = new VisualElement();
			row.AddToClassList("layer-row");

			// Nom du layer
			string layerName = $"Layer {layerIndex}";
			try {
				var layerInfo = controllerPlayable.GetLayerName(layerIndex);
				if (!string.IsNullOrEmpty(layerInfo))
					layerName = layerInfo;
			}
			catch {
				// Garder le nom par défaut si erreur
			}

			var nameLabel = new Label(layerName);
			nameLabel.AddToClassList("layer-name");
			row.Add(nameLabel);

			// State
			var currentState = controllerPlayable.GetCurrentAnimatorStateInfo(layerIndex);
			var stateName    = GetStateName(currentState, ctrlIndex, layerIndex);
			var stateLabel   = new Label(stateName);
			stateLabel.AddToClassList("layer-state");
			row.Add(stateLabel);

			// Weight
			float weight      = controllerPlayable.GetLayerWeight(layerIndex);
			var   weightLabel = new Label($"{weight:F3}");
			weightLabel.AddToClassList("layer-weight");
			row.Add(weightLabel);

			parent.Add(row);

			// Tracker pour ce layer
			var trackerKey = $"{ctrlIndex}_{layerIndex}";
			_layerTrackers[trackerKey] = new LayerTracker {
				WeightLabel   = weightLabel,
				StateLabel    = stateLabel,
				LastWeight    = weight,
				LastStateHash = currentState.shortNameHash
			};
		}

		private string GetStateName(AnimatorStateInfo currentState, int ctrlIndex, int layerIndex) {
			if (currentState.shortNameHash == 0)
				return "None";

			var stateName = "Unknown";

			try {
				var controller = ctrlIndex < _module.controllers.Length ? _module.controllers[ctrlIndex] : null;

				if (controller) {
					// Chercher dans les clips
					foreach (var clip in controller.animationClips) {
						if (Animator.StringToHash(clip.name) == currentState.shortNameHash) {
							stateName = clip.name;
							return stateName;
						}
					}

					// Chercher dans les états de la state machine
					if (controller is AnimatorController editorController) {
						var layers = editorController.layers;
						if (layerIndex < layers.Length) {
							var stateMachine = layers[layerIndex].stateMachine;
							foreach (var state in stateMachine.states) {
								if (state.state.nameHash == currentState.shortNameHash) {
									stateName = state.state.name;
									return stateName;
								}
							}
						}
					}
				}

				// Fallback: afficher le hash
				stateName = $"Hash: {currentState.shortNameHash}";
			}
			catch {
				stateName = $"Hash: {currentState.shortNameHash}";
			}

			return stateName;
		}

		private void UpdatePlayableValues() {
			if (!_isPlaying || _module.ControllerPlayables == null || _layerTrackers.Count == 0)
				return;

			for (var ctrlIndex = 0; ctrlIndex < _module.ControllerPlayables.Length; ctrlIndex++) {
				var controllerPlayable = _module.ControllerPlayables[ctrlIndex];

				var layerCount = controllerPlayable.GetLayerCount();

				for (var layerIndex = 0; layerIndex < layerCount; layerIndex++) {
					var trackerKey = $"{ctrlIndex}_{layerIndex}";
					if (!_layerTrackers.TryGetValue(trackerKey, out var tracker))
						continue;

					// Récupérer les valeurs actuelles
					float weight       = controllerPlayable.GetLayerWeight(layerIndex);
					var   currentState = controllerPlayable.GetCurrentAnimatorStateInfo(layerIndex);
					int   stateHash    = currentState.shortNameHash;

					// Mettre à jour uniquement ce qui a changé
					if (System.Math.Abs(tracker.LastWeight - weight) > 0.001f) {
						tracker.WeightLabel.text = $"{weight:F3}";
						tracker.LastWeight       = weight;
					}

					if (tracker.LastStateHash != stateHash) {
						var stateName           = GetStateName(currentState, ctrlIndex, layerIndex);
						tracker.StateLabel.text = stateName;
						tracker.LastStateHash   = stateHash;
					}
				}
			}
		}
	}
}
#endif