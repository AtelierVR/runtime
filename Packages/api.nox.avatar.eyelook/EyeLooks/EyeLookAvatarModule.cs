using System;
using Nox.Avatars;
using Nox.CCK.Build;
using System.Linq;
using UnityEngine;

namespace Nox.CCK.Avatars.EyeLooks {
	public class EyeLookAvatarModule : MonoBehaviour, IAvatarModule, ICompilable {
		private IAvatarDescriptor _descriptor;
		
		[SerializeReference] public BaseEyeLook[] eyeLooks = Array.Empty<BaseEyeLook>();

		public BaseEyeLook[] GetEyeLooks()
			=> eyeLooks?.ToArray() ?? Array.Empty<BaseEyeLook>();

		public void SetEyeLooks(BaseEyeLook[] value)
			=> eyeLooks = value ?? Array.Empty<BaseEyeLook>();

		public void OnPlay(IAvatarDescriptor descriptor)
			=> _descriptor = descriptor;
	}
}