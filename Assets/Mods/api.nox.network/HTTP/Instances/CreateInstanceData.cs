using Newtonsoft.Json.Linq;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.network
{
    public class CreateInstanceData : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string server;

        [ShareObjectImport, ShareObjectExport] public string name;
        [ShareObjectImport, ShareObjectExport] public string title;
        [ShareObjectImport, ShareObjectExport] public string description;
        [ShareObjectImport, ShareObjectExport] public string expose;
        [ShareObjectImport, ShareObjectExport] public string world;
        [ShareObjectImport, ShareObjectExport] public ushort capacity;
        [ShareObjectImport, ShareObjectExport] public bool use_password;
        [ShareObjectImport, ShareObjectExport] public string password;
        [ShareObjectImport, ShareObjectExport] public bool use_whitelist;
        [ShareObjectImport, ShareObjectExport] public string[] whitelist;

        internal string ToJSON()
        {
            var obj = new JObject();
            if (!string.IsNullOrEmpty(name)) obj["name"] = name;
            if (!string.IsNullOrEmpty(title)) obj["title"] = title;
            if (!string.IsNullOrEmpty(description)) obj["description"] = description;
            obj["expose"] = expose;
            obj["world"] = world;
            obj["capacity"] = capacity;
            obj["use_password"] = use_password;
            if (string.IsNullOrEmpty(password)) obj["password"] = password;
            obj["use_whitelist"] = use_whitelist;
            if (whitelist != null) obj["whitelist"] = JArray.FromObject(whitelist);
            Debug.Log(obj.ToString());
            return obj.ToString();
        }
    }
}