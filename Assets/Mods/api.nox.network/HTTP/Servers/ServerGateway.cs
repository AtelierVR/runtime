using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class ServerGateway : ShareObject
    {
        [ShareObjectExport] public string http;
        [ShareObjectExport] public string ws;
        [ShareObjectExport] public string proxy;
        [ShareObjectExport] public string cdn;
    }
}