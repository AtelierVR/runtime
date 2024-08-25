using System;
using Newtonsoft.Json.Linq;
using Nox.CCK.Mods;

namespace api.nox.network
{
    public class CreateAssetData : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string server;
        [ShareObjectImport, ShareObjectExport] public uint worldId;
        [ShareObjectImport, ShareObjectExport] public uint id;
        [ShareObjectImport, ShareObjectExport] public ushort version;
        [ShareObjectImport, ShareObjectExport] public string engine;
        [ShareObjectImport, ShareObjectExport] public string platform;
        [ShareObjectImport, ShareObjectExport] public string url;
        [ShareObjectImport, ShareObjectExport] public string hash;
        [ShareObjectImport, ShareObjectExport] public uint size;

        internal string ToJSON()
        {
            var obj = new JObject();
            if (id > 0) obj["id"] = id;
            obj["version"] = version;
            obj["engine"] = engine;
            obj["platform"] = platform;
            if (!string.IsNullOrEmpty(url)) obj["url"] = url;
            if (!string.IsNullOrEmpty(hash)) obj["hash"] = hash;
            if (size > 0) obj["size"] = size;
            return obj.ToString();
        }
    }
}