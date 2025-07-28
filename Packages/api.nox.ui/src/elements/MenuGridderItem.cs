using System;
using UnityEngine;

namespace api.nox.ui.components {
	public class WidgetGridItem : MonoBehaviour {
		public WidgetGrid Gridder
			=> GetComponentInParent<WidgetGrid>();

		public uint              index    = 0u;
		public Vector2Int       position = Vector2Int.zero;
		public Vector2Int       size     = new(1, 1);
		public GridderItemFlags flags    = GridderItemFlags.None;

		void OnValidate()
			=> UpdatePosition();

		public void UpdatePosition(Vector2Int pos, Vector2 dimensions = default) {
			this.position = pos;
			UpdatePosition(dimensions);
		}

		private void UpdatePosition(Vector2 dimensions = default) {
			try {
				if (Gridder == null) return;
				if (Gridder.dimensions is { x: 0, y: 0 }) return;
				var rect   = GetComponent<RectTransform>();
				var parent = rect?.parent?.GetComponent<RectTransform>();
				if (parent == null) return;

				dimensions = dimensions == default
					? Gridder.GetDimensions()
					: dimensions;

				var cellWidth  = parent.rect.width  / dimensions.x;
				var cellHeight = parent.rect.height / dimensions.y;

				var totalSpacingX = Gridder.spacing * (dimensions.x - 1);
				var totalSpacingY = Gridder.spacing * (dimensions.y - 1);

				cellWidth  -= totalSpacingX / dimensions.x;
				cellHeight -= totalSpacingY / dimensions.y;

				rect.anchoredPosition = new Vector2(
					position.x  * (Gridder.dimensions.x == 0 ? cellHeight : cellWidth) + position.x * Gridder.spacing,
					-position.y * (Gridder.dimensions.y == 0 ? cellWidth : cellHeight) - position.y * Gridder.spacing
				);
				rect.sizeDelta = new Vector2(
					size.x * (Gridder.dimensions.x == 0 ? cellHeight : cellWidth) + (size.x - 1) * Gridder.spacing,
					size.y * (Gridder.dimensions.y == 0 ? cellWidth : cellHeight) + (size.y - 1) * Gridder.spacing
				);
			} catch {
				// ignored
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