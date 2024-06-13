#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.Mods.Type
{
    public class InternalMod : ModType
    {
        private ModMetadata _metadata;
        private Dictionary<string, Assembly> _assemblies = new();

        public InternalMod(string path) : base(path) { }

        public override ModMetadata GetMetadata() => _metadata ??= ReadMetadata();

        public override bool IsValid() => GetMetadata() != null;

        public override bool Repart() => IsValid();

        public override string[] GetNamespaces() => new string[] { GetMetadata().GetId() };

        public ModMetadata ReadMetadata()
        {
            if (!Directory.Exists(_path)) return null;
            var file = Directory.GetFiles(_path, "nox.mod.json*", SearchOption.AllDirectories).FirstOrDefault();
            if (file == null) return null;
            return ModMetadata.LoadFromJson(JObject.Parse(File.ReadAllText(file)));
        }

        public override Assembly LoadAssembly(string ns)
        {
            if (ns != GetMetadata().GetId()) return null;
            var p = Path.Combine(Application.dataPath, "..", "Library/ScriptAssemblies/" + GetMetadata().GetId() + ".dll");
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
            return Directory.GetFiles(Path.Combine(_path, "Resources"), "*", SearchOption.AllDirectories);
        }

        public override T GetAsset<T>(string ns, string name)
        {
            return AssetDatabase.LoadAssetAtPath<T>(Path.Combine(_path, "Resources", ns, name));
        }

        public override Scene LoadScene(string ns, string name)
        {
            var path = Path.Combine(_path, "Resources", ns, "worlds", name, name + ".unity");
            if (!File.Exists(path)) return default;
            SceneManager.LoadScene(path, LoadSceneMode.Additive);
            return SceneManager.GetSceneByPath(path);
        }
    }
}

#endif
