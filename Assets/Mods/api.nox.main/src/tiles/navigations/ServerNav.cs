/*using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.game.Tiles
{
    internal class ServerNav
    {
        private NavigationHandler _handler;

        public ServerNav()
        {
            _handler = new NavigationHandler
            {
                id = "api.nox.game.navigation.server",
                text_key = "dashboard.navigation.server",
                title_key = "dashboard.navigation.server.title",
                icon = GameClientSystem.CoreAPI.AssetAPI.GetAsset<Texture2D>("icons/server.png"),
                GetWorkers = () =>
                {
                    var config = Config.Load();
                    var serversT = config.Get("servers");
                    if (serversT == null) return Array.Empty<NavigationWorker>();
                    var serverD = serversT.ToObject<Dictionary<string, NavigationWorkerInfo>>();
                    var servers = serverD.Values.ToArray();
                    return servers
                        .Where(x => (x.navigation || x.address == config.Get("server", ""))
                                    && x.features.Contains("server"))
                        .Select(x => new NavigationWorker
                        {
                            server_address = x.address,
                            server_title = x.title,
                            Fetch = async query => await FetchServers(x.address, query)
                        }).ToArray();
                }
            };
        }

        private async UniTask<NavigationResult> FetchServers(string server, string query)
        {
            Logger.Log("Fetching servers");
            var res = await GameClientSystem.NetworkAPI.GetField<INoxObject>("Server")
                .CallMethod<UniTask<INoxObject>>("SearchServers", new Dictionary<string, object>()
                {
                    { "server", server },
                    { "query", query }
                });
            if (res == null) return new NavigationResult { error = "Error fetching servers." };
            var servers = res.GetField<INoxObject[]>("servers");
            Logger.Log("Fetched servers " + servers.Length);
            return new NavigationResult
            {
                data = servers.Select(x => new NavigationResultData
                {
                    title = x.GetField<string>("title"),
                    imageUrl = x.GetField<string>("icon"),
                    goto_id = "game.server",
                    goto_data = new object[] { x }
                }).ToArray()
            };
        }

        internal void UpdateHandler() => GameClientSystem.CoreAPI.EventAPI.Emit("game.navigation", _handler);

        internal void OnDispose()
        {
            _handler.GetWorkers = null;
            UpdateHandler();
            _handler = null;
        }
    }
}*/