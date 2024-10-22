using System.Collections.Generic;
using System.Linq;
using api.nox.network.Instances;
using api.nox.network.Worlds;
using api.nox.network.Worlds.Assets;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;

namespace api.nox.game.Tiles
{
    internal class InstanceNav
    {
        internal NavigationTileManager navigationTile;
        private NavigationHandler _handler;

        public InstanceNav(NavigationTileManager navigationTile)
        {
            this.navigationTile = navigationTile;
            GenerateHandler();
        }

        private void GenerateHandler()
        {
            _handler = new NavigationHandler
            {
                id = "api.nox.game.navigation.instance",
                text_key = "dashboard.navigation.instance",
                title_key = "dashboard.navigation.instance.title",
                GetWorkers = () =>
                {
                    var config = Config.Load();
                    var servers_t = config.Get("servers");
                    if (servers_t == null) return new NavigationWorker[0];
                    var server_d = servers_t.ToObject<Dictionary<string, NavigationWorkerInfo>>();
                    var servers = server_d.Values.ToArray();
                    return servers
                        .Where(x => (x.navigation || x.address == config.Get("server", "")) && x.features.Contains("instance"))
                        .Select(x => new NavigationWorker
                        {
                            server_address = x.address,
                            server_title = x.title,
                            Fetch = async (string query) => await FetchInstances(x.address, query)
                        }).ToArray();
                }
            };
        }

        private async UniTask<NavigationResult> FetchInstances(string server, string query)
        {
            Debug.Log("Fetching instances");
            var res = await GameClientSystem.Instance.NetworkAPI.Instance.SearchInstances(new() { query = query, server = server });
            if (res == null) return new NavigationResult { error = "Error fetching instances." };
            Debug.Log("Fetched instances " + res.instances.Length);

            List<InstanceWithWorld> iww = new();
            foreach (var instance in res.instances)
            {
                var spli = instance.world.Split('@');
                if (spli.Length < 1 || string.IsNullOrEmpty(spli[0])) continue;
                var address = spli.Length == 1 ? instance.server : spli[1];
                iww.Add(new InstanceWithWorld
                {
                    worldId = uint.Parse(spli[0]),
                    worldServer = address,
                    instance = instance
                });
            }

            var WorldAPI = GameClientSystem.Instance.NetworkAPI.World;
            foreach (var address in iww.GroupBy(x => x.worldServer).ToDictionary(x => x.Key, x => x.Select(y => y.worldId)))
            {
                var resWorld = await GameClientSystem.Instance.NetworkAPI.World.SearchWorlds(new() { server = address.Key, world_ids = address.Value.ToArray() });
                if (resWorld == null) continue;
                foreach (var i in iww.Where(x => x.worldServer == address.Key))
                    i.world = resWorld.worlds.FirstOrDefault(x => x.id == i.worldId);
            }

            foreach (var i in iww.Where(x => x.world != null))
            {
                var resAsset = await GameClientSystem.Instance.NetworkAPI.World.Asset.SearchAssets(new()
                {
                    server = i.worldServer,
                    world_id = i.worldId,
                    offset = 0,
                    limit = 1,
                    platforms = new string[] { PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform) },
                    engines = new string[] { "unity" }

                });
                if (resAsset == null || resAsset.assets.Length == 0) continue;
                i.asset = resAsset.assets[0];
            }

            return new NavigationResult
            {
                data = iww.Select(x => new NavigationResultData
                {
                    title = string.IsNullOrEmpty(x.instance.title) ? (
                        string.IsNullOrEmpty(x.world?.title) ? x.instance.name : x.world.title
                    ) : x.instance.title,
                    imageUrl = x.world?.thumbnail,
                    goto_id = "game.instance",
                    goto_data = new object[] { x.instance, x.world, x.asset }
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

    internal class InstanceWithWorld
    {
        public uint worldId;
        public uint worldAssetId;
        public string worldServer;
        public Instance instance;
        public World world;
        public WorldAsset asset;
    }
}