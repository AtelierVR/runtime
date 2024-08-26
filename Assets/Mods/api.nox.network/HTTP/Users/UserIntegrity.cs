using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class UserIntegrity : ShareObject
    {
        public string token;
        public ulong expires;
        public ulong created_at;

        public bool IsExpirated() => (ulong)System.DateTimeOffset.Now.ToUnixTimeMilliseconds() > expires;
    }
}