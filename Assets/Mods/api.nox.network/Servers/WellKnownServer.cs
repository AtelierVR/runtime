using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class WellKnownServer : ShareObject
    {
        [ShareObjectExport] public string version;
        [ShareObjectExport] public string status;
        [ShareObjectExport] public uint started_at;
       [ShareObjectExport]  public string[] features;
    }
}