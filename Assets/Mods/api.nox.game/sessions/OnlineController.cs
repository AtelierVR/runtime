using Cysharp.Threading.Tasks;
using Nox.SimplyLibs;
using UnityEngine;

namespace api.nox.game.sessions
{
    public class OnlineController : SessionController
    {
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
                Debug.Log("Enter failed");
                return false;
            }

            if (!enter.IsSuccess)
            {
                Debug.Log("Enter failed: " + enter.Result + " " + enter.Reason);
                return false;
            }

            Debug.Log("Enter success");

            return true;
        }


    }
}