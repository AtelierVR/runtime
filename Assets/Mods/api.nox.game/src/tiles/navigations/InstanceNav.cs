using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.network.Instances;
using api.nox.network.Worlds;
using api.nox.network.Worlds.Assets;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.game.Tiles
{
    internal class InstanceNav
    {
        private NavigationHandler _handler;

        public InstanceNav()
        {
            _handler = new NavigationHandler
            {
                id = "api.nox.game.navigation.instance",
                text_key = "dashboard.navigation.instance",
                title_key = "dashboard.navigation.instance.title",
                icon = GameClientSystem.CoreAPI.AssetAPI.GetAsset<Texture2D>("icons/instance.png"),
                GetWorkers = () =>
                {
                    var config = Config.Load();
                    var serversT = config.Get("servers");
                    if (serversT == null) return Array.Empty<NavigationWorker>();
                    var serverD = serversT.ToObject<Dictionary<string, NavigationWorkerInfo>>();
                    var servers = serverD.Values.ToArray();
                    return servers
                        .Where(x => (x.navigation || x.address == config.Get("server", "")) &&
                                    x.features.Contains("instance"))
                        .Select(x => new NavigationWorker
                        {
                            server_address = x.address,
                            server_title = x.title,
                            Fetch = async query => await FetchInstances(x.address, query)
                        }).ToArray();
                }
            };
        }

        private async UniTask<NavigationResult> FetchInstances(string server, string query)
        {
            Logger.Log("Fetching instances");
            var res = await GameClientSystem.NetworkAPI
                .GetField<INoxObject>("Instance")
                .CallMethod<UniTask<INoxObject>>("SearchInstances", new Dictionary<string, object>
                {
                    { "query", query }, { "server", server }
                });

            if (res == null) return new NavigationResult { error = "Error fetching instances." };
            var instances = res.CallMethod<Instance[]>("GetInstances");
            Logger.Log("Fetched instances " + instances.Length);

            List<InstanceWithWorld> iww = new();
            foreach (var instance in instances)
            {
                var split = instance.world.Split('@');
                if (split.Length < 1 || string.IsNullOrEmpty(split[0])) continue;
                var address = split.Length == 1 ? instance.server : split[1];
                iww.Add(new InstanceWithWorld
                {
                    WorldId = uint.Parse(split[0]),
                    WorldServer = address,
                    Instance = instance
                });
            }

            var worldAPI = GameClientSystem.NetworkAPI.GetField<INoxObject>("World");
            foreach (var address in iww.GroupBy(x => x.WorldServer)
                         .ToDictionary(x => x.Key, x => x.Select(y => y.WorldId)))
            {
                var resWorld = await worldAPI.CallMethod<UniTask<INoxObject>>("SearchWorlds",
                    new Dictionary<string, object>()
                    {
                        { "server", address.Key },
                        { "world_ids", address.Value.ToArray() }
                    });
                if (resWorld == null) continue;
                foreach (var i in iww.Where(x => x.WorldServer == address.Key))
                    i.World = resWorld.GetField<World[]>("GetWorlds").FirstOrDefault(x => x.id == i.WorldId);
            }

            foreach (var i in iww.Where(x => x.World != null))
            {
                var resAsset = await worldAPI.GetField<INoxObject>("Asset")
                    .CallMethod<UniTask<INoxObject>>("SearchAssets", new Dictionary<string, object>
                    {
                        { "server", i.WorldServer },
                        { "world_id", i.WorldId },
                        { "offset", 0 },
                        { "limit", 1 },
                        { "platforms", new[] { Constants.CurrentPlatform.GetPlatformName() } },
                        { "engines", new[] { "unity" } }
                    });
                if (resAsset == null) continue;
                var assets = resAsset.CallMethod<WorldAsset[]>("GetAssets");
                if (assets.Length == 0) continue;
                i.Asset = assets[0];
            }

            return new NavigationResult
            {
                data = iww.Select(x => new NavigationResultData
                {
                    title = string.IsNullOrEmpty(x.Instance.title)
                        ? string.IsNullOrEmpty(x.World?.title) ? x.Instance.name : x.World.title
                        : x.Instance.title,
                    imageUrl = x.World?.thumbnail,
                    goto_id = "game.instance",
                    goto_data = new object[] { x.Instance, x.World, x.Asset }
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
        public uint WorldId;
        public uint WorldAssetId;
        public string WorldServer;
        public Instance Instance;
        public World World;
        public WorldAsset Asset;
    }
}