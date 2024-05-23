using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Nox.Mods
{
    public class Contact : CCK.Mods.Metadata.Contact
    {
        public static Contact LoadFromJson(JToken json) => new()
        {
            _customs = json.ToObject<Dictionary<string, object>>()
        };

        public T Get<T>(string key) where T : class => Has<T>(key) ? _customs[key] as T : null;
        public bool Has<T>(string key) where T : class => _customs.ContainsKey(key);
        public Dictionary<string, object> GetAll() => _customs;

        private Dictionary<string, object> _customs;
    }
}