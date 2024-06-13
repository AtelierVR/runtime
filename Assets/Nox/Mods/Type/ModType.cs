using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nox.CCK;
using UnityEngine.SceneManagement;

namespace Nox.Mods.Type
{
    public abstract class ModType
    {
        protected string _path;
        public ModType(string path)
        {
            _path = path;
        }
        public abstract bool IsValid();
        public abstract ModMetadata GetMetadata();
        public abstract bool Repart();
        public abstract Assembly LoadAssembly(string ns);
        public abstract Assembly GetAssembly(string ns);
        public virtual string[] GetNamespaces()
        {
            var mt = GetMetadata();
            if (mt == null) return new string[0];
            var namespaces = new List<string>();
            foreach (var r in mt.GetReferences())
                if (r.GetPlatform() == PlatfromExtensions.CurrentPlatform
                    && r.GetEngine().GetName() == EngineExtensions.CurrentEngine
                    && r.GetEngine().GetVersion().Matches(EngineExtensions.CurrentVersion))
                    namespaces.Add(r.GetNamespace());
            return namespaces.Distinct().ToArray();
        }
        public abstract void Destroy();
        public abstract string[] GetAllAssetNames();
        public abstract T GetAsset<T>(string ns, string name) where T : UnityEngine.Object;
        public abstract Scene LoadScene(string ns, string name);
    }
}