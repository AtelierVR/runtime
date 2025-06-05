using UnityEngine;

namespace Nox.Widgets {
	public interface IWidget {
		public string     GetKey();
		public Vector2Int GetSize();
		public GameObject GetContent();
	}
}