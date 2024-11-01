using System;
using UnityEngine;
using UnityEngine.Events;

namespace api.nox.game.Tiles
{
    public class SessionHandler
    {
        public string id;
        public string text_key;
        public string title_key;
        public Texture2D icon;
        public Func<TileObject, bool> CanSelect;
        public UnityAction<TileObject, GameObject> OnSelected;
        public UnityAction<TileObject, GameObject> OnDeselected;

        public Func<TileObject, GameObject, Transform, GameObject> GetContent;
        public Action<TileObject, GameObject, Transform> OnUpdate;

        public virtual void Dispose() { }
    }
}