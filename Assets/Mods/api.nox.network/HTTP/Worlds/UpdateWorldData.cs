using Newtonsoft.Json.Linq;
using Nox.CCK.Mods;

namespace api.nox.network
{
    public class UpdateWorldData : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string server;
        [ShareObjectImport, ShareObjectExport] public uint worldId;
        [ShareObjectImport, ShareObjectExport] public string title;
        [ShareObjectImport, ShareObjectExport] public string description;
        [ShareObjectImport, ShareObjectExport] public ushort capacity;

        internal string ToJSON()
        {
            var obj = new JObject();
            if (!string.IsNullOrEmpty(title)) obj["title"] = title;
            if (!string.IsNullOrEmpty(description)) obj["description"] = description;
            if (capacity > 0) obj["capacity"] = capacity;
            return obj.ToString();
        }
    }
}