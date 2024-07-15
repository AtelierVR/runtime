using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyServer : ShareObject
    {
        public string id;
        public string title;
        public string description;
        public string address;
        public string version;
        public long ready_at;
        public string icon;
        public string public_key;
    }
}