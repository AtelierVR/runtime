using System.Collections.Generic;
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
            var res = await GameClientSystem.Instance.NetworkAPI.World.SearchWorlds(server, query);
            if (res == null) return new NavigationResult { error = "Error fetching worlds." };
            var data = new List<NavigationResultData>();
            for (var i = 0; i < res.worlds.Length; i++)
            {
                var world = res.worlds[i];
                var asset = await world.SearchAssets(0, 1, null, new string[] { PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform) }, new string[] { "unity" });
                if (asset == null || asset.assets.Length == 0) continue;
                data.Add(new NavigationResultData
                {
                    title = world.title,
                    imageUrl = world.thumbnail,
                    goto_id = "game.world",
                    goto_data = new object[] { world, asset.assets[0] }
                });
            }
            return new NavigationResult { data = data.ToArray() };
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