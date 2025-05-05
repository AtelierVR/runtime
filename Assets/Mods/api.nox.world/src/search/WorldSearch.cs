using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.world.client;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.world.search
{
    public class WorldSearch
    {
        private static MainModInitializer SearchSystem
            => WorldSystem.CoreAPI.ModAPI
                .GetMod("search").GetMains()
                .FirstOrDefault();

        private readonly INoxObject _handler;

        internal WorldSearch()
        {
            _handler = SearchSystem?.CallMethod("AddHandler", new Dictionary<string, object>()
            {
                { "id", "api.nox.world.search" },
                { "title_key", "search.world.title" },
                { "placeholder_key", "search.world.placeholder" },
                { "icon", WorldSystem.CoreAPI.AssetAPI.GetAsset<Texture2D>("icons/globe.png") },
                {
                    "workers", new Func<Dictionary<string, object>[]>(() =>
                    {
                        var x0 = Config.Load().Get("servers");
                        if (x0 == null) return Array.Empty<Dictionary<string, object>>();
                        var x1 = x0.ToObject<Dictionary<string, JObject>>();
                        var x2 = new List<Dictionary<string, object>>();

                        foreach (var (address, value) in x1)
                        {
                            var title = value["title"]?.ToString();
                            var features = value["features"]?.Values<string>().ToArray() ?? Array.Empty<string>();
                            var search = value["search"]?.ToObject<bool>() ?? false;
                            if (!(search && features.Contains("world"))) continue;
                            x2.Add(new Dictionary<string, object>
                            {
                                { "server_address", address },
                                { "server_title", title },
                                {
                                    "fetch",
                                    new Func<Dictionary<string, object>, UniTask<Dictionary<string, object>>>(
                                        data => Fetch(data, address)
                                    )
                                }
                            });
                        }

                        return x2.ToArray();
                    })
                }
            });
        }

        private async UniTask<Dictionary<string, object>> Fetch(Dictionary<string, object> data, string server)
        {
            var query = data["query"]?.ToString();
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(query))
                return new Dictionary<string, object> { { "error", "Invalid server or query." } };
            var res = await WorldSystem.NetworkAPI.GetField("World").CallAsyncMethod("SearchWorlds",
                new Dictionary<string, object>
                {
                    { "server", server },
                    { "query", query }
                });
            if (res == null)
                return new Dictionary<string, object> { { "error", "Error fetching worlds." } };
            var worlds = res.GetField<INoxObject[]>("worlds");

            return new Dictionary<string, object>
            {
                {
                    "data",
                    worlds.Select(x => new Dictionary<string, object>
                    {
                        { "id", x.CallMethod("ToIdentifier").CallMethod<string>("ToFullString", server).GetHashCode() },
                        { "title", x.GetField<string>("title") },
                        { "image_url", x.GetField<string>("thumbnail") },
                        { "goto_id", WorldPage.GetKey() },
                        { "goto_data", new object[] { "world", x } }
                    }).ToArray()
                },
                { "ratio", 4f / 3f }
            };
        }

        internal void Dispose()
        {
            SearchSystem?.CallMethod("RemoveHandler", _handler);
        }
    }
}