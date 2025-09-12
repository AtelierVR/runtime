using System;
using Nox.Avatars;
using Nox.CCK.Build;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.EyeLooks {
	public class EyeLookAvatarModule : MonoBehaviour, IAvatarModule {
		private IAvatarDescriptor _descriptor;

		[SerializeReference]
		public BaseEyeLook[] eyeLooks = Array.Empty<BaseEyeLook>();

		public BaseEyeLook[] GetEyeLooks()
			=> eyeLooks?.ToArray() ?? Array.Empty<BaseEyeLook>();

		public void SetEyeLooks(BaseEyeLook[] value)
			=> eyeLooks = value ?? Array.Empty<BaseEyeLook>();

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			await UniTask.Yield();
			_descriptor = runtimeAvatar.GetDescriptor();
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