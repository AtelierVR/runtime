using System;
using System.Collections.Generic;
using CCKMetadata = Nox.CCK.Mods.ModMetadata;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Nox.Mods
{
    public class ModMetadata : CCKMetadata
    {
        public static ModMetadata LoadFromPath(string path) => LoadFromJson(JsonFromPath(path));
        public static string RemoveCommentary(string text)
        {
            var tex = System.Text.RegularExpressions.Regex.Replace(text, @"\/\/.*", "");
            text = System.Text.RegularExpressions.Regex.Replace(tex, @"\/\*[\s\S]*?\*\/", "");
            foreach (var line in text.Split('\n'))
            {
                Debug.Log(line);
            }
            return tex;
        }

        public static ModMetadata LoadFromText(string text)
        {
            return LoadFromJson(JsonFromText(text));
        }

        public static JObject JsonFromText(string text) => JObject.Parse(RemoveCommentary(text));
        public static JObject JsonFromPath(string path) => JsonFromText(System.IO.File.ReadAllText(path));

        public static ModMetadata LoadFromJson(JObject json) => new ModMetadata()
        {
            _id = json.TryGetValue("id", out var id) ? id.Value<string>() : null,
            _name = json.TryGetValue("name", out var name) ? name.Value<string>() : null,
            _description = json.TryGetValue("description", out var description) ? description.Value<string>() : null,
            _version = json.TryGetValue("version", out var version) ? new Version(version.Value<string>()) : null,
            _license = json.TryGetValue("license", out var license) ? license.Value<string>() : null,
            _permissions = json.TryGetValue("permissions", out var permissions) ? permissions.ToObject<string[]>() : new string[0],
            _customs = json.TryGetValue("customs", out var customs) ? customs.ToObject<Dictionary<string, object>>() : new Dictionary<string, object>(),
            _platforms = json.TryGetValue("platforms", out var platforms) ? platforms.ToArray().Select(p => Platform.LoadFromJson(p)).ToArray() : new Platform[0],
            _engines = json.TryGetValue("engines", out var engines) ? engines.ToArray().Select(e => Engine.LoadFromJson(e)).ToArray() : new Engine[0],
            _references = json.TryGetValue("references", out var references) ? references.ToArray().Select(r => Reference.LoadFromJson(r)).ToArray() : new Reference[0],
            _relations = json.TryGetValue("relations", out var relations) ? relations.ToArray().Select(r => Relation.LoadFromJson(r)).ToArray() : new Relation[0],
            _authors = json.TryGetValue("authors", out var authors) ? authors.ToArray().Select(a => Person.LoadFromJson(a)).ToArray() : new Person[0],
            _contributors = json.TryGetValue("contributors", out var contributors) ? contributors.ToArray().Select(c => Person.LoadFromJson(c)).ToArray() : new Person[0],
            _contact = json.TryGetValue("contact", out var contact) ? Contact.LoadFromJson(contact) : null,
            _required = json.TryGetValue("required", out var required) ? Required.LoadFromJson(required) : null,
            _side = json.TryGetValue("side", out var side) ? (CCK.Mods.Metadata.SideFlags)Enum.Parse(typeof(CCK.Mods.Metadata.SideFlags), json.Value<string>("side")) : CCK.Mods.Metadata.SideFlags.None,
            _provides = json.TryGetValue("provides", out var provides) ? provides.ToObject<string[]>() : new string[0],
            _entryPoints = json.TryGetValue("entrypoints", out var entrypoints) ? Entries.LoadFromJson(entrypoints) : null
        };

        public string GetDataType() => "mod";
        public string GetId() => _id;
        public string[] GetProvides() => _provides;
        public Version GetVersion() => _version;
        public CCK.Mods.Metadata.SideFlags GetSide() => _side;
        public string[] GetPermissions() => _permissions;
        public CCK.Mods.Metadata.Required GetRequired() => _required;
        public string GetName() => _name;
        public string GetDescription() => _description;
        public string GetLicense() => _license;
        public string GetIcon(uint size = 0)
        {
            return null;
        }

        public CCK.Mods.Metadata.Contact GetContact() => _contact;
        public CCK.Mods.Metadata.Person[] GetAuthors() => _authors;
        public CCK.Mods.Metadata.Person[] GetContributors() => _contributors;
        public CCK.Mods.Metadata.Relation[] GetRelations() => _relations;
        public CCK.Mods.Metadata.Relation[] GetDepends() => _relations
            .Where(r => r.GetRelationType() == CCK.Mods.Metadata.RelationType.Depends).ToArray();
        public CCK.Mods.Metadata.Relation[] GetBreaks() => _relations
            .Where(r => r.GetRelationType() == CCK.Mods.Metadata.RelationType.Breaks).ToArray();
        public CCK.Mods.Metadata.Relation[] GetConflicts() => _relations
            .Where(r => r.GetRelationType() == CCK.Mods.Metadata.RelationType.Conflicts).ToArray();
        public CCK.Mods.Metadata.Relation[] GetRecommends() => _relations
            .Where(r => r.GetRelationType() == CCK.Mods.Metadata.RelationType.Recommends).ToArray();
        public CCK.Mods.Metadata.Relation[] GetSuggests() => _relations
            .Where(r => r.GetRelationType() == CCK.Mods.Metadata.RelationType.Suggests).ToArray();
        public CCK.Mods.Metadata.Reference[] GetReferences() => _references;
        public CCK.Mods.Metadata.Engine[] GetEngines() => _engines;
        public CCK.Mods.Metadata.Platform[] GetPlatforms() => _platforms;
        public T GetCustom<T>(string key) where T : class => HasCustom<T>(key) ? _customs[key] as T : null;
        public bool HasCustom<T>(string key) where T : class => _customs.ContainsKey(key);
        public Dictionary<string, object> GetCustoms() => _customs;
        public CCK.Mods.Metadata.Entries GetEntryPoints() => _entryPoints;

        private string _id;
        private string _name;
        private string _description;
        private Version _version;
        private string _license;
        private string[] _permissions;
        private Dictionary<string, object> _customs;
        private CCK.Mods.Metadata.Platform[] _platforms;
        private CCK.Mods.Metadata.Engine[] _engines;
        private CCK.Mods.Metadata.Reference[] _references;
        private CCK.Mods.Metadata.Relation[] _relations;
        private CCK.Mods.Metadata.Person[] _authors;
        private CCK.Mods.Metadata.Person[] _contributors;
        private CCK.Mods.Metadata.Contact _contact;
        private CCK.Mods.Metadata.Required _required;
        private CCK.Mods.Metadata.SideFlags _side;
        private string[] _provides;
        private CCK.Mods.Metadata.Entries _entryPoints;
    }
}