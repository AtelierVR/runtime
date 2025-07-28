using System;
using UnityEngine;

namespace Nox.UI.Widgets {
	public interface IWidget {
		public string GetKey();

		public Vector2Int GetSize();

		public int GetPriority();
	}
}