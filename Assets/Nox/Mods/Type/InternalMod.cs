
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

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
            Debug.Log("InternalMod.GetAssembly: " + ns);
            if (_assemblies.ContainsKey(ns)) return _assemblies[ns];
            return LoadAssembly(ns);
        }

        public override void Destroy()
        {
        }
    }
}

