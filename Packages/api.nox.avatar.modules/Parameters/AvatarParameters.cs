using UnityEngine;

namespace Nox.CCK.Avatars.Parameters {
	[CreateAssetMenu(fileName = "AvatarParameters", menuName = "Nox/Avatars/Parameters", order = 1)]
	public class AvatarParameters : ScriptableObject {
		public ParameterEntry[] parameters;
	}
}