using UnityEngine;

namespace Nox.CCK.Jint {
	public class JintFile : ScriptableObject {
		[SerializeField] public string text;

		public override string ToString()
			=> $"{GetType().Name}[]";
	}
}