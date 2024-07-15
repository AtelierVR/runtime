using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class ServerGateway : ShareObject
    {
        public string http;
        public string ws;
        public string proxy;
        public string cdn;
    }
}