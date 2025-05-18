using System;
using System.Collections.Generic;
using Nox.CCK.Avatars;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.controller {
	public class DesktopController : MonoBehaviour, INoxObject {
		internal const int    DefaultPriority = 1;
		internal const string DefaultId       = "nox.desktop";

		internal static bool IsBetterThanCurrent() {
			var controller = PlayerSystem.Instance.GetController();
			return controller                           == null
				|| controller.GetField<int>("Priority") < DefaultPriority
				|| controller.GetField<string>("Id")    == DefaultId;
		}

		internal static void Make() {
			var prefab = PlayerSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("controller.prefab");
			if (!prefab) {
				Logger.LogError("Failed to load desktop controller prefab");
				return;
			}

			var instance = Instantiate(prefab);
			var desktop  = instance.GetComponent<DesktopController>();

			if (!desktop) {
				Logger.LogError("Failed to get desktop controller component");
				Destroy(instance);
				return;
			}

			PlayerSystem.Instance.SetController(desktop);
		}


		public CharacterController controller;
		public AvatarDescriptor    avatar;
		public Camera              camera;
		public Locomotion          locomotion;


		public string Id
			=> DefaultId;

		public int Priority
			=> DefaultPriority;

		public Dictionary<ushort, Transform> GetParts() {
			var anim = avatar?.Animator;
			var parts = new Dictionary<ushort, Transform> {
				{ PlayerRig.Base.ToIndex(), avatar?.transform ?? transform },
				{ PlayerRig.Head.ToIndex(), camera?.transform ?? transform }
			};
			foreach (HumanBodyBones bones in Enum.GetValues(typeof(HumanBodyBones))) {
				if (bones == HumanBodyBones.LastBone) continue;
				var bone = anim?.GetBoneTransform(bones);
				if (bone) parts.Add(bones.ToPlayerRig().ToIndex(), bone);
			}

			return parts;
		}
	}
}