using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Nox.CCK;

namespace Nox.Mods.Type
{
    public class ArchiveMod : ModType
    {
        private ZipArchive _archive;
        private ModMetadata _metadata;
        private Dictionary<string, Assembly> _assemblies = new();

        public ArchiveMod(string path) : base(path)
        {
            _archive = null;
        }

        public bool Open()
        {
            try
            {
                _archive = ZipFile.OpenRead(_path);
                return true;
            }
            catch { return false; }
        }

        private ModMetadata ReadMetadata()
        {
            if (_archive == null) return null;
            var entry = _archive.GetEntry("nox.mod.json") ?? _archive.GetEntry("nox.mod.jsonc");
            if (entry == null) return null;
            using var stream = entry.Open();
            string jsonstring = new StreamReader(stream).ReadToEnd();
            return ModMetadata.LoadFromJson(JObject.Parse(jsonstring));
        }

        public override ModMetadata GetMetadata() => _metadata ??= ReadMetadata();
        public override bool IsValid() => _archive != null && GetMetadata() != null;
        public override bool Repart() => Open() && IsValid();

        public static string[] GetAllMods(string basepath)
        {
            return Directory.GetFiles(basepath, "*.nmod", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(basepath, "*.zip", SearchOption.AllDirectories))
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
                    && r.GetEngine().GetName() == EngineExtensions.CurrentEngine
                    && r.GetEngine().GetVersion().Matches(EngineExtensions.CurrentVersion))
                {
                    rf = r;
                    break;
                }
            if (rf == null) return null;
            var entry = _archive.GetEntry("libs/" + rf.GetFile());
            if (entry == null) return null;
            using var stream = entry.Open();
            var bytes = new BinaryReader(stream).ReadBytes((int)entry.Length);
            var assembly = Assembly.Load(bytes);
            _assemblies[ns] = assembly;
            return assembly;
        }

        public override Assembly GetAssembly(string ns)
        {
            if (_assemblies.ContainsKey(ns)) return _assemblies[ns];
            return LoadAssembly(ns);
        }

        public override void Destroy()
        {
            _archive?.Dispose();
        }
    }
}