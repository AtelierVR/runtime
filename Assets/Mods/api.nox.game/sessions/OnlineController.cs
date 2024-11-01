using System;
using System.Collections.Generic;
using api.nox.game.Worlds;
using api.nox.network;
using api.nox.network.Instances;
using api.nox.network.RelayInstances;
using api.nox.network.RelayInstances.Enter;
using api.nox.network.RelayInstances.Quit;
using api.nox.network.Relays;
using api.nox.network.Users;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Logger = Nox.CCK.Logger;

namespace api.nox.game.sessions
{
    public class OnlineController : ISessionController
    {
        public string SessionType => "online";
        public Session _session;
        public Session GetSession() => _session;
        internal void SetSession(Session session) => _session = session;
        void ISessionController.SetSession(Session session) => SetSession(session);

        public string GetTitle() => GetInstance().title ?? GetSession().world.title;
        public string GetThumbnail() => GetInstance().thumbnail ?? GetSession().world.thumbnail;
        public string GetDescription() => GetInstance().description ?? GetSession().world.description;

        internal MakeRelayConnectionData connectionData;
        internal string Server { get; private set; }
        internal uint InstanceId { get; private set; }
        internal ushort InternalId { get; private set; }
        private bool isReady = false;

        private byte MaxTps = 4;

        internal Instance GetInstance() => NetCache.Get<Instance>(Instance.GetCacheKey(InstanceId, Server));
        internal Relay GetRelay() => GetInstance()?.GetRelay();
        internal RelayInstance GetRelayInstance() => GetRelay() != null ? RelayInstanceManager.Get(InternalId, GetRelay().Id) : null;
        internal OnlineController(Instance instance)
        {
            Server = instance.server;
            InstanceId = instance.id;
        }

        private DateTime lastUpdate = DateTime.Now;

        public void Update()
        {
            var relayinstance = GetRelayInstance();
            var session = GetSession();
            if (isReady && (DateTime.Now - lastUpdate).TotalMilliseconds > 1000 / MaxTps)
            {
                lastUpdate = DateTime.Now;
                foreach (var player in session.abstractPlayers)
                {
                    player.TickUpdate();
                    foreach (var part in player.GetParts())
                        if (part != null && part.Transform != null) // && part.Transform.deleveryType == TransformDeleveryType.LocalModified
                        {
                            relayinstance.SendTransform(new(player.GetId(), part));
                            part.Transform.deleveryType = TransformDeleveryType.None;
                        }
                }
            }
        }

        public async UniTask Close()
        {
            var relayinstance = GetRelayInstance();
            if (relayinstance != null)
            {
                await relayinstance.Quit(QuitType.Normal);
                RelayInstanceManager.Remove(relayinstance);
            }
            var session = GetSession();
            if (session != null)
            {
                foreach (var player in session.abstractPlayers)
                    player.Unregister();
                session.abstractPlayers.Clear();
                await Worlds.WorldManager.UnloadScene(session.worldAsset.hash, 0);
            }
            var relay = GetRelay();
            if (relay != null)
            {
                List<OnlineController> controllers = new();
                foreach (var s in SessionManager.Instance.GetSessionsWithController<OnlineController>())
                    if (!controllers.Contains(s.Controller as OnlineController))
                        controllers.Add(s.Controller as OnlineController);
                if (controllers.Count < 2)
                    relay.Dispose();
            }
        }


        public async UniTask<bool> Prepare()
        {
            var relay = GetRelay();
            if (relay == null)
            {
                var result = await GameSystem.Instance.NetworkAPI.Relay.MakeConnection(connectionData);
                relay = result.Relay;
            }
            if (relay == null)
            {
                Logger.Log("Relay is null");
                return false;
            }

            0d.ToString("0.000");

            var status = await relay.RequestStatus();
            if (status == null)
            {
                Logger.Log("Status is null");
                return false;
            }
            RelayInstance relayinstance = null;
            foreach (var i in status.Instances)
                if (i.Id == InstanceId)
                {
                    relayinstance = i;
                    break;
                }
            if (relayinstance == null)
            {
                Logger.Log("RelayInstance is null");
                return false;
            }

            InternalId = relayinstance.InternalId;

            var enter = await relayinstance.Enter(new RequestEnter
            {
                DisplayName = GameSystem.Instance.NetworkAPI.User.CurrentUser.display,
                Password = connectionData.password,
                Flags = EnterFlags.None
                    | (relayinstance.Flags.HasFlag(InstanceFlags.UsePassword) ? EnterFlags.UsePassword : EnterFlags.None)
                    | (string.IsNullOrEmpty(connectionData.display_name) ? EnterFlags.UsePseudonyme : EnterFlags.None)
            });

            if (enter == null)
            {
                Logger.Log("Enter failed (timeout)");
                return false;
            }

            MaxTps = enter.MaxTps;

            Logger.Log("Enter success");

            if (!enter.IsSuccess)
            {
                Logger.Log("Enter failed: " + enter.Result + " " + enter.Reason);
                return false;
            }

            Logger.Log("Enter success");

            var configworld = await relayinstance.RequestConfigWorldData();
            if (configworld == null)
            {
                Logger.Log("ConfigWorldData failed");
                await relayinstance.Quit(QuitType.ConfigurationError, "ConfigWorldData failed");
                return false;
            }

            var world = await GameSystem.Instance.NetworkAPI.World.GetWorld(configworld.Address, configworld.MasterId);
            Logger.Log("World: " + world);

            if (world == null)
            {
                Logger.Log("World is null");
                await relayinstance.Quit(QuitType.ConfigurationError, "No world found");
                return false;
            }

            var search = await GameSystem.Instance.NetworkAPI.World.Asset.SearchAssets(new()
            {
                server = world.server,
                world_id = world.id,
                versions = configworld.Version == ushort.MaxValue ? null : new ushort[] { configworld.Version },
                platforms = new string[] { PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform) },
                engines = new string[] { "unity" },
                limit = 1,
                offset = 0
            });

            var asset = search?.assets[0];
            if (asset == null)
            {
                Logger.Log("Asset is null");
                await relayinstance.Quit(QuitType.ConfigurationError, "No world asset found");
                return false;
            }

            if (!WorldCache.HasWorldInCache(asset.hash))
            {
                var res = await WorldCache.DownloadWorld(asset.hash, asset.url);
                if (!res.success) return false;
            }

            var scene_result = await WorldManager.LoadScene(new() { 
                hash = asset.hash, 
                id = 0,
                mode = LoadSceneMode.Additive
            });

            if (!scene_result.success)
            {
                Logger.Log("LoadScene failed: " + scene_result.error);
                await relayinstance.Quit(QuitType.ConfigurationError, "Loadscene failed" + scene_result.error);
                return false;
            }

            var scene = scene_result.scene;
            var session = GetSession();

            var wlock = new WorldLock()
            {
                hash = asset.hash,
                id = (uint)(DateTime.Now.Ticks % uint.MaxValue),
                scenes = new() { new() { index = 0, scene = scene } }
            };

            WorldManager.LockWorlds.Add(wlock);
            session.worldLock = wlock;

            var wh = WorldHidden.Make(scene);
            Logger.Log("Scene: " + scene);
            Logger.Log("WorldHidden: " + wh);
            wh.Set(false);

            var desc = MainDescriptor.GetDescriptor(scene);
            if (desc == null || desc.GetType() != typeof(MainDescriptor))
            {
                Logger.Log("Descriptor is null or not MainDescriptor");
                await relayinstance.Quit(QuitType.ConfigurationError, "Descriptor is null or not MainDescriptor");
                return false;
            }

            var descriptor = desc as MainDescriptor;
            descriptor.SetCustom("world_hidden", new byte[] { 0 });

            if (!relayinstance.SendConfigReady())
            {
                Logger.Log("SendConfigReady failed");
                await relayinstance.Quit(QuitType.Timeout, "SendConfigReady failed");
                return false;
            }

            RegisterPlayer(enter.Player);

            var abstractPlayer = session.GetAbstractPlayer(enter.Player.Id);
            if (abstractPlayer == null)
            {
                Logger.Log("AbstractPlayer is null");
                await relayinstance.Quit(QuitType.UnknowError, "AbstractPlayer is null");
                return false;
            }

            if (descriptor != null && descriptor.GetSpawnType() != SpawnType.None)
            {
                var spawn = descriptor.ChoiceSpawn();
                if (spawn != null)
                    abstractPlayer.Teleport(spawn.transform);
                Logger.Log("Teleport success");
            }

            isReady = true;

            return true;
        }

        public void RegisterPlayer(NetPlayer player)
            => new NetAbstractPlayer(player, GetSession()).Register();

        public void UnRegisterPlayer(NetPlayer player)
            => GetSession().GetAbstractPlayer(player.Id)?.Unregister();

    }
}