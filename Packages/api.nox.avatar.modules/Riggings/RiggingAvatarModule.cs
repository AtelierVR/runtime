using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

#if HAS_FINALIK
using RootMotion.FinalIK;
#endif

namespace Nox.CCK.Avatars.Rigging {
	public class RiggingAvatarModule : MonoBehaviour, IRiggingModule, IParameterGroup {
		private IAvatarDescriptor _descriptor;
		private Transform         _anchor;

		public readonly List<IParameter>  Parameters = new();
		public readonly List<RiggingPart> Parts      = new();

		#if HAS_FINALIK
		private VRIK _vrik;

		public VRIK GetVRIK()
			=> _vrik ? _vrik : _vrik = _descriptor.GetAnimator().GetOrAddComponent<VRIK>();
		#endif

		// GetRigBuilder est toujours disponible pour la compatibilité avec IKRigGenerator
		private RigBuilder _rigBuilder;

		public RigBuilder GetRigBuilder()
			=> _rigBuilder ? _rigBuilder : _rigBuilder = _descriptor.GetAnimator().GetOrAddComponent<RigBuilder>();

		public Transform GetAnchor() {
			if (_anchor) return _anchor;
			_anchor = new GameObject("Rigging Anchor").transform;
			_anchor.transform.SetParent(transform, false);
			_anchor.transform.localPosition = Vector3.zero;
			_anchor.transform.localRotation = Quaternion.identity;
			_anchor.transform.localScale    = Vector3.one;
			return _anchor;
		}

		public UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			_descriptor = runtimeAvatar.GetDescriptor();

			#if HAS_FINALIK
			// Utilise FinalIK VR (préféré)
			FinalIKRigGenerator.CreateVRIKRig(this);
			#else
			// Utilise RigBuilder (legacy)
			IKRigGenerator.CreateIKRig(this);
			#endif

			IKRigParameters.SetupParameters(this);
			HeadTarget.CreateTargets(this);
			return UniTask.FromResult(true);
		}

		bool IRiggingModule.TryGetPart(ushort id, out IRigPart part) {
			part = Parts.FirstOrDefault(p => p.GetId() == id);
			return part != null;
		}

		public Transform GetPart(HumanBodyBones bone) {
			var index = bone.ToIndex();
			var part  = Parts.FirstOrDefault(p => p.GetId() == index);
			return part?.GetTransform();
		}

		public IRigPart[] GetParts()
			=> Parts.Cast<IRigPart>().ToArray();


		public void SetPart(HumanBodyBones bone, Transform part) {
			var index        = bone.ToIndex();
			var existingPart = Parts.FirstOrDefault(p => p.GetId() == index);
			if (existingPart != null) {
				existingPart.SetTransform(part);
				return;
			}

			var rigPart = new RiggingPart(index, part);
			Parts.Add(rigPart);
		}

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