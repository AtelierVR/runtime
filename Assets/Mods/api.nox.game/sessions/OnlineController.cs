using api.nox.game.Controllers;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Worlds;
using Nox.SimplyLibs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace api.nox.game.sessions
{
    public class OnlineController : SessionController
    {
        public Session _session;
        public Session session
        {
            get => _session;
            set => _session = value;
        }

        internal SimplyMakeRelayConnectionData connectionData;
        private string server;
        private uint instanceId;

        internal SimplyInstance GetInstance() => GameSystem.instance.NetworkAPI.Instance.GetInstanceInCache(server, instanceId);
        internal SimplyRelay GetRelay() => GetInstance().GetRelay();

        public OnlineController(SimplyInstance instance)
        {
            server = instance.server;
            instanceId = instance.id;
        }

        public void Dispose() { }

        public async UniTask<bool> Prepare()
        {
            var relay = GetRelay();
            if (relay == null)
            {
                var result = await GameSystem.instance.NetworkAPI.Relay.MakeConnection(connectionData);
                relay = result.Relay;
            }
            if (relay == null)
            {
                Debug.Log("Relay is null");
                return false;
            }

            var status = await relay.RequestStatus();
            SimplyRelayInstance instance = null;
            foreach (var i in status.Instances)
                if (i.Id == instanceId)
                {
                    instance = i;
                    break;
                }
            if (instance == null)
            {
                Debug.Log("Instance is null");
                return false;
            }
            var enter = await instance.Enter(new SimplyRelayRequestEnter
            {
                DisplayName = GameSystem.instance.NetworkAPI.GetCurrentUser().display,
                Password = connectionData.password,
                Flags = SimplyRelayEnterFlags.None
                    | (instance.Flags.HasFlag(SimplyRelayInstanceFlags.UsePassword) ? SimplyRelayEnterFlags.UsePassword : SimplyRelayEnterFlags.None)
                    | (string.IsNullOrEmpty(connectionData.display_name) ? SimplyRelayEnterFlags.UsePseudonyme : SimplyRelayEnterFlags.None)
            });

            if (enter == null)
            {
                Debug.Log("Enter failed (timeout)");
                return false;
            }

            if (!enter.IsSuccess)
            {
                Debug.Log("Enter failed: " + enter.Result + " " + enter.Reason);
                return false;
            }

            Debug.Log("Enter success");

            var configworld = await instance.RequestConfigWorldData();
            if (configworld == null)
            {
                Debug.Log("ConfigWorldData failed");
                return false;
            }

            var world = await GameSystem.instance.NetworkAPI.World.GetWorld(configworld.Address, configworld.MasterId);
            var search = world != null ? await world.SearchAssets(0, 1,
                configworld.Version == ushort.MaxValue ? null : new uint[] { configworld.Version },
                new string[] { PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform) },
                new string[] { "unity" }
            ) : null;

            var asset = search?.assets[0];
            if (world == null || asset == null)
            {
                Debug.Log("World or Asset is null");
                return false;
            }

            if (!WorldManager.HasWorldInCache(asset.hash))
            {
                var res = await WorldManager.DownloadWorld(asset.hash, asset.url);
                if (!res.success) return false;
            }

            var scene = await WorldManager.LoadWorld(
                asset.hash, 0,
                GameSystem.instance.sessionManager.CurrentSession == null ? LoadSceneMode.Single : LoadSceneMode.Additive
            );

            if (scene == default || !scene.IsValid())
            {
                Debug.Log("Scene is null");
                return false;
            }
            session.scenes.Add(scene);

            var indexMainDescriptor = session.IndexOfMainDescriptor(out var descriptor);

            if (indexMainDescriptor == byte.MaxValue)
            {
                Debug.Log("MainDescriptor is null");
                return false;
            }

            if (descriptor.GetSpawnType() != SpawnType.None)
            {
                var spawn = descriptor.ChoiceSpawn();
                PlayerController.Instance.CurrentController.Teleport(spawn.transform);
            }

            if (!instance.SendConfigReady())
            {
                Debug.Log("SendConfigReady failed");
                return false;
            }

            

            Debug.Log("World loaded");

            return true;
        }


    }
}