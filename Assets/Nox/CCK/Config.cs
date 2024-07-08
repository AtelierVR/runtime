using System.IO;
using Newtonsoft.Json.Linq;

namespace Nox.CCK
{
    public class Config
    {
        public static string GetPath() => Path.Combine(Constants.GameAppDataPath, "config.json");
        public static Config Current;

        private JObject _jsonObject = new JObject();

        public static Config Load(bool force = false)
        {
            if (Current != null && !force) return Current;
            if (!File.Exists(GetPath()))
                return new Config().Save();
            var jsonString = File.ReadAllText(GetPath());
            var config = new Config() { _jsonObject = JObject.Parse(jsonString) };
            Current = config;
            return config;
        }

        public bool Has(string propertyName)
        {
            var split = propertyName.Split('.');
            var current = _jsonObject;
            for (var i = 0; i < split.Length - 1; i++)
            {
                if (current[split[i]] == null)
                    return false;
                current = (JObject)current[split[i]];
            }
            return current[split[^1]] != null;
        }

        public T Get<T>(string propertyName, T defaultValue = default(T))
        {
            var split = propertyName.Split('.');
            var current = _jsonObject;
            for (var i = 0; i < split.Length - 1; i++)
            {
                if (current[split[i]] == null)
                    return defaultValue;
                current = (JObject)current[split[i]];
            }
            if (current[split[^1]] == null)
                return defaultValue;
            return current[split[^1]].ToObject<T>();
        }

        public void Set<T>(string propertyName, T value)
        {
            var split = propertyName.Split('.');
            var current = _jsonObject;
            for (var i = 0; i < split.Length - 1; i++)
            {
                if (current[split[i]] == null)
                    current[split[i]] = new JObject();
                current = (JObject)current[split[i]];
            }
            current[split[^1]] = JToken.FromObject(value);
        }

        public void Remove(string propertyName)
        {
            var split = propertyName.Split('.');
            var current = _jsonObject;
            for (var i = 0; i < split.Length - 1; i++)
            {
                if (current[split[i]] == null)
                    return;
                current = (JObject)current[split[i]];
            }
            current.Remove(split[^1]);
        }

        public Config Save()
        {
            File.WriteAllText(GetPath(), _jsonObject.ToString());
            return this;
        }
    }
}