using Nox.CCK.Utils;

namespace api.nox.network.Auths
{
    [System.Serializable]
    public class Integrity : INoxObject
    {
        [NoxPublic(NoxAccess.Read)] public string token;
        [NoxPublic(NoxAccess.Read)] public ulong expires;
        [NoxPublic(NoxAccess.Read)] public ulong created_at;

        [NoxPublic(NoxAccess.Method)]
        public bool IsExpired() 
            => (ulong)System.DateTimeOffset.Now.ToUnixTimeMilliseconds() > expires;

        public override string ToString()
            => $"{GetType().Name}[token={token}, expires={expires}, created_at={created_at}]";
    }
}