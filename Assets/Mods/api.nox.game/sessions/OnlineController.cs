using api.nox.game.Controllers;
using api.nox.network;
using api.nox.network.Instances;
using api.nox.network.RelayInstances;
using api.nox.network.RelayInstances.Enter;
using api.nox.network.Relays;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Worlds;
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

        internal MakeRelayConnectionData connectionData;
        private string server;
        private uint instanceId;
        private bool isReady = false;

        internal Instance GetInstance() => NetCache.Get<Instance>($"instance.{server}.{instanceId}");
        internal Relay GetRelay() => GetInstance()?.GetRelay();

        public OnlineController(Instance instance)
        {
            server = instance.server;
            instanceId = instance.id;
        }

        public void Update()
        {
            var player = PlayerController.Instance.CurrentController;
            if (isReady && player != null)
            {

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
            RelayInstance instance = null;
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
            var enter = await instance.Enter(new RequestEnter
            {
                DisplayName = GameSystem.Instance.NetworkAPI.User.CurrentUser.display,
                Password = connectionData.password,
                Flags = EnterFlags.None
                    | (instance.Flags.HasFlag(InstanceFlags.UsePassword) ? EnterFlags.UsePassword : EnterFlags.None)
                    | (string.IsNullOrEmpty(connectionData.display_name) ? EnterFlags.UsePseudonyme : EnterFlags.None)
            });

            if (enter == null)
            {
                Debug.Log("Enter failed (timeout)");
                return false;
            }

            Debug.Log("Enter success");

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

            var world = await GameSystem.Instance.NetworkAPI.World.GetWorld(configworld.Address, configworld.MasterId);
            var search = world != null ? await GameSystem.Instance.NetworkAPI.World.Asset.SearchAssets(new() {
                versions = configworld.Version == ushort.MaxValue ? null : new ushort[] { configworld.Version },
                platforms = new string[] { PlatfromExtensions.GetPlatformName(Constants.CurrentPlatform) },
                engines = new string[] { "unity" },
                limit = 1,
                offset = 0
            }) : null;
            

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
                GameSystem.Instance.SessionManager.CurrentSession == null ? LoadSceneMode.Single : LoadSceneMode.Additive
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

            // var player = new LocalPlayer(null);
            // session.SpawnPlayer(player);

            if (descriptor.GetSpawnType() != SpawnType.None)
            {
                var spawn = descriptor.ChoiceSpawn();

                Debug.Log("Teleporting to spawn");
                try
                {
                    PlayerController.Instance.CurrentController.Teleport(spawn.transform);
                }
                catch (System.Exception e) { Debug.LogWarning(e); }
            }


            Debug.Log("Player spawned");

            if (!instance.SendConfigReady())
            {
                Debug.Log("SendConfigReady failed");
                return false;
            }



            isReady = true;

            return true;
        }


    }
}