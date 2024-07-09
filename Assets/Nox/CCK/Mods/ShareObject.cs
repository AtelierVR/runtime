using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Nox.CCK.Mods
{
    public interface ShareObject
    {
        public JObject ToJson() => JObject.FromObject(this);
        public ShareObject FromJson(JObject obj) => obj.ToObject(GetType()) as ShareObject;

        private Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> dict = new();
            foreach (var prop in GetType().GetFields())
                dict[prop.Name] = prop.GetValue(this);
            return dict;
        }

        private void SetValues(Dictionary<string, object> dict)
        {
            foreach (var prop in GetType().GetFields())
                if (dict.ContainsKey(prop.Name))
                    prop.SetValue(this, dict[prop.Name]);
        }

        public T Convert<T>() where T : ShareObject
        {
            T obj = (T)System.Activator.CreateInstance(typeof(T));
            obj.SetValues(ToDictionary());
            return obj;
        }
    }
}