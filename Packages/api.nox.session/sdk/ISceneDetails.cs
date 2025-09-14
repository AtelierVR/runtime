using Nox.Worlds;
using UnityEngine;

namespace Nox.Sessions {
	public interface ISceneDetails {
		public GameObject       GetAnchor();
		public IWorldDescriptor GetDescriptor();
	}
}