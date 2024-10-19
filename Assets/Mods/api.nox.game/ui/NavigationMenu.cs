using System;
using UnityEngine;

namespace api.nox.game.UI
{

    [Serializable]
    public class NavigationMenu
    {
        public string key;
        public NavigationMenuItem[] items;
        public RectTransform container;
        public GameObject itemPrefab;
        public Vector2 anchor;
    }
}