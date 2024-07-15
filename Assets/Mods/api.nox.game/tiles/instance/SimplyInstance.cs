using Nox.CCK.Mods;

namespace api.nox.game
{
    [System.Serializable]
    public class SimplyInstance : ShareObject
    {
        public uint id;
        public string title;
        public string description;
        public string server;
        public string name;
        public ushort capacity;
        public string[] tags;
        public string world;
    }
}