using System.Linq;
using Newtonsoft.Json.Linq;

namespace Nox.Mods
{
    public class Required : CCK.Mods.Metadata.Required
    {
        public static Required LoadFromJson(JToken json) => new()
        {
            _requirements = json["requirements"].ToObject<string[]>()
        };

        public bool ForClient() => _requirements.Contains("client");
        public bool ForInstance() => _requirements.Contains("instance");

        string[] _requirements;
    }
}