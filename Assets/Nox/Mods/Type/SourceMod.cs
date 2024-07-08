using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Nox.CCK;
using UnityEngine.SceneManagement;

namespace Nox.Mods.Type
{
    public class SourceMod : ModType
    {
        private ModMetadata _metadata;
        private Dictionary<string, Assembly> _assemblies = new();

        public SourceMod(string path) : base(path) { }

        public override ModMetadata GetMetadata() => _metadata ??= ReadMetadata();
        public ModMetadata ReadMetadata()
        {
            if (!Directory.Exists(_path)) return null;
            var file = Directory.GetFiles(_path, "nox.mod.json*", SearchOption.AllDirectories).FirstOrDefault();
            if (file == null) return null;
            return ModMetadata.LoadFromJson(JObject.Parse(File.ReadAllText(file)));
        }

        public override bool IsValid() => Directory.Exists(_path) && GetMetadata() != null;
        public override bool Repart() => IsValid();

        public static string[] GetAllMods(string basepath)
        {
            return Directory.GetDirectories(basepath, "*", SearchOption.TopDirectoryOnly)
                .Where(d => Directory.GetFiles(d, "nox.mod.json*", SearchOption.TopDirectoryOnly).Length > 0)
                .ToArray();
        }

        public override Assembly LoadAssembly(string ns)
        {
            var mt = GetMetadata();
            if (mt == null) return null;
            CCK.Mods.Metadata.Reference rf = null;
            foreach (var r in mt.GetReferences().Reverse())
                if (r.GetNamespace() == ns
                    && r.GetPlatform() == PlatfromExtensions.CurrentPlatform
                    && r.GetEngine().GetVersion().Matches(EngineExtensions.CurrentVersion))
                {
                    rf = r;
                    break;
                }
            if (rf == null) return null;
            var p = Path.Combine(_path, "libs/" + rf.GetFile());
            // check if path is in Application.dataPath, if true, load from Library/ScriptAssemblie
            if (!File.Exists(p)) return null;
            var asm = Assembly.LoadFrom(p);
            _assemblies[ns] = asm;
            return asm;
        }

        public override Assembly GetAssembly(string ns)
        {
            if (_assemblies.ContainsKey(ns)) return _assemblies[ns];
            return LoadAssembly(ns);
        }

        public override void Destroy()
        {
        }

        public override string[] GetAllAssetNames()
        {
            return Directory.GetFiles(Path.Combine(_path, "resources"), "*.*", SearchOption.AllDirectories);
        }

        public override T GetAsset<T>(string ns, string name)
        {
            return null;
        }

        public override Scene LoadScene(string ns, string name, LoadSceneMode mode = LoadSceneMode.Single)
        {
            return default;
        }
    }
}