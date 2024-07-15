using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class Server : ShareObject
    {
        public string id;
        public string title;
        public string description;
        public string address;
        public string version;
        public long ready_at;
        public string icon;
        public string public_key;
        public ServerGateway gateways;
    }
}