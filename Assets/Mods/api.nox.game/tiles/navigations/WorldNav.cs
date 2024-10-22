using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK;

namespace api.nox.game.Tiles
{
    internal class WorldNav
    {
        internal NavigationTileManager navigationTile;
        private NavigationHandler _handler;

        public WorldNav(NavigationTileManager navigationTile)
        {
            this.navigationTile = navigationTile;
            GenerateHandler();
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
                    var servers_t = config.Get("servers");
                    if (servers_t == null) return new NavigationWorker[0];
                    var server_d = servers_t.ToObject<Dictionary<string, NavigationWorkerInfo>>();
                    var servers = server_d.Values.ToArray();
                    return servers
                        .Where(x => (x.navigation || x.address == config.Get("server", "")) && x.features.Contains("world"))
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
            var res = await GameClientSystem.Instance.NetworkAPI.World.SearchWorlds(new() { server = server, query = query });
            if (res == null) return new NavigationResult { error = "Error fetching worlds." };
            var data = new List<NavigationResultData>();
            for (var i = 0; i < res.worlds.Length; i++)
            {
                var world = res.worlds[i];
                var asset = await GameClientSystem.Instance.NetworkAPI.World.Asset.SearchAssets(new()
                {
                    server = world.server,
                    world_id = world.id,
                    platforms = new string[] { PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform) },
                    engines = new string[] { "unity" },
                    limit = 1,
                    offset = 0
                });
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