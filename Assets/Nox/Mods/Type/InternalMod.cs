#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Nox.CCK;
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
            => Resources.Load<T>(Path.Combine(ns, name));

        public override Scene LoadScene(string ns, string name, LoadSceneMode mode = LoadSceneMode.Single)
        {
            var path = Path.Combine(ns, "worlds", name, name + ".unity");
            var validpath = "";
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var sc = SceneUtility.GetScenePathByBuildIndex(i);
                if (sc.Replace('\\', '/').EndsWith(path.Replace('\\', '/')))
                    validpath = sc;
            }
            if (string.IsNullOrEmpty(validpath)) return default;
            var id = SceneUtility.GetBuildIndexByScenePath(validpath);
            if (id == -1) return default;
            var scene = SceneManager.GetSceneByBuildIndex(id);
            if (!scene.isLoaded) SceneManager.LoadScene(id, mode);
            return scene;
        }
    }
}

#endif
