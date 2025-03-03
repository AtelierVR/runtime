using System;
using UnityEngine;

namespace api.nox.game
{
    public class MenuGridderItem : MonoBehaviour
    {
        public MenuGridder gridder => GetComponentInParent<MenuGridder>();
        public uint index => (uint)transform.GetSiblingIndex();
        public Vector2Int position = Vector2Int.zero;
        public Vector2Int size = new(1, 1);
        public GridderItemFlags flags = GridderItemFlags.None;

        void OnValidate() => UpdatePosition();

        public void UpdatePosition(Vector2Int position, Vector2 dimensions = default)
        {
            this.position = position;
            UpdatePosition(dimensions);
        }

        private void UpdatePosition(Vector2 dimensions = default)
        {
            try
            {
                if (gridder == null) return;
                if (gridder.dimensions is { x: 0, y: 0 }) return;
                var rect = GetComponent<RectTransform>();
                var parent = rect?.parent?.GetComponent<RectTransform>();
                if (parent == null) return;

                dimensions = dimensions == default
                    ? gridder.GetDimensions()
                    : dimensions;

                var cellWidth = parent.rect.width / dimensions.x;
                var cellHeight = parent.rect.height / dimensions.y;
                
                var totalSpacingX = gridder.spacing * (dimensions.x - 1);
                var totalSpacingY = gridder.spacing * (dimensions.y - 1);
                
                cellWidth -= totalSpacingX / dimensions.x;
                cellHeight -= totalSpacingY / dimensions.y;
                
                rect.anchoredPosition = new Vector2(
                    position.x * (gridder.dimensions.x == 0 ? cellHeight : cellWidth) + position.x * gridder.spacing,
                    -position.y * (gridder.dimensions.y == 0 ? cellWidth : cellHeight) - position.y * gridder.spacing
                );
                rect.sizeDelta = new Vector2(
                    size.x * (gridder.dimensions.x == 0 ? cellHeight : cellWidth) + (size.x - 1) * gridder.spacing,
                    size.y * (gridder.dimensions.y == 0 ? cellWidth : cellHeight) + (size.y - 1) * gridder.spacing
                );
            }
            catch
            {
                // ignored
            }
        }
    }

    [Flags]
    public enum GridderItemFlags
    {
        None = 0,
        ManualPosition = 1,
        IgnoreCollision = 2,
        AlwaysVisible = 4,
        ManualVisible = 8,
        Manual = ManualPosition | IgnoreCollision
    }
}