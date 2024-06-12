#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Metadata;
using UnityEngine;
using MEngine = Nox.CCK.Mods.Metadata.Engine;

namespace api.nox.mod
{
    internal class GMetadata : ModMetadata
    {
        private ModMetadata _base;
        private Reference _ref;
        internal GMetadata(ModMetadata metadata) => _base = metadata;
        public Person[] GetAuthors() => _base.GetAuthors();
        public Relation[] GetBreaks() => _base.GetBreaks();
        public Relation[] GetConflicts() => _base.GetConflicts();
        public Contact GetContact() => _base.GetContact();
        public Person[] GetContributors() => _base.GetContributors();
        public T GetCustom<T>(string key) where T : class => _base.GetCustom<T>(key);
        public Dictionary<string, object> GetCustoms() => _base.GetCustoms();
        public string GetDataType() => _base.GetDataType();
        public Relation[] GetDepends() => _base.GetDepends();
        public string GetDescription() => _base.GetDescription();
        public Entries GetEntryPoints() => _base.GetEntryPoints();
        public string GetIcon(uint size = 0) => _base.GetIcon(size);
        public string GetId() => _base.GetId();
        public string GetLicense() => _base.GetLicense();
        public string GetName() => _base.GetName();
        public string[] GetPermissions() => _base.GetPermissions();
        public string[] GetProvides() => _base.GetProvides();
        public Relation[] GetRecommends() => _base.GetRecommends();
        public Reference[] GetReferences()
        {
            var refs = _base.GetReferences().ToList();
            if (_ref != null) refs.Add(_ref);
            return refs.ToArray();
        }
        public bool SetNewReference(Reference reference)
        {
            if (_ref == null)
            {
                _ref = reference;
                return true;
            }
            return false;
        }
        public Relation[] GetRelations() => _base.GetRelations();
        public SideFlags GetSide() => _base.GetSide();
        public Relation[] GetSuggests() => _base.GetSuggests();
        public Version GetVersion() => _base.GetVersion();
        public bool HasCustom<T>(string key) where T : class => _base.HasCustom<T>(key);
        public JObject ToObject()
        {
            var obj = _base.ToObject();
            if (_ref != null)
                obj["references"] = new JArray(GetReferences().Select(r => new JObject
                {
                    ["name"] = r.GetNamespace(),
                    ["file"] = r.GetFile(),
                    ["engine"] = new JObject
                    {
                        ["name"] = EngineExtensions.GetEngineName(r.GetEngine().GetName()),
                        ["version"] = r.GetEngine().GetVersion().ToString()
                    },
                    ["platform"] = PlatfromExtensions.GetPlatformName(r.GetPlatform())
                }));
            return obj;
        }
        public string ToJson() => ToObject().ToString();

        public static string FindMetadata(string id)
        {
            var source = FindModSource.FindSource(id);
            if (source == null) return null;
            var file = Directory.GetFiles(source, "nox.mod.json*", SearchOption.TopDirectoryOnly).FirstOrDefault();
            return file;
        }

        public static GMetadata LoadFromPath(string path) => null;
        public static GMetadata LoadFromJson(string json) => null;
        public bool Save(string path)
        {
            var json = ToJson();
            if (json == null) return false;
            File.WriteAllText(path, json);
            return true;
        }
    }

    internal class GEngine : MEngine
    {
        public Nox.CCK.Engine GetName() => _name;
        public VersionMatching GetVersion() => _version;
        internal GEngine(Nox.CCK.Engine name, string version)
        {
            _name = name;
            _version = new VersionMatching(version);
        }
        private Nox.CCK.Engine _name;
        private VersionMatching _version;
    }

    internal class GReference : Reference
    {
        internal GReference(string name, string file, MEngine engine, Platfrom platform)
        {
            _name = name;
            _file = file;
            _engine = engine;
            _platform = platform;
        }
        public string GetFile() => _file;
        public string GetNamespace() => _name;
        public Platfrom GetPlatform() => _platform;
        MEngine Reference.GetEngine() => _engine;
        private string _name;
        private string _file;
        private MEngine _engine;
        private Platfrom _platform;
    }
}
#endif