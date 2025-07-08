using UnityEngine;

namespace Nox.Widgets {
	public interface IWidgetComportment {
		public int        GetId();
		public string     GetFrom();
		public Vector2Int GetSize();
	}
}