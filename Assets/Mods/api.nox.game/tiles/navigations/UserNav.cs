using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace api.nox.game
{
    internal class UserNav
    {
        internal NavigationTileManager navigationTile;
        private NavigationHandler _handler;

        public UserNav(NavigationTileManager navigationTile)
        {
            this.navigationTile = navigationTile;
            GenerateHandler();
            Initialization().Forget();
        }

        private async UniTask Initialization()
        {
            await UniTask.DelayFrame(1);
            UpdateHandler();
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
                    var servers = config.Get("navigation.servers", new WorkerInfo[0]);
                    return servers
                        .Where(x => x.features.Contains("user"))
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
            var res = await navigationTile.clientMod.NetworkAPI.User.SearchUsers(server, query);
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

        private void UpdateHandler()
        {
            navigationTile.clientMod.coreAPI.EventAPI.Emit("game.navigation", _handler);
        }

        internal void OnDispose()
        {
            _handler.GetWorkers = null;
            UpdateHandler();
            _handler = null;
        }
    }

    [Serializable]
    internal class WorkerInfo
    {
        public string address;
        public string title;
        public string[] features;
    }
}