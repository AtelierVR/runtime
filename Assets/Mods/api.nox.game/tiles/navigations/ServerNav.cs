using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace api.nox.game
{
    internal class ServerNav
    {
        internal NavigationTileManager navigationTile;
        private NavigationHandler _handler;

        public ServerNav(NavigationTileManager navigationTile)
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
                id = "api.nox.game.navigation.server",
                text_key = "dashboard.navigation.server",
                title_key = "dashboard.navigation.server.title",
                GetWorkers = () =>
                {
                    var config = Config.Load();
                    var servers = config.Get("navigation.servers", new WorkerInfo[0]);
                    return servers
                        .Where(x => x.features.Contains("server"))
                        .Select(x => new NavigationWorker
                        {
                            server_address = x.address,
                            server_title = x.title,
                            Fetch = async (string query) => await FetchServers(x.address, query)
                        }).ToArray();
                }
            };
        }

        private async UniTask<NavigationResult> FetchServers(string server, string query)
        {
            Debug.Log("Fetching servers");
            var res = await GameClientSystem.Instance.NetworkAPI.Server.SearchServers(server, query);
            if (res == null) return new NavigationResult { error = "Error fetching servers." };
            Debug.Log("Fetched servers " + res.servers.Length);
            return new NavigationResult
            {
                data = res.servers.Select(x => new NavigationResultData
                {
                    title = x.title,
                    imageUrl = x.icon,
                    goto_id = "game.server",
                    goto_data = new object[] { x }
                }).ToArray()
            };
        }

        private void UpdateHandler()
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