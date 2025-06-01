/*using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.game.Tiles
{
    internal class UserNav
    {
        private NavigationHandler _handler;

        public UserNav()
        {
            _handler = new NavigationHandler
            {
                id = "api.nox.game.navigation.user",
                text_key = "dashboard.navigation.user",
                title_key = "dashboard.navigation.user.title",
                icon = GameClientSystem.CoreAPI.AssetAPI.GetAsset<Texture2D>("icons/profile.png"),
                GetWorkers = () =>
                {
                    var config = Config.Load();
                    var serversT = config.Get("servers");
                    if (serversT == null) return Array.Empty<NavigationWorker>();
                    var serverD = serversT.ToObject<Dictionary<string, NavigationWorkerInfo>>();
                    var servers = serverD.Values.ToArray();
                    return servers
                        .Where(x => (x.navigation || x.address == config.Get("server", "")) &&
                                    x.features.Contains("user"))
                        .Select(x => new NavigationWorker
                        {
                            server_address = x.address,
                            server_title = x.title,
                            Fetch = async query => await FetchUsers(x.address, query)
                        }).ToArray();
                }
            };
        }

        private async UniTask<NavigationResult> FetchUsers(string server, string query)
        {
            Logger.Log("Fetching users");
            var res = await GameClientSystem.NetworkAPI.GetField("User").CallAsyncMethod("SearchUsers",
                new Dictionary<string, object>
                {
                    { "server", server },
                    { "query", query }
                });
            if (res == null) return new NavigationResult { error = "Error fetching users." };
            var users = res.GetField<INoxObject[]>("users");
            Logger.Log("Fetched users " + users.Length);
            return new NavigationResult
            {
                data = users.Select(x => new NavigationResultData
                {
                    title = x.GetField<string>("display"),
                    imageUrl = x.GetField<string>("thumbnail"),
                    goto_id = "game.user",
                    goto_data = new object[] { x, false }
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