using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;

namespace api.nox.game.Tiles
{
    internal class UserNav
    {
        internal NavigationTileManager navigationTile;
        private NavigationHandler _handler;

        public UserNav(NavigationTileManager navigationTile)
        {
            this.navigationTile = navigationTile;
            GenerateHandler();
        }


        private void GenerateHandler()
        {

            _handler = new NavigationHandler
            {
                id = "api.nox.game.navigation.user",
                text_key = "dashboard.navigation.user",
                title_key = "dashboard.navigation.user.title",
                GetWorkers = () =>
                {
                    var config = Config.Load();
                    var servers_t = config.Get("servers");
                    if (servers_t == null) return new NavigationWorker[0];
                    var server_d = servers_t.ToObject<Dictionary<string, NavigationWorkerInfo>>();
                    var servers = server_d.Values.ToArray();
                    return servers
                        .Where(x => (x.navigation || x.address == config.Get("server", "")) && x.features.Contains("user"))
                        .Select(x => new NavigationWorker
                        {
                            server_address = x.address,
                            server_title = x.title,
                            Fetch = async (string query) => await FetchUsers(x.address, query)
                        }).ToArray();
                }
            };
        }

        private async UniTask<NavigationResult> FetchUsers(string server, string query)
        {
            Debug.Log("Fetching users");
            var res = await GameClientSystem.Instance.NetworkAPI.User.SearchUsers(new() { server = server, query = query });
            if (res == null) return new NavigationResult { error = "Error fetching users." };
            Debug.Log("Fetched users " + res.users.Length);
            return new NavigationResult
            {
                data = res.users.Select(x => new NavigationResultData
                {
                    title = x.display,
                    imageUrl = x.thumbnail,
                    goto_id = "game.user",
                    goto_data = new object[] { x, false }
                }).ToArray()
            };
        }

        internal void UpdateHandler()
        {
            GameClientSystem.CoreAPI.EventAPI.Emit("game.navigation", _handler);
        }

        internal void OnDispose()
        {
            _handler.GetWorkers = null;
            UpdateHandler();
            _handler = null;
        }
    }

}