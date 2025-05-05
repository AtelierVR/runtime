using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.search
{
    public class Handler : INoxObject
    {
        public string Id;
        public string TitleKey;
        public string PlaceholderKey;
        public Texture2D Icon;
        public Func<Worker[]> GetWorkers;

        public static Handler From(Dictionary<string, object> data)
        {
            var handler = new Handler();

            if (data.TryGetValue("id", out var id) && id is string i)
                handler.Id = i;

            if (data.TryGetValue("title_key", out var titleKey) && titleKey is string ti)
                handler.TitleKey = ti;

            if (data.TryGetValue("placeholder_key", out var placeholderKey) && placeholderKey is string pk)
                handler.PlaceholderKey = pk;

            if (data.TryGetValue("icon", out var icon) && icon is Texture2D iconTex)
                handler.Icon = iconTex;

            if (data.TryGetValue("workers", out var getWorkers) && getWorkers is Func<Dictionary<string, object>[]> gw)
                handler.GetWorkers = () => gw().Select(Worker.From).ToArray();

            return handler;
        }
    }
}