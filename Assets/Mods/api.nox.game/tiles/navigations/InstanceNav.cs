using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.SimplyLibs;
using UnityEngine;

namespace api.nox.game
{
    internal class InstanceNav
    {
        internal NavigationTileManager navigationTile;
        private NavigationHandler _handler;

        public InstanceNav(NavigationTileManager navigationTile)
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
                id = "api.nox.game.navigation.instance",
                text_key = "dashboard.navigation.instance",
                title_key = "dashboard.navigation.instance.title",
                GetWorkers = () =>
                {
                    var config = Config.Load();
                    var servers = config.Get("navigation.servers", new WorkerInfo[0]);
                    return servers
                        .Where(x => x.features.Contains("instance"))
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
            var res = await navigationTile.clientMod.NetworkAPI.Instance.SearchInstances(server, query);
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
            var WorldAPI = navigationTile.clientMod.NetworkAPI.World;
            foreach (var address in iww.GroupBy(x => x.worldServer).ToDictionary(x => x.Key, x => x.Select(y => y.worldId)))
            {
                var resWorld = await WorldAPI.GetWorlds(address.Key, address.Value.ToArray().ToArray());
                if (resWorld == null) continue;
                foreach (var i in iww.Where(x => x.worldServer == address.Key))
                    i.world = resWorld.FirstOrDefault(x => x.id == i.worldId);
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
                    goto_data = new object[] { x.instance }
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

    internal class InstanceWithWorld
    {
        public uint worldId;
        public string worldServer;
        public SimplyInstance instance;
        public SimplyWorld world;
    }
}