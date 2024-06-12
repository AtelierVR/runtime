using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Nox.Mods;
using UnityEngine;

namespace Nox.Editor.Manage
{
    public class SourceModManager
    {
        public static string[] FindAllSource()
        {
            var files = Directory.GetFiles(Application.dataPath, "*.asmdef", SearchOption.AllDirectories);
            return files.Select(file =>
            {
                var asmobj = JObject.Parse(File.ReadAllText(file));
                if (asmobj.TryGetValue("name", out var name))
                    return name.Value<string>();
                return null;
            }).Where(name => !string.IsNullOrEmpty(name) && FindSource(name) != null).ToArray();
        }

        public static string FindSource(string id)
        {
            var asmdef = Directory.GetFiles(Application.dataPath, id + ".asmdef", SearchOption.AllDirectories).FirstOrDefault();
            if (asmdef == null) return null;
            var asmobj = JObject.Parse(File.ReadAllText(asmdef));
            if (asmobj.TryGetValue("name", out var name) && name.Value<string>() != id) return null;
            var noxmod = Directory.GetFiles(Path.GetDirectoryName(asmdef), "nox.mod.json*", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (noxmod == null) return null;
            var noxobj = ModMetadata.LoadFromPath(noxmod);
            if (noxobj == null) return null;
            return Path.GetDirectoryName(asmdef);
        }

        public static ModMetadata FindMetadata(string id)
        {
            var source = FindSource(id);
            if (source == null) return null;
            var file = Directory.GetFiles(source, "nox.mod.json*", SearchOption.TopDirectoryOnly).FirstOrDefault();
            return ModMetadata.LoadFromPath(file);
        }
    }
}