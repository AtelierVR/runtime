#if UNITY_EDITOR
using Nox.CCK.Avatars.Playable;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;

namespace api.nox.avatar.modules {
	[UnityEditor.CustomEditor(typeof(PlayableAvatarModule))]
	public class PlayableAvatarModuleEditor : Editor {
		public PlayableAvatarModule Module
			=> (PlayableAvatarModule)target;

		public override void OnInspectorGUI() {
			EditorGUILayout.PropertyField(serializedObject.FindProperty("controllers"));

			if (!Application.isPlaying) {
				EditorGUILayout.HelpBox("Playable Avatar Module only works in Play Mode.", MessageType.Info);
				return;
			}

			var desc     = Module.Descriptor;
			var animator = desc?.GetAnimator();

			if (!animator) {
				EditorGUILayout.HelpBox("Animator is not available.", MessageType.Warning);
				return;
			}

			// Affichage des Controller Playables
			if (Module.ControllerPlayables is { Length: > 0 }) {
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Controller Playables", EditorStyles.boldLabel);

				for (var ctrlIndex = 0; ctrlIndex < Module.ControllerPlayables.Length; ctrlIndex++) {
					var controllerPlayable = Module.ControllerPlayables[ctrlIndex];

					if (!controllerPlayable.IsValid()) continue;

					EditorGUILayout.BeginVertical(GUI.skin.box);
					EditorGUILayout.LabelField($"Controller {ctrlIndex}", EditorStyles.boldLabel);

					// Afficher les layers de ce controller playable
					var layerCount = controllerPlayable.GetLayerCount();

					for (var layerIndex = 0; layerIndex < layerCount; layerIndex++) {
						EditorGUILayout.BeginHorizontal();

						// Nom du layer
						string layerName = $"Layer {layerIndex}";
						try {
							var layerInfo = controllerPlayable.GetLayerName(layerIndex);
							if (!string.IsNullOrEmpty(layerInfo)) {
								layerName = layerInfo;
							}
						} catch {
							// Garder le nom par défaut si erreur
						}

						EditorGUILayout.LabelField(layerName, GUILayout.Width(120));

						// Weight du layer
						float weight = controllerPlayable.GetLayerWeight(layerIndex);
						EditorGUILayout.LabelField($"Weight: {weight:F3}", GUILayout.Width(100));

						// État actuel du layer
						var    currentState = controllerPlayable.GetCurrentAnimatorStateInfo(layerIndex);
						string stateName    = "Unknown";

						if (currentState.shortNameHash != 0) {
							try {
								// Utiliser le controller depuis Module.controllers
								var controller = ctrlIndex < Module.controllers.Length
									? Module.controllers[ctrlIndex]
									: null;

								if (controller) {
									// Parcourir les clips pour trouver celui qui correspond
									foreach (var clip in controller.animationClips) {
										if (Animator.StringToHash(clip.name) != currentState.shortNameHash) continue;
										stateName = clip.name;
										break;
									}

									// Si pas trouvé dans les clips, essayer les états de la state machine
									if (stateName == "Unknown" && controller is AnimatorController editorController) {
										var layers = editorController.layers;
										if (layerIndex < layers.Length) {
											var stateMachine = layers[layerIndex].stateMachine;
											foreach (var state in stateMachine.states) {
												if (state.state.nameHash != currentState.shortNameHash) continue;
												stateName = state.state.name;
												break;
											}
										}
									}
								}

								// Fallback: afficher le hash si rien trouvé
								if (stateName == "Unknown") {
									stateName = $"Hash: {currentState.shortNameHash}";
								}
							} catch {
								stateName = $"Hash: {currentState.shortNameHash}";
							}
						}

						EditorGUILayout.LabelField($"State: {stateName}", GUILayout.Width(150));
						EditorGUILayout.LabelField($"Time: {currentState.normalizedTime:F3}", GUILayout.Width(100));

						EditorGUILayout.EndHorizontal();
					}

					EditorGUILayout.EndVertical();
					EditorGUILayout.Space();
				}
			} else {
				EditorGUILayout.HelpBox("No Controller Playables available.", MessageType.Info);
			}

			// Forcer le repaint pour mise à jour en temps réel
			if (Application.isPlaying)
				Repaint();
		}
	}
}
#endif