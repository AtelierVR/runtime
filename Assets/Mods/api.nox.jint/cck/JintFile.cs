using System.IO;
using UnityEngine;

namespace Nox.CCK.Worlds {
	public class JintFile : ScriptableObject {
		[SerializeField] public string text;

		public override string ToString()
			=> $"{GetType().Name}[]";
	}
}