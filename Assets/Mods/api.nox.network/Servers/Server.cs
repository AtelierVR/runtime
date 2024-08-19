using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class Server : ShareObject
    {
        [ShareObjectExport] public string id;
        [ShareObjectExport] public string title;
        [ShareObjectExport] public string description;
        [ShareObjectExport] public string address;
        [ShareObjectExport] public string version;
        [ShareObjectExport] public long ready_at;
        [ShareObjectExport] public string icon;
        [ShareObjectExport] public string public_key;
        [ShareObjectExport] public ServerGateway gateways;
        [ShareObjectExport] public string[] features;
    }
}