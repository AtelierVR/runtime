using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Nox.Mods
{
    public class Person : CCK.Mods.Metadata.Person
    {
        public static Person LoadFromJson(JToken json) => new()
        {
            _name = json["name"].ToString(),
            _email = json["email"].ToString(),
            _website = json["website"].ToString(),
            _customs = json.ToObject<Dictionary<string, object>>()
        };

        public string GetName() => _name;
        public string GetEmail() => _email;
        public string GetWebsite() => _website;
        public T Get<T>(string key) where T : class => Has<T>(key) ? _customs[key] as T : null;
        public bool Has<T>(string key) where T : class => _customs.ContainsKey(key);
        public Dictionary<string, object> GetAll() => _customs;

        private string _name;
        private string _email;
        private string _website;
        private Dictionary<string, object> _customs;
    }
}