using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace Nox.CCK.Avatars.Rigging {
	public abstract class BaseRiggingModule : MonoBehaviour, IRiggingModule, IParameterGroup {
		public IAvatarDescriptor Descriptor;

		public readonly List<IParameter>  Parameters = new();
		public readonly List<RiggingPart> Parts      = new();

		public abstract bool SetupParameters(BaseRiggingModule module);

		public abstract bool IsActive(HumanBodyBones bone);

		public abstract void SetActive(HumanBodyBones bone, bool active);

		public async UniTask<bool> Setup(IRuntimeAvatar runtime) {
			// Vérification de sécurité pour éviter les NullReferenceException
			if (runtime == null) {
				Logger.LogError("RuntimeAvatar is null, cannot setup rigging.");
				return false;
			}

			// Supprimer le composant après l'initialisation
			var anchor = runtime.GetDescriptor().GetAnchor();

			#if HAS_FINALIK
			// Utilise FinalIK VR (préféré)
			var arguments = runtime.GetArguments();
			var ufik = arguments != null
				&& arguments.TryGetValue("local", out var isLocalObj)
				&& isLocalObj is true
				&& arguments.TryGetValue("xr", out var allowXRObj)
				&& allowXRObj is true;

			if (ufik) {
				var fik = anchor.GetOrAddComponent<FinalIKAvatarModule>();
				fik.Descriptor = runtime.GetDescriptor();
				FinalIKRigGenerator.Create(fik);
			} else {
				var rik = anchor.GetOrAddComponent<RigBuilderAvatarModule>();
				rik.Descriptor = runtime.GetDescriptor();
				RigBuilderRigGenerator.Create(rik);
			}
			#else
			// Utilise RigBuilder (legacy)
			var rik = anchor.GetOrAddComponent<RigBuilderAvatarModule>();
			rik.Descriptor = runtime.GetDescriptor();
			IKRigGenerator.CreateIKRig(rik);
			#endif

			var module = anchor.GetComponent<BaseRiggingModule>();
			if (!module) {
				Logger.LogError("BaseRiggingModule component is missing on avatar anchor.");
				return false;
			}

			module.Descriptor = runtime.GetDescriptor();

			// Vérification de sécurité pour éviter les NullReferenceException
			if (module.Descriptor == null) {
				Logger.LogError("Avatar descriptor is null, cannot setup rigging.");
				return false;
			}

			if (!IKRigParameters.SetupParameters(module)) {
				Logger.LogError("Failed to setup rigging parameters.");
				return false;
			}

			return true;
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
			=> Descriptor.GetAnimator().GetBoneTransform(bone);

		public IParameter[] GetParameters()
			=> Parameters.Cast<IParameter>().ToArray();

		public IParameter GetParameter(string key)
			=> Parameters.FirstOrDefault(p => p.GetName() == key);

		public IParameter GetParameter(int hash)
			=> Parameters.FirstOrDefault(p => p.GetKey() == hash);

		public static bool Check(IAvatarDescriptor descriptor)
			=> descriptor.GetModules<BaseRiggingModule>().Length switch {
				1 => true,
				0 => descriptor.GetAnchor().AddComponent<RigBuilderAvatarModule>(),
				_ => false
			};
	}
}