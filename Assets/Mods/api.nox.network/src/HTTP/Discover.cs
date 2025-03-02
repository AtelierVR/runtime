using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.network.HTTP
{
    public class Discover
    {
        public static async UniTask<string> GetGateway(string server)
        {
            var config = Config.Load();

            // Check if gateway is already known
            var gateway = config.Get<string>(new []{"servers", server, "gateway"});
            if (gateway != null) return gateway;

            // Check if user is logged in, and if the server is the same
            if (NetworkSystem.ModInstance.Auth.GetCurrentServerAddress() == server)
            {
                gateway = config.Get<string>("gateway");
                if (gateway != null) return gateway;
            }

            // Find gateway
            var req = await Gateway.FindGatewayMaster(server);
            if (req == null) return null;
            config.Set(new []{"servers", server, "gateway"}, req.OriginalString);
            config.Save();

            return req.OriginalString;
        }
    }
}