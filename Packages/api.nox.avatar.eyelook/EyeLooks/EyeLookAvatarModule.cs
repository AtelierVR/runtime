using System;
using Nox.Avatars;
using Nox.CCK.Build;
using System.Linq;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.EyeLooks {
	public class EyeLookAvatarModule : MonoBehaviour, IAvatarModule, ICompilable {
		private IAvatarDescriptor _descriptor;

		[SerializeReference]
		public BaseEyeLook[] eyeLooks = Array.Empty<BaseEyeLook>();

		public BaseEyeLook[] GetEyeLooks()
			=> eyeLooks?.ToArray() ?? Array.Empty<BaseEyeLook>();

		public void SetEyeLooks(BaseEyeLook[] value)
			=> eyeLooks = value ?? Array.Empty<BaseEyeLook>();

		public bool OnPlay(IRuntimeAvatar runtimeAvatar) {
			_descriptor = runtimeAvatar.GetDescriptor();;
			return true;
		}

		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<EyeLookAvatarModule>();

			if (modules.Length > 1) {
				Logger.LogError("Multiple EyeLookAvatarModule components found on the Avatar prefab. Only one is allowed.");
				return false;
			}

			return true;
		}
	}
}