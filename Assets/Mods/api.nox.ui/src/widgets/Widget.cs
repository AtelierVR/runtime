using System;
using System.Collections.Generic;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.ui.widgets
{
    public class Widget : INoxObject
    {
        public string Key;
        public int Weight = 1;
        public Vector2Int Size = new(1, 1);
        public Func<int, RectTransform, GameObject> GetContent;

        internal static Widget From(Dictionary<string, object> data)
        {
            var widget = new Widget();
            if (data.TryGetValue("key", out var key) && key is string keyStr)
                widget.Key = keyStr;
            else return null;
            if (data.TryGetValue("weight", out var weight) && weight is int weightInt)
                widget.Weight = weightInt;
            var size = new Vector2Int(1, 1);
            if (data.TryGetValue("width", out var width) && width is int widthInt and > 0)
                size.x = widthInt;
            if (data.TryGetValue("height", out var height) && height is int heightInt and > 0)
                size.y = heightInt;
            widget.Size = size;
            if (data.TryGetValue("content", out var getContent)
                && getContent is Func<int, RectTransform, GameObject> getContentFunc)
                widget.GetContent = getContentFunc;
            return widget;
        }
    }
}