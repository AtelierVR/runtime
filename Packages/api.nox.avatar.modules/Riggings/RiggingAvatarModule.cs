using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace Nox.CCK.Avatars.Rigging {
	public class RiggingAvatarModule : MonoBehaviour, IAvatarModule, IRiggingModule, IParameterGroup {
		private IAvatarDescriptor _descriptor;
		private Transform         _anchor;

		public List<IParameter>                      Parameters = new();
		public Dictionary<HumanBodyBones, Transform> Parts      = new();

		public Transform GetAnchor() {
			if (_anchor) return _anchor;
			_anchor = new GameObject("Rigging Anchor").transform;
			_anchor.transform.SetParent(transform, false);
			_anchor.transform.localPosition = Vector3.zero;
			_anchor.transform.localRotation = Quaternion.identity;
			_anchor.transform.localScale    = Vector3.one;
			return _anchor;
		}

		public RigBuilder GetRigBuilder()
			=> _descriptor.GetAnimator().GetOrAddComponent<RigBuilder>();

		public UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			_descriptor = runtimeAvatar.GetDescriptor();
			IKRigGenerator.CreateIKRig(this);
			IKRigParameters.SetupParameters(this);
			return UniTask.FromResult(true);
		}

		public Transform GetPart(HumanBodyBones bone)
			=> Parts.GetValueOrDefault(bone);

		public void SetPart(HumanBodyBones bone, Transform part)
			=> Parts[bone] = part;

		public Transform GetBone(HumanBodyBones bone)
			=> _descriptor.GetAnimator().GetBoneTransform(bone);

		public IParameter[] GetParameters()
			=> Parameters.Cast<IParameter>().ToArray();

		public IParameter GetParameter(string key)
			=> Parameters.FirstOrDefault(p => p.GetName() == key);

		public IParameter GetParameter(int hash)
			=> Parameters.FirstOrDefault(p => p.GetHash() == hash);

		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<RiggingAvatarModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.GetAnchor().AddComponent<RiggingAvatarModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the Avatar prefab has a valid RiggingAvatarModule component.");
				return false;
			}

			return true;
		}
	}
}