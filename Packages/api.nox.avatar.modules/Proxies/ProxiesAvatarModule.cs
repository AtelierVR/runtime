using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Proxies {
	public class ProxiesAvatarModule : MonoBehaviour, IAvatarModule {
		public ReplaceClip[] clips;

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			if (clips == null || clips.Length == 0) {
				Logger.LogWarning("Aucun clip de remplacement spécifié");
				return true;
			}

			var animator      = runtimeAvatar.GetDescriptor().GetAnimator();
			var playableGraph = animator.playableGraph;

			try {
				// Créer un dictionnaire pour un accès rapide aux remplacements
				var replacementDict = clips
					.ToDictionary(clip => clip.original, clip => clip.replacement);

				await UniTask.Yield();

				// Remplacer les clips dans le PlayableGraph
				ReplaceClipsInPlayableGraph(playableGraph, replacementDict);

				Logger.Log($"Remplacement de {clips.Length} clips d'animation effectué avec succès");
				return true;
			} catch (Exception ex) {
				Logger.LogError($"Erreur lors du remplacement des clips: {ex.Message}");
				return false;
			}
		}

		private void ReplaceClipsInPlayableGraph(PlayableGraph graph, Dictionary<AnimationClip, AnimationClip> replacements) {
			// Parcourir tous les playables dans le graph
			var outputCount = graph.GetOutputCount();

			for (var i = 0; i < outputCount; i++) {
				var output = graph.GetOutput(i);
				if (output.IsOutputValid()) {
					TraverseAndReplaceClips(output.GetSourcePlayable(), replacements, new HashSet<Playable>());
				}
			}
		}

		private void TraverseAndReplaceClips(Playable playable, Dictionary<AnimationClip, AnimationClip> replacements, HashSet<Playable> visited) {
			// Éviter les cycles infinis
			if (visited.Contains(playable) || !playable.IsValid()) {
				return;
			}

			visited.Add(playable);

			// Si c'est un AnimationClipPlayable, vérifier s'il faut le remplacer
			if (playable.GetPlayableType() == typeof(AnimationClipPlayable)) {
				var clipPlayable = (AnimationClipPlayable)playable;
				var currentClip  = clipPlayable.GetAnimationClip();

				if (currentClip != null && replacements.TryGetValue(currentClip, out var replacementClip)) {
					if (replacementClip != null) {
						// Créer un nouveau AnimationClipPlayable avec le clip de remplacement
						var newClipPlayable = AnimationClipPlayable.Create(playable.GetGraph(), replacementClip);

						// Copier les propriétés du playable original
						newClipPlayable.SetTime(clipPlayable.GetTime());
						newClipPlayable.SetSpeed(clipPlayable.GetSpeed());
						newClipPlayable.SetDuration(clipPlayable.GetDuration());

						// Remplacer dans les inputs des parents
						ReplacePlayableInParents(playable, newClipPlayable);

						Logger.Log($"Clip remplacé: {currentClip.name} -> {replacementClip.name}");
					}
				}
			}

			// Parcourir récursivement les inputs
			int inputCount = playable.GetInputCount();
			for (int i = 0; i < inputCount; i++) {
				var inputPlayable = playable.GetInput(i);
				TraverseAndReplaceClips(inputPlayable, replacements, visited);
			}
		}

		private void ReplacePlayableInParents(Playable oldPlayable, Playable newPlayable) {
			var graph       = oldPlayable.GetGraph();
			int outputCount = graph.GetOutputCount();

			// Chercher dans tous les outputs du graph
			for (int i = 0; i < outputCount; i++) {
				var output = graph.GetOutput(i);
				if (output.IsOutputValid()) {
					ReplaceInPlayableTree(output.GetSourcePlayable(), oldPlayable, newPlayable, new HashSet<Playable>());
				}
			}
		}

		private void ReplaceInPlayableTree(Playable current, Playable target, Playable replacement, HashSet<Playable> searchVisited) {
			if (searchVisited.Contains(current) || !current.IsValid()) {
				return;
			}

			searchVisited.Add(current);

			// Vérifier les inputs de ce playable
			int inputCount = current.GetInputCount();
			for (int i = 0; i < inputCount; i++) {
				var input = current.GetInput(i);

				if (input.Equals(target)) {
					// Remplacer l'input
					current.DisconnectInput(i);
					current.ConnectInput(i, replacement, 0);
					current.SetInputWeight(i, current.GetInputWeight(i));
				} else {
					// Continuer la recherche récursivement
					ReplaceInPlayableTree(input, target, replacement, searchVisited);
				}
			}
		}

		// Méthode utilitaire pour remplacer un clip spécifique
		public void ReplaceSpecificClip(AnimationClip originalClip, AnimationClip replacementClip, Animator animator) {
			if (originalClip == null || replacementClip == null || animator == null) {
				Logger.LogWarning("Paramètres invalides pour le remplacement de clip");
				return;
			}

			var replacements = new Dictionary<AnimationClip, AnimationClip> {
				{ originalClip, replacementClip }
			};

			ReplaceClipsInPlayableGraph(animator.playableGraph, replacements);
		}

		// Méthode pour remplacer plusieurs clips à la fois
		public void ReplaceMultipleClips(IEnumerable<(AnimationClip original, AnimationClip replacement)> clipPairs, Animator animator) {
			if (clipPairs == null || animator == null) {
				Logger.LogWarning("Paramètres invalides pour le remplacement multiple de clips");
				return;
			}

			var replacements = clipPairs.ToDictionary(pair => pair.original, pair => pair.replacement);
			ReplaceClipsInPlayableGraph(animator.playableGraph, replacements);
		}
	}

	[Serializable]
	public class ReplaceClip {
		public AnimationClip original;
		public AnimationClip replacement;
	}
}