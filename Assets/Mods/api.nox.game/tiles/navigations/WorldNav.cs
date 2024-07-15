using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace api.nox.game
{
    internal class WorldNav
    {
        internal NavigationTileManager navigationTile;
        private NavigationHandler _handler;

        public WorldNav(NavigationTileManager navigationTile)
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
                id = "api.nox.game.navigation.world",
                text_key = "dashboard.navigation.world",
                title_key = "dashboard.navigation.world.title",
                GetWorkers = () =>
                {
                    var config = Config.Load();
                    var servers = config.Get("navigation.servers", new WorkerInfo[0]);
                    return servers
                        .Where(x => x.features.Contains("world"))
                        .Select(x => new NavigationWorker
                        {
                            server_address = x.address,
                            server_title = x.title,
                            Fetch = async (string query) => await FetchWorlds(x.address, query)
                        }).ToArray();
                }
            };
        }

        private async UniTask<NavigationResult> FetchWorlds(string server, string query)
        {
            Debug.Log("Fetching worlds");
            var res = await navigationTile.clientMod.NetworkAPI.World.SearchWorlds(server, query);
            if (res == null) return new NavigationResult { error = "Error fetching worlds." };
            Debug.Log("Fetched worlds " + res.worlds.Length);
            return new NavigationResult
            {
                data = res.worlds.Select(x => new NavigationResultData
                {
                    title = x.title,
                    imageUrl = x.thumbnail,
                    goto_id = "game.world",
                    goto_data = new object[] { x }
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
}