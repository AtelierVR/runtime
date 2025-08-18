using Cysharp.Threading.Tasks;
using Nox.Avatars;
using UnityEngine;

namespace api.nox.avatar {
	public abstract class BaseAvatar : IAvatar {
		public IAvatarDescriptor Descriptor;
		public GameObject        Root;
		
		public virtual string GetId()
			=> Root.GetInstanceID().ToString();

		public virtual IAvatarDescriptor GetDescriptor()
			=> Descriptor;

		public abstract UniTask Dispose();
	}
}