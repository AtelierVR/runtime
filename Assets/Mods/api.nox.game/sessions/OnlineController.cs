using System;
using api.nox.game.Controllers;
using api.nox.network;
using api.nox.network.Instances;
using api.nox.network.Players;
using api.nox.network.RelayInstances;
using api.nox.network.RelayInstances.Enter;
using api.nox.network.Relays;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Worlds;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace api.nox.game.sessions
{
    public class OnlineController : ISessionController
    {
        public Session _session;
        public Session GetSession() => _session;
        internal void SetSession(Session session) => _session = session;
        void ISessionController.SetSession(Session session) => SetSession(session);

        internal MakeRelayConnectionData connectionData;
        internal string Server { get; private set; }
        internal uint InstanceId { get; private set; }
        internal ushort InternalId { get; private set; }
        private bool isReady = false;

        private byte MaxTps = 4;

        internal Instance GetInstance() => NetCache.Get<Instance>(Instance.GetCacheKey(InstanceId, Server));
        internal Relay GetRelay() => GetInstance()?.GetRelay();
        internal RelayInstance GetRelayInstance() => RelayInstanceManager.Get(InternalId, GetRelay().Id);
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

        public void Dispose() { }

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
                Debug.Log("Relay is null");
                return false;
            }

            var status = await relay.RequestStatus();
            RelayInstance relayinstance = null;
            foreach (var i in status.Instances)
                if (i.Id == InstanceId)
                {
                    relayinstance = i;
                    break;
                }
            if (relayinstance == null)
            {
                Debug.Log("RelayInstance is null");
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
                Debug.Log("Enter failed (timeout)");
                return false;
            }

            MaxTps = enter.MaxTps;

            Debug.Log("Enter success");

            if (!enter.IsSuccess)
            {
                Debug.Log("Enter failed: " + enter.Result + " " + enter.Reason);
                return false;
            }

            Debug.Log("Enter success");

            var configworld = await relayinstance.RequestConfigWorldData();
            if (configworld == null)
            {
                Debug.Log("ConfigWorldData failed");
                return false;
            }

            var world = await GameSystem.Instance.NetworkAPI.World.GetWorld(configworld.Address, configworld.MasterId);
            Debug.Log("World: " + world);

            if (world == null)
            {
                Debug.Log("World is null");
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
                Debug.Log("Asset is null");
                return false;
            }

            if (!WorldManager.HasWorldInCache(asset.hash))
            {
                var res = await WorldManager.DownloadWorld(asset.hash, asset.url);
                if (!res.success) return false;
            }

            var scene = await WorldManager.LoadWorld(
                asset.hash, 0,
                GameSystem.Instance.SessionManager.CurrentSession == null ? LoadSceneMode.Single : LoadSceneMode.Additive
            );

            if (scene == default || !scene.IsValid())
            {
                Debug.Log("Scene is null");
                return false;
            }
            GetSession().scenes.Add(scene);

            var indexMainDescriptor = GetSession().IndexOfMainDescriptor(out var descriptor);

            if (indexMainDescriptor == byte.MaxValue)
            {
                Debug.Log("MainDescriptor is null");
                return false;
            }

            if (!relayinstance.SendConfigReady())
            {
                Debug.Log("SendConfigReady failed");
                return false;
            }

            RegisterPlayer(enter.Player);

            var abstractPlayer = GetSession().GetAbstractPlayer(enter.Player.Id);
            if (abstractPlayer == null)
            {
                Debug.Log("AbstractPlayer is null");
                return false;
            }

            if (descriptor != null && descriptor.SpawnType != SpawnType.None)
            {
                var spawn = descriptor.ChoiceSpawn();
                if (spawn != null)
                    abstractPlayer.Teleport(spawn.transform);
                Debug.Log("Teleport success");
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