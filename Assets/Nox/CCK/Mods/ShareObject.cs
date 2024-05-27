using Newtonsoft.Json.Linq;

namespace Nox.CCK.Mods
{
    public interface ShareObject
    {
        public JObject ToJson() => JObject.FromObject(this);
        public ShareObject FromJson(JObject obj) => obj.ToObject(GetType()) as ShareObject;
    }
}