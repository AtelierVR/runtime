using UnityEngine;
using Nox.CCK.Utils;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Extensions pour RiggingAvatarModule pour le contrôle des contraintes de tête
	/// </summary>
	public static class RiggingAvatarModuleExtensions {
		/// <summary>
		/// Bascule entre les modes de contrainte de tête : AIM (LookAt) et TwoBoneIK
		/// </summary>
		/// <param name="module">Le module de rigging</param>
		/// <param name="useLookAt">True pour utiliser AIM/LookAt, False pour TwoBoneIK</param>
		public static void SwitchHeadConstraintMode(this RiggingAvatarModule module, bool useLookAt) {
			module.useHeadLookAt = useLookAt;
			module.useHeadTwoBoneIK = !useLookAt; // Synchroniser les deux variables
			
			var rigBuilder = module.GetRigBuilder();
			if (!rigBuilder) return;
			
			var rigContainer = IKRigGenerator.CreateOrGetRigContainer(module.Anchor.transform);
			
			// Utiliser la méthode existante de IKRigGenerator pour switcher les poids
			IKRigGenerator.SwitchHeadConstraintMode(rigContainer, !module.useHeadLookAt);
			
			Logger.Log($"Switched head constraint to {(module.useHeadLookAt ? "AIM/LookAt" : "TwoBoneIK")} mode");
		}

		/// <summary>
		/// Toggle le mode de contrainte de tête
		/// </summary>
		/// <param name="module">Le module de rigging</param>
		public static void ToggleHeadConstraintMode(this RiggingAvatarModule module) {
			module.SwitchHeadConstraintMode(!module.useHeadLookAt);
		}

		/// <summary>
		/// Propriété pour obtenir le mode actuel de contrainte de tête
		/// </summary>
		/// <param name="module">Le module de rigging</param>
		/// <returns>True si en mode LookAt/AIM, False si en mode TwoBoneIK</returns>
		public static bool IsUsingHeadLookAt(this RiggingAvatarModule module) {
			return module.useHeadLookAt;
		}
	}

	/// <summary>
	/// Partial class pour ajouter OnValidate au RiggingAvatarModule
	/// </summary>
	public partial class RiggingAvatarModuleValidator : MonoBehaviour {
		// Variables pour détecter les changements dans OnValidate
		private bool _previousUseHeadLookAt;
		private bool _previousUseHeadTwoBoneIK;
		private bool _initialized = false;

		/// <summary>
		/// Appelé automatiquement par Unity quand les valeurs changent dans l'inspecteur
		/// </summary>
		private void OnValidate() {
			var module = GetComponent<RiggingAvatarModule>();
			if (!module) return;

			// Initialiser les valeurs précédentes au premier appel
			if (!_initialized) {
				_previousUseHeadLookAt = module.useHeadLookAt;
				_previousUseHeadTwoBoneIK = module.useHeadTwoBoneIK;
				_initialized = true;
				return;
			}

			// Vérifier si les variables ont changé
			bool headModeChanged = _previousUseHeadLookAt != module.useHeadLookAt || 
			                      _previousUseHeadTwoBoneIK != module.useHeadTwoBoneIK;
			
			if (headModeChanged) {
				// Synchroniser les variables (useHeadLookAt a la priorité)
				if (_previousUseHeadLookAt != module.useHeadLookAt) {
					module.useHeadTwoBoneIK = !module.useHeadLookAt;
				} else if (_previousUseHeadTwoBoneIK != module.useHeadTwoBoneIK) {
					module.useHeadLookAt = !module.useHeadTwoBoneIK;
				}
				
				// Appliquer le switch si on est en mode play
				if (Application.isPlaying) {
					SwitchHeadConstraintModeInternal(module);
				}
			}
			
			// Sauvegarder les valeurs actuelles
			_previousUseHeadLookAt = module.useHeadLookAt;
			_previousUseHeadTwoBoneIK = module.useHeadTwoBoneIK;
		}

		/// <summary>
		/// Méthode interne pour switcher le mode de contrainte de tête
		/// </summary>
		private void SwitchHeadConstraintModeInternal(RiggingAvatarModule module) {
			var rigBuilder = module.GetRigBuilder();
			if (!rigBuilder) return;
			
			var rigContainer = IKRigGenerator.CreateOrGetRigContainer(module.Anchor.transform);
			
			// Utiliser la méthode existante de IKRigGenerator pour switcher les poids
			IKRigGenerator.SwitchHeadConstraintMode(rigContainer, module.useHeadTwoBoneIK);
			
			Logger.Log($"OnValidate: Switched head constraint to {(module.useHeadLookAt ? "AIM/LookAt" : "TwoBoneIK")} mode");
		}
	}
}
