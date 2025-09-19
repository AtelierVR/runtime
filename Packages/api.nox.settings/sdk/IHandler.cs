using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.Settings {
	public interface IHandler {
		public string[] GetPath();

		public GameObject GetContent(RectTransform transform);

		public UniTask<GameObject> GetContentAsync(RectTransform transform);
	}
}