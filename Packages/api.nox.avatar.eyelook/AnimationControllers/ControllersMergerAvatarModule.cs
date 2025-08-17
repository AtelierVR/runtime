using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.CCK.Build;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Nox.CCK.Utils;
using Logger = Nox.CCK.Utils.Logger;

namespace AnimationControllers {
	public class ControllersMergerAvatarModule : MonoBehaviour, IAvatarModule, ICompilable {
		private IAvatarDescriptor _descriptor;

		public void OnPlay(IAvatarDescriptor descriptor)
			=> SetDescriptor(descriptor);

		public void SetDescriptor(IAvatarDescriptor descriptor)
			=> _descriptor = descriptor;

		public IAvatarDescriptor GetDescriptor()
			=> _descriptor ?? gameObject.GetComponentInParents<IAvatarDescriptor>();

		public AnimatorController[] controllers      = Array.Empty<AnimatorController>();
		public string[]             globalParameters = Array.Empty<string>();

		private static AnimatorController MakeController(AnimatorController controller) {
			var isFile = controller && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(controller));
			if (isFile) {
				var controllerCopy = Instantiate(controller);
				controllerCopy.name = $"{controller.name}_Merged";
				return controllerCopy;
			}

			var newController = new AnimatorController { name = "MergedController" };
			var baseLayer = new AnimatorControllerLayer {
				name          = "Base Layer",
				defaultWeight = 1.0f,
				stateMachine  = new AnimatorStateMachine { name = "Base Layer" }
			};
			newController.AddLayer(baseLayer);

			return newController;
		}

		private Motion ProcessMotion(Motion motion, List<(string, AnimatorControllerParameterType, string)> parameters, int id) {
			if (motion == null) return null;

			if (motion is BlendTree blendTree) {
				// Create a copy of the blend tree
				var newBlendTree = new BlendTree();
				newBlendTree.name                   = blendTree.name;
				newBlendTree.blendType              = blendTree.blendType;
				newBlendTree.useAutomaticThresholds = blendTree.useAutomaticThresholds;
				newBlendTree.minThreshold           = blendTree.minThreshold;
				newBlendTree.maxThreshold           = blendTree.maxThreshold;

				// Rename blend parameters
				if (!string.IsNullOrEmpty(blendTree.blendParameter)) {
					var renamedParam = parameters.FirstOrDefault(p => p.Item1 == blendTree.blendParameter);
					if (renamedParam != default) {
						newBlendTree.blendParameter = renamedParam.Item3;
					} else if (!globalParameters.Contains(blendTree.blendParameter)) {
						// Parameter not found in controller parameters and not global, add prefix
						newBlendTree.blendParameter = $"{id}_{blendTree.blendParameter}";
					} else {
						newBlendTree.blendParameter = blendTree.blendParameter;
					}
				}

				if (!string.IsNullOrEmpty(blendTree.blendParameterY)) {
					var renamedParam = parameters.FirstOrDefault(p => p.Item1 == blendTree.blendParameterY);
					if (renamedParam != default) {
						newBlendTree.blendParameterY = renamedParam.Item3;
					} else if (!globalParameters.Contains(blendTree.blendParameterY)) {
						// Parameter not found in controller parameters and not global, add prefix
						newBlendTree.blendParameterY = $"{id}_{blendTree.blendParameterY}";
					} else {
						newBlendTree.blendParameterY = blendTree.blendParameterY;
					}
				}

				// Process children recursively
				var children = new ChildMotion[blendTree.children.Length];
				for (int i = 0; i < blendTree.children.Length; i++) {
					var child = blendTree.children[i];
					children[i] = new ChildMotion {
						motion               = ProcessMotion(child.motion, parameters, id),
						threshold            = child.threshold,
						position             = child.position,
						timeScale            = child.timeScale,
						cycleOffset          = child.cycleOffset,
						directBlendParameter = child.directBlendParameter
					};
				}

				newBlendTree.children = children;

				return newBlendTree;
			}

			return motion;
		}

		private void CollectBlendTreeParameters(Motion motion, HashSet<string> usedParameters) {
			if (motion == null) return;

			if (motion is BlendTree blendTree) {
				if (!string.IsNullOrEmpty(blendTree.blendParameter))
					usedParameters.Add(blendTree.blendParameter);

				if (!string.IsNullOrEmpty(blendTree.blendParameterY))
					usedParameters.Add(blendTree.blendParameterY);

				// Process children recursively
				foreach (var child in blendTree.children) {
					CollectBlendTreeParameters(child.motion, usedParameters);
				}
			}
		}

		private AnimatorController AddController(AnimatorController controller, AnimatorController toAdd) {
			if (!toAdd) return controller;
			if (!controller)
				return toAdd;

			var id = toAdd.GetInstanceID();

			// get the list of parameters from the controller and check if they already exist in the controller
			// check if is in the list of global parameters, if not, rename as "Merged_<id>_<n>"
			// base name, base type, renamed name
			var parameters = new List<(string, AnimatorControllerParameterType, string)>();
			foreach (var parameter in toAdd.parameters) {
				var s = parameter.name;
				if (!globalParameters.Contains(s))
					s = $"{id}_{s}";
				parameters.Add((parameter.name, parameter.type, s));
			}

			// Collect all parameters used by blend trees (even if not defined as controller parameters)
			var usedBlendTreeParameters = new HashSet<string>();
			foreach (var layer in toAdd.layers) {
				foreach (var state in layer.stateMachine.states) {
					CollectBlendTreeParameters(state.state.motion, usedBlendTreeParameters);
				}
			}

			// Add missing blend tree parameters that aren't already in the parameters list
			foreach (var blendParam in usedBlendTreeParameters) {
				if (!parameters.Any(p => p.Item1 == blendParam) && !globalParameters.Contains(blendParam)) {
					// Assume Float type for blend tree parameters (most common)
					var renamedName = $"{id}_{blendParam}";
					parameters.Add((blendParam, AnimatorControllerParameterType.Float, renamedName));
				}
			}

			// add the parameters to the controller
			foreach (var (_, type, renamedName) in parameters) {
				if (controller.parameters.Any(p => p.name == renamedName)) continue;
				controller.AddParameter(renamedName, type);
			}

			// get all layers from toAdd, and add them to the controller like this: <id>_<layer.name>
			// do not missing to rename parameters in the layers
			var first = true;
			foreach (var layer in toAdd.layers) {
				var newLayer = new AnimatorControllerLayer {
					name                     = $"{id}_{layer.name}",
					defaultWeight            = first ? 1.0f : layer.defaultWeight,
					stateMachine             = new AnimatorStateMachine { name = $"{id}_{layer.stateMachine.name}" },
					avatarMask               = layer.avatarMask,
					blendingMode             = layer.blendingMode,
					syncedLayerIndex         = layer.syncedLayerIndex,
					iKPass                   = layer.iKPass,
					syncedLayerAffectsTiming = layer.syncedLayerAffectsTiming
				};
				first = false;

				// Create a mapping of original state names to new states for transition creation
				var stateMapping = new Dictionary<string, AnimatorState>();
				foreach (var state in layer.stateMachine.states) {
					var newState = newLayer.stateMachine.AddState($"{id}_{state.state.name}");
					newState.motion              = ProcessMotion(state.state.motion, parameters, id);
					newState.writeDefaultValues  = state.state.writeDefaultValues;
					newState.speed               = state.state.speed;
					newState.timeParameter       = state.state.timeParameter;
					newState.timeParameterActive = state.state.timeParameterActive;
					newState.tag                 = state.state.tag;
					newState.behaviours = state.state.behaviours.Select(
							b => {
								var behaviour = (StateMachineBehaviour)Activator.CreateInstance(b.GetType());
								// Copy properties from the original behaviour to the new one
								foreach (var prop in b.GetType().GetProperties())
									if (prop.CanWrite)
										prop.SetValue(behaviour, prop.GetValue(b));
								return behaviour;
							}
						)
						.ToArray();

					stateMapping[state.state.name] = newState;
				}

				// Add state transitions
				foreach (var stateInfo in layer.stateMachine.states) {
					var sourceState = stateMapping[stateInfo.state.name];

					foreach (var transition in stateInfo.state.transitions) {
						AnimatorStateTransition newTransition;

						if (transition.destinationState != null) {
							// Transition to another state
							var destinationState = stateMapping[transition.destinationState.name];
							newTransition = sourceState.AddTransition(destinationState);
						} else {
							// Exit transition
							newTransition = sourceState.AddExitTransition();
						}

						// Copy transition properties
						newTransition.duration            = transition.duration;
						newTransition.hasExitTime         = transition.hasExitTime;
						newTransition.exitTime            = transition.exitTime;
						newTransition.offset              = transition.offset;
						newTransition.interruptionSource  = transition.interruptionSource;
						newTransition.orderedInterruption = transition.orderedInterruption;
						newTransition.canTransitionToSelf = transition.canTransitionToSelf;

						// Copy and rename conditions
						newTransition.conditions = transition.conditions.Select(
								c => {
									var parameterName = c.parameter;
									// Find the renamed parameter
									var renamedParam = parameters.FirstOrDefault(p => p.Item1 == parameterName);
									if (renamedParam != default)
										parameterName = renamedParam.Item3;

									return new AnimatorCondition {
										mode      = c.mode,
										parameter = parameterName,
										threshold = c.threshold
									};
								}
							)
							.ToArray();
					}
				}

				// Add any state transitions
				foreach (var transition in layer.stateMachine.anyStateTransitions) {
					if (transition.destinationState == null) continue;

					var destinationState = stateMapping.ContainsKey(transition.destinationState.name)
						? stateMapping[transition.destinationState.name]
						: null;

					if (destinationState == null) continue;

					var newTransition = newLayer.stateMachine.AddAnyStateTransition(destinationState);
					newTransition.duration            = transition.duration;
					newTransition.hasExitTime         = transition.hasExitTime;
					newTransition.exitTime            = transition.exitTime;
					newTransition.offset              = transition.offset;
					newTransition.interruptionSource  = transition.interruptionSource;
					newTransition.orderedInterruption = transition.orderedInterruption;
					newTransition.canTransitionToSelf = transition.canTransitionToSelf;

					// Copy and rename conditions
					newTransition.conditions = transition.conditions.Select(
							c => {
								var parameterName = c.parameter;
								// Find the renamed parameter
								var renamedParam = parameters.FirstOrDefault(p => p.Item1 == parameterName);
								if (renamedParam != default)
									parameterName = renamedParam.Item3;

								return new AnimatorCondition {
									mode      = c.mode,
									parameter = parameterName,
									threshold = c.threshold
								};
							}
						)
						.ToArray();
				}

				// Add entry transitions
				foreach (var transition in layer.stateMachine.entryTransitions) {
					if (transition.destinationState == null) continue;

					var destinationState = stateMapping.ContainsKey(transition.destinationState.name)
						? stateMapping[transition.destinationState.name]
						: null;

					if (destinationState == null) continue;

					var newTransition = newLayer.stateMachine.AddEntryTransition(destinationState);

					// Copy and rename conditions
					newTransition.conditions = transition.conditions.Select(
							c => {
								var parameterName = c.parameter;
								// Find the renamed parameter
								var renamedParam = parameters.FirstOrDefault(p => p.Item1 == parameterName);
								if (renamedParam != default)
									parameterName = renamedParam.Item3;

								return new AnimatorCondition {
									mode      = c.mode,
									parameter = parameterName,
									threshold = c.threshold
								};
							}
						)
						.ToArray();
				}

				// Set default state if it exists
				if (layer.stateMachine.defaultState != null && stateMapping.ContainsKey(layer.stateMachine.defaultState.name)) {
					newLayer.stateMachine.defaultState = stateMapping[layer.stateMachine.defaultState.name];
				}

				controller.AddLayer(newLayer);
			}

			// add the layers to the controller
			return controller;
		}

		private static AnimatorController SaveAsFile(AnimatorController controller) {
			if (!controller) return null;
			var path = AssetDatabase.GetAssetPath(controller);
			if (string.IsNullOrEmpty(path)) {
				path = $"Assets/MergedControllers/{controller.name}.controller";
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? string.Empty);
				AssetDatabase.CreateAsset(controller, path);
			} else EditorUtility.SetDirty(controller);

			return controller;
		}

		public void Compile() {
			if (controllers.Length == 0) {
				Logger.LogWarning("ControllersMergerAvatarModule: No controllers to merge");
				return;
			}

			var animator = _descriptor?.GetAnimator();
			if (!animator) {
				Logger.LogError("ControllersMergerAvatarModule: No Animator found on descriptor");
				return;
			}

			var workingController = animator.runtimeAnimatorController as AnimatorController;
			workingController = MakeController(workingController);
			foreach (var controller in controllers)
				workingController = AddController(workingController, controller);

			workingController                  = SaveAsFile(workingController);
			animator.runtimeAnimatorController = workingController;

			EditorUtility.SetDirty(animator);
			AssetDatabase.SaveAssets();
			Logger.LogDebug("ControllersMergerAvatarModule: Controllers merged successfully");
		}
	}
}