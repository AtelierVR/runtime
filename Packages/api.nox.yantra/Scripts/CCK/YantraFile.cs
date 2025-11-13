using Nox.YantraJS;
using UnityEngine;

namespace Nox.CCK.YantraJS {
	[CreateAssetMenu(fileName = "YantraFile", menuName = "Nox/YantraJS File", order = 1)]
	public class YantraFile : ScriptableObject {
		[SerializeField]
		public string text;

		public string GetText()
			=> text;

		public override string ToString()
			=> $"{GetType().Name}[]";
	}
}

