using Newtonsoft.Json.Linq;
using Nox.CCK;

namespace Nox.Mods
{
    public class Platform : CCK.Mods.Metadata.Platform
    {
        public static Platform LoadFromJson(JToken json) => new()
        {
            _name = PlatfromExtensions.GetPlatformFromName(json["name"].Value<string>()),
            _version = json["version"].Value<string>()
        };

        public Platfrom GetPlatfrom() => _name;
        public string GetVersion() => _version;

        private Platfrom _name;
        private string _version;
    }
}