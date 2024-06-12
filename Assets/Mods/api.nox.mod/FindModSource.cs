#if UNITY_EDITOR
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace api.nox.mod
{
    internal class FindModSource
    {
        internal static string FindSource(string id)
        {
            var asmdef = Directory.GetFiles(Application.dataPath, id + ".asmdef", SearchOption.AllDirectories).FirstOrDefault();
            if (asmdef == null)
            {
                Debug.LogError($"ASMDEF not found for {id}");
                return null;
            }
            var asmobj = JObject.Parse(File.ReadAllText(asmdef));
            if (asmobj.TryGetValue("name", out var name) && name.Value<string>() != id)
            {
                Debug.LogError($"ASMDEF name not match for {id}");
                return null;
            }
            var noxmod = Directory.GetFiles(Path.GetDirectoryName(asmdef), "nox.mod.json*", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (noxmod == null)
            {
                Debug.LogError($"NoxMod not found for {id}");
                return null;
            }
            var noxobj = ModEditorMod._api.LibsAPI.LoadMetadata(noxmod);
            if (noxobj == null)
            {
                Debug.LogError($"NoxMod not valid for {id}");
                return null;
            }
            return Path.GetDirectoryName(asmdef);
        }
    }
}
#endif