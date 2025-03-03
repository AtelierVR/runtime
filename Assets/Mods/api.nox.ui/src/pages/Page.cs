using System;
using System.Collections.Generic;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
using Transform = UnityEngine.Transform;

namespace api.nox.ui.pages
{
    public class Page : INoxObject, IDisposable
    {
        [NoxPublic(NoxAccess.Read)] public string Key;
        [NoxPublic(NoxAccess.Read)] public int MenuId;

        internal GameObject Content;
        internal object[] Context;

        // Generates the content of the tile (the Transform is the parent of the content)
        [NoxPublic(NoxAccess.Field)] public Func<RectTransform, GameObject> GetContent;

        // Called when the tile is opened at the first time (before reading the content) (the string is the previous tile id)
        [NoxPublic(NoxAccess.Field)] public Action<string, GameObject> OnOpen = null;

        // Called when the tile is restored (before reading the content) (the string is the previous tile id)
        [NoxPublic(NoxAccess.Field)] public Action<string, GameObject> OnRestore = null;

        // Called when the tile is removed (after hiding the content) (the string is the next tile id)
        [NoxPublic(NoxAccess.Field)] public Action<GameObject> OnRemove = null;

        // Called when the tile is displayed (after reading the content) (the string is the previous tile id)
        [NoxPublic(NoxAccess.Field)] public Action<string, GameObject> OnDisplay = null;

        // Called when the tile is hidden (after hiding the content) (the string is the next tile id)
        [NoxPublic(NoxAccess.Field)] public Action<string, GameObject> OnHide = null;

        public void Dispose()
        {
            OnRemove?.Invoke(Content);
            Object.Destroy(Content);
            GetContent = null;
            OnOpen = null;
            OnRestore = null;
            OnRemove = null;
            OnDisplay = null;
            OnHide = null;
        }

        internal static Page From(Dictionary<string, object> data)
        {
            var page = new Page();
            if (data.TryGetValue("key", out var key) && key is string keyStr)
                page.Key = keyStr;
            else return null;
            if (data.TryGetValue("content", out var getContent) &&
                getContent is Func<Transform, GameObject> getContentFunc)
                page.GetContent = getContentFunc;
            if (data.TryGetValue("open", out var onOpen) && onOpen is Action<string, GameObject> onOpenAction)
                page.OnOpen = onOpenAction;
            if (data.TryGetValue("restore", out var onRestore) && onRestore is Action<string, GameObject> onRestoreAction)
                page.OnRestore = onRestoreAction;
            if (data.TryGetValue("remove", out var onRemove) && onRemove is Action<GameObject> onRemoveAction)
                page.OnRemove = onRemoveAction;
            if (data.TryGetValue("display", out var onDisplay) &&
                onDisplay is Action<string, GameObject> onDisplayAction)
                page.OnDisplay = onDisplayAction;
            if (data.TryGetValue("hide", out var onHide) && onHide is Action<string, GameObject> onHideAction)
                page.OnHide = onHideAction;
            return page;
        }
    }
}