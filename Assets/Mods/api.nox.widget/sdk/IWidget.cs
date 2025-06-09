using UnityEngine;

namespace Nox.Widgets {
	public interface IWidget {
		public string GetKey();
		public GameObject Build(RectTransform parent);
	}
}