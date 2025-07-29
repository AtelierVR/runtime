using System;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui.components {
	public class WidgetGridItem : MonoBehaviour, IUpdateLayout {
		public WidgetGrid Gridder
			=> GetComponentInParent<WidgetGrid>();

		public uint             index    = 0u;
		public Vector2Int       position = Vector2Int.zero;
		public Vector2Int       size     = new(1, 1);
		public GridderItemFlags flags    = GridderItemFlags.None;

		void OnValidate()
			=> UpdateLayout();

		public void UpdateLayout()
			=> UpdatePosition();

		public void UpdatePosition(Vector2Int pos, Vector2 dimensions = default) {
			position = pos;
			UpdatePosition(dimensions);
		}

		private void UpdatePosition(Vector2 dimensions = default) {
			try {
				var gridder = Gridder;
				if (!gridder) return;
				if (gridder.dimensions is { x: 0, y: 0 }) return;
				var rect   = GetComponent<RectTransform>();
				var parent = rect?.parent?.GetComponent<RectTransform>();
				if (!parent) return;

				var cellSize = gridder.GetCellSize();
				
				rect.anchoredPosition = new Vector2(
					position.x  * (gridder.dimensions.x == 0 ? cellSize.y : cellSize.x) + position.x * gridder.spacing,
					-position.y * (gridder.dimensions.y == 0 ? cellSize.x : cellSize.y) - position.y * gridder.spacing
				);
				rect.sizeDelta = new Vector2(
					size.x * (gridder.dimensions.x == 0 ? cellSize.y : cellSize.x) + (size.x - 1) * gridder.spacing,
					size.y * (gridder.dimensions.y == 0 ? cellSize.x : cellSize.y) + (size.y - 1) * gridder.spacing
				);
			} catch (Exception e) {
				Logger.LogException(e, this);
			}
		}
	}

	[Flags]
	public enum GridderItemFlags {
		None            = 0,
		ManualPosition  = 1,
		IgnoreCollision = 2,
		AlwaysVisible   = 4,
		ManualVisible   = 8,
		Manual          = ManualPosition | IgnoreCollision
	}
}