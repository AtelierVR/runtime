using System;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;

namespace api.nox.network
{
    public class AuthAPI : ShareObject
    {
        internal NetworkSystem _netSystem;

        public AuthAPI(NetworkSystem netSystem)
        {
            _netSystem = netSystem;
        }

        public async UniTask<AuthToken> GetToken(string address)
        {
            var config = Config.Load();
            if (_netSystem.GetCurrentUser()?.server == address)
            {
                if (config.Has("token"))
                    return new AuthToken() { token = config.Get<string>("token"), isIntegrity = false };
                return null;
            }

            if (config.Has($"servers.{address}.integrity.token"))
            {
                var token = config.Get($"servers.{address}.integrity.expires", ulong.MinValue);
                if (token != ulong.MinValue && token > (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds())
                    return new AuthToken() { token = config.Get<string>($"servers.{address}.integrity.token"), isIntegrity = true };
            }

            var result = await _netSystem._user.CreateIntegrity(address);
            if (result != null && !result.IsExpirated()){
                return new AuthToken() { token = result.token, isIntegrity = true };}

            return null;
        }

        [ShareObjectExport] public Func<string, UniTask<ShareObject>> SharedGetToken;

        public void BeforeExport()
        {
            SharedGetToken = async (address) => await GetToken(address);
        }

        public void AfterExport()
        {
            SharedGetToken = null;
        }
    }
}