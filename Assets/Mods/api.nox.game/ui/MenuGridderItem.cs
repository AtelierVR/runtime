using System;
using UnityEngine;

namespace api.nox.game
{
    public class MenuGridderItem : MonoBehaviour
    {
        public MenuGridder gridder => GetComponentInParent<MenuGridder>();
        public uint index => (uint)transform.GetSiblingIndex();
        public Vector2 position = Vector2.zero;
        public Vector2 size = new(1, 1);
        public GridderItemFlags flags = GridderItemFlags.None;

        void OnValidate() => UpdatePosition();

        public void UpdatePosition(Vector2 position)
        {
            this.position = position;
            UpdatePosition();
        }

        public void UpdatePosition()
        {
            var rect = GetComponent<RectTransform>();
            if (rect == null) return;
            var parent = rect.parent?.GetComponent<RectTransform>();
            if (parent == null) return;
            Debug.Log("Parent: " + parent.name + " for " + rect.name);
            rect.anchoredPosition = new Vector2(
                position.x * parent.rect.width / gridder.dimensions.x,
                -position.y * parent.rect.height / gridder.dimensions.y
            );
            rect.sizeDelta = new Vector2(
                size.x * parent.rect.width / gridder.dimensions.x,
                size.y * parent.rect.height / gridder.dimensions.y
            );
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