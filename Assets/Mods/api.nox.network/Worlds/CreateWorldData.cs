using Newtonsoft.Json.Linq;
using Nox.CCK.Mods;

namespace api.nox.network
{
    public class CreateWorldData : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string server;
        [ShareObjectImport, ShareObjectExport] public uint id;
        [ShareObjectImport, ShareObjectExport] public string title;
        [ShareObjectImport, ShareObjectExport] public string description;
        [ShareObjectImport, ShareObjectExport] public ushort capacity;
        [ShareObjectImport, ShareObjectExport] public string thumbnail;

        internal string ToJSON()
        {
            var obj = new JObject();
            if (id > 0) obj["id"] = id;
            if (!string.IsNullOrEmpty(title)) obj["title"] = title;
            if (!string.IsNullOrEmpty(description)) obj["description"] = description;
            if (capacity > 0) obj["capacity"] = capacity;
            if (!string.IsNullOrEmpty(thumbnail)) obj["thumbnail"] = thumbnail;
            return obj.ToString();
        }
    }
}