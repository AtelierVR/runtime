using Nox.CCK.Mods;

namespace Nox.CCK.Servers
{
    [System.Serializable]
    public class WellKnownServer : ShareObject
    {
        public string version;
        public string status;
        public uint started_at;
        public string[] features;
    }
}