#if UNITY_EDITOR
using Nox.CCK.Avatars;
using UnityEngine.Events;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.avatar.editor {
	public class AvatarEditorHelper {
		public static AvatarDescriptor CurrentAvatar;

		public static readonly UnityEvent<AvatarDescriptor> OnAvatarSelected = new();

		[InitializeOnLoadMethod]
		private static void Initialize() {
			Selection.selectionChanged         += OnSelectionChanged;
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			OnHierarchyChanged();
		}

		private static void OnSelectionChanged() {
			if (!Selection.activeGameObject) return;
			var avatarDescriptor = Selection.activeGameObject.GetComponent<AvatarDescriptor>();
			if (!avatarDescriptor)
				avatarDescriptor = Selection.activeGameObject.GetComponentInParent<AvatarDescriptor>();
			if (avatarDescriptor && avatarDescriptor != CurrentAvatar)
				SetCurrentAvatar(avatarDescriptor);
		}

		private static void OnHierarchyChanged() {
			try {
				if (CurrentAvatar?.gameObject.activeInHierarchy ?? false) return;
				var activeAvatars = Object.FindObjectsByType<AvatarDescriptor>(FindObjectsSortMode.None)
					.Where(avatar => avatar.gameObject.activeInHierarchy)
					.ToArray();
				SetCurrentAvatar(activeAvatars.Length > 0 ? activeAvatars[0] : null);
			} catch {
				SetCurrentAvatar(null);
			}
		}

		private static void SetCurrentAvatar(AvatarDescriptor newAvatar) {
			if (CurrentAvatar == newAvatar) return;
			Logger.LogDebug($"Current avatar changed to {(newAvatar ? newAvatar.name : "null")}");
			CurrentAvatar = newAvatar;
			OnAvatarSelected?.Invoke(newAvatar);
		}
	}
}
#endif