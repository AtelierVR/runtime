using Cysharp.Threading.Tasks;
using Nox.Avatars;
using UnityEngine;

namespace api.nox.avatar {
	public abstract class BaseRuntimeRuntimeAvatar : IRuntimeAvatar {
		public IAvatarDescriptor Descriptor;
		public IAvatarIdentifier Identifier;
		public string            Id;
		public GameObject        Root;

		public virtual string GetId()
			=> Id;

		public virtual IAvatarDescriptor GetDescriptor()
			=> Descriptor;

		public IAvatarIdentifier GetIdentifier()
			=> Identifier;

		public void SetIdentifier(IAvatarIdentifier identifier)
			=> Identifier = identifier;

		public abstract UniTask Dispose();
	}
}