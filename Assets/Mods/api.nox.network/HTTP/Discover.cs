using System;
using System.Threading.Tasks;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;

namespace api.nox.network.HTTP
{
    public class Discover
    {
        public static async UniTask<string> GetGateway(string server)
        {
            var config = Config.Load();

            // Check if gateway is already known
            var hash = Animator.StringToHash(server);
            var gateway = config.Get<string>($"servers.{hash}.gateway");
            if (gateway != null) return gateway;

            // Check if user is logged in, and if the server is the same
            if (NetworkSystem.ModInstance.Auth.CurrentServerAddress == server)
            {
                gateway = config.Get<string>("gateway");
                if (gateway != null) return gateway;
            }

            // Find gateway
            var req = await Gateway.FindGatewayMaster(server);
            if (req == null) return null;
            config.Set($"servers.{hash}.gateway", req.OriginalString);
            config.Save();

            return req.OriginalString;
        }
    }
}