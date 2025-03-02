using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.game.Tiles
{
    internal class WorldNav
    {
        private NavigationHandler _handler;

        public WorldNav()
        {
            _handler = new NavigationHandler
            {
                id = "api.nox.game.navigation.world",
                text_key = "dashboard.navigation.world",
                title_key = "dashboard.navigation.world.title",
                icon = GameClientSystem.CoreAPI.AssetAPI.GetAsset<Texture2D>("icons/world.png"),
                GetWorkers = () =>
                {
                    var config = Config.Load();
                    var serversT = config.Get("servers");
                    if (serversT == null) return Array.Empty<NavigationWorker>();
                    var serverD = serversT.ToObject<Dictionary<string, NavigationWorkerInfo>>();
                    var servers = serverD.Values.ToArray();
                    return servers
                        .Where(x => (x.navigation || x.address == config.Get("server", ""))
                                    && x.features.Contains("world"))
                        .Select(x => new NavigationWorker
                        {
                            server_address = x.address,
                            server_title = x.title,
                            Fetch = async query => await FetchWorlds(x.address, query)
                        }).ToArray();
                }
            };
        }

        private async UniTask<NavigationResult> FetchWorlds(string server, string query)
        {
            var worldApi = GameClientSystem.NetworkAPI.GetField("World");
            var assetApi = GameClientSystem.NetworkAPI.GetField("Asset");

            var res = await worldApi.CallAsyncMethod("SearchWorlds",
                new Dictionary<string, object>
                {
                    { "server", server },
                    { "query", query }
                });
            if (res == null) return new NavigationResult { error = "Error fetching worlds." };
            var data = new List<NavigationResultData>();
            var worlds = res.GetField<INoxObject[]>("worlds");
            Logger.LogDebug("Fetched worlds " + worlds.Length);
            foreach (var world in worlds)
            {
                var asset = await assetApi.CallAsyncMethod("SearchAssets",
                    new Dictionary<string, object>
                    {
                        { "server", world.GetField<string>("server") },
                        { "world_id", world.GetField<uint>("id") },
                        { "platforms", new[] { Constants.CurrentPlatform.GetPlatformName() } },
                        { "engines", new[] { Constants.CurrentEngine.GetEngineName() } },
                        { "limit", 1 },
                        { "offset", 0 }
                    });
                if (asset == null) continue;
                var assets = asset.GetField<INoxObject[]>("assets");
                if (assets.Length == 0) continue;
                data.Add(new NavigationResultData
                {
                    title = world.GetField<string>("title"),
                    imageUrl = world.GetField<string>("thumbnail"),
                    goto_id = "game.world",
                    goto_data = new object[] { world, assets[0] }
                });
            }

            return new NavigationResult { data = data.ToArray() };
        }

        internal void UpdateHandler() => GameClientSystem.CoreAPI.EventAPI.Emit("game.navigation", _handler);

        internal void OnDispose()
        {
            _handler.GetWorkers = null;
            UpdateHandler();
            _handler = null;
        }
    }
}