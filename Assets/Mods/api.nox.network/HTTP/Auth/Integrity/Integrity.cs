namespace api.nox.network.Auths
{
    [System.Serializable]
    public class Integrity
    {
        public string token;
        public ulong expires;
        public ulong created_at;

        public bool IsExpirated() => (ulong)System.DateTimeOffset.Now.ToUnixTimeMilliseconds() > expires;
    }
}