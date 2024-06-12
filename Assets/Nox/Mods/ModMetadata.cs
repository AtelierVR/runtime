using System;
using System.Collections.Generic;
using CCKMetadata = Nox.CCK.Mods.ModMetadata;
using Newtonsoft.Json.Linq;
using System.Linq;
using Nox.CCK.Mods.Metadata;
using Nox.CCK;
using System.IO;

namespace Nox.Mods
{
    public class ModMetadata : CCKMetadata
    {
        public static ModMetadata LoadFromPath(string path) => LoadFromJson(JsonFromPath(path));
        public static string RemoveCommentary(string text)
        {
            var tex = System.Text.RegularExpressions.Regex
                .Replace(text, @"\/\/.*", m => m.Value.Contains("\"") ? m.Value : "");
            text = System.Text.RegularExpressions.Regex.Replace(tex, @"\/\*[\s\S]*?\*\/", "");
            return tex;
        }

        public static ModMetadata LoadFromText(string text) => LoadFromJson(JsonFromText(text));

        public static JObject JsonFromText(string text) => JObject.Parse(RemoveCommentary(text));
        public static JObject JsonFromPath(string path) => JsonFromText(System.IO.File.ReadAllText(path));

        public static ModMetadata LoadFromJson(JObject json)
        {
            try
            {
                var obj = new ModMetadata()
                {
                    _id = json.TryGetValue("id", out var id) ? id.Value<string>() : null,
                    _name = json.TryGetValue("name", out var name) ? name.Value<string>() : null,
                    _description = json.TryGetValue("description", out var description) ? description.Value<string>() : null,
                    _version = json.TryGetValue("version", out var version) ? new Version(version.Value<string>()) : null,
                    _license = json.TryGetValue("license", out var license) ? license.Value<string>() : null,
                    _permissions = json.TryGetValue("permissions", out var permissions) ? permissions.ToObject<string[]>() : new string[0],
                    _references = json.TryGetValue("references", out var references) ? references.ToArray().Select(r => Reference.LoadFromJson(r.ToObject<JObject>())).ToArray() : new Reference[0],
                    _relations = json.TryGetValue("relations", out var relations) ? ObjectToRelations(relations) : new Relation[0],
                    _authors = json.TryGetValue("authors", out var authors) ? authors.ToArray().Select(a => Person.LoadFromJson(a)).ToArray() : new Person[0],
                    _contributors = json.TryGetValue("contributors", out var contributors) ? contributors.ToArray().Select(c => Person.LoadFromJson(c)).ToArray() : new Person[0],
                    _contact = json.TryGetValue("contact", out var contact) ? Contact.LoadFromJson(contact) : null,
                    _side = json.TryGetValue("side", out var side) ? SideExtensions.GetSideTypeFromNames(side.ToObject<string[]>()) : CCK.Mods.Metadata.SideFlags.None,
                    _provides = json.TryGetValue("provides", out var provides) ? provides.ToObject<string[]>() : new string[0],
                    _entryPoints = json.TryGetValue("entrypoints", out var entrypoints) ? Entries.LoadFromJson(entrypoints.ToObject<JObject>()) : new Entries(),
                    _customs = new Dictionary<string, object>()
                };
                foreach (var (key, value) in json)
                    if (!ignoreKeys.Contains(key))
                        obj._customs[key] = value;
                return obj;
            }
            catch (Exception e)
            {
                Debug.LogWarning(json.ToString());
                Debug.LogWarning(e);
                return null;
            }
        }

        private static string[] ignoreKeys = new string[] { "id", "name", "description", "version", "license", "permissions", "customs", "platforms", "engines", "references", "relations", "authors", "contributors", "contact", "required", "side", "provides", "entrypoints" };

        internal static CCK.Mods.Metadata.Relation[] ObjectToRelations(JToken objectRelations)
        {
            if (objectRelations.Type == JTokenType.Array)
            {
                var relations = new List<CCK.Mods.Metadata.Relation>();
                foreach (var value in objectRelations)
                    if (value.Type == JTokenType.String)
                        relations.Add(Relation.LoadFromData(value.Value<string>(), ">=0.0.0"));
                    else if (value.Type == JTokenType.Object)
                    {
                        var obj = value.ToObject<JObject>();
                        relations.Add(Relation.LoadFromJson(obj["id"].Value<string>(), value.ToObject<JObject>()));
                    }
                return relations.ToArray();
            }
            else if (objectRelations.Type == JTokenType.Object)
            {
                var relations = new List<CCK.Mods.Metadata.Relation>();
                foreach (var (key, value) in objectRelations.ToObject<JObject>())
                    if (value.Type == JTokenType.String)
                        relations.Add(Relation.LoadFromData(key, value.Value<string>()));
                    else if (value.Type == JTokenType.Object)
                        relations.Add(Relation.LoadFromJson(key, value.ToObject<JObject>()));
                return relations.ToArray();
            }
            return new CCK.Mods.Metadata.Relation[0];
        }

        public string GetDataType() => "mod";
        public string GetId() => _id;
        public string[] GetProvides() => _provides;
        public Version GetVersion() => _version;
        public SideFlags GetSide() => _side;
        public string[] GetPermissions() => _permissions;
        public string GetName() => _name;
        public string GetDescription() => _description;
        public string GetLicense() => _license;
        public string GetIcon(uint size = 0) => null;

        public CCK.Mods.Metadata.Contact GetContact() => _contact;
        public CCK.Mods.Metadata.Person[] GetAuthors() => _authors;
        public CCK.Mods.Metadata.Person[] GetContributors() => _contributors;
        public CCK.Mods.Metadata.Relation[] GetRelations() => _relations;
        public CCK.Mods.Metadata.Relation[] GetDepends() => _relations
            .Where(r => r.GetRelationType() == RelationType.Depends).ToArray();
        public CCK.Mods.Metadata.Relation[] GetBreaks() => _relations
            .Where(r => r.GetRelationType() == RelationType.Breaks).ToArray();
        public CCK.Mods.Metadata.Relation[] GetConflicts() => _relations
            .Where(r => r.GetRelationType() == RelationType.Conflicts).ToArray();
        public CCK.Mods.Metadata.Relation[] GetRecommends() => _relations
            .Where(r => r.GetRelationType() == RelationType.Recommends).ToArray();
        public CCK.Mods.Metadata.Relation[] GetSuggests() => _relations
            .Where(r => r.GetRelationType() == RelationType.Suggests).ToArray();
        public CCK.Mods.Metadata.Reference[] GetReferences() => _references;
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
        private CCK.Mods.Metadata.Reference[] _references;
        private CCK.Mods.Metadata.Relation[] _relations;
        private CCK.Mods.Metadata.Person[] _authors;
        private CCK.Mods.Metadata.Person[] _contributors;
        private CCK.Mods.Metadata.Contact _contact;
        private CCK.Mods.Metadata.SideFlags _side;
        private string[] _provides;
        private CCK.Mods.Metadata.Entries _entryPoints;
        public JObject ToObject()
        {
            var entries = GetEntryPoints();
            var json = new JObject
            {
                ["type"] = GetDataType(),
                ["id"] = GetId(),
                ["provides"] = new JArray(GetProvides()),
                ["name"] = GetName(),
                ["version"] = GetVersion().ToString(),
                ["description"] = GetDescription(),
                ["license"] = GetLicense(),
                ["icon"] = GetIcon(),
                ["contact"] = new JObject(
                    GetContact().GetAll().Select(c => new JProperty(c.Key, c.Value))
                ),
                ["authors"] = new JArray(GetAuthors().Select(a => new JObject
                {
                    ["name"] = a.GetName(),
                    ["email"] = a.GetEmail(),
                    ["website"] = a.GetWebsite()
                })),
                ["contributors"] = new JArray(GetContributors().Select(a => new JObject
                {
                    ["name"] = a.GetName(),
                    ["email"] = a.GetEmail(),
                    ["website"] = a.GetWebsite()
                })),
                ["relations"] = new JArray(GetRelations().Select(r => new JObject
                {
                    ["id"] = r.GetId(),
                    ["version"] = r.GetVersion().ToString(),
                    ["type"] = RelationExtensions.GetRelationTypeFromEnum(r.GetRelationType())
                })),
                ["references"] = new JArray(GetReferences().Select(r => new JObject
                {
                    ["name"] = r.GetNamespace(),
                    ["file"] = r.GetFile(),
                    ["engine"] = new JObject
                    {
                        ["name"] = EngineExtensions.GetEngineName(r.GetEngine().GetName()),
                        ["version"] = r.GetEngine().GetVersion().ToString()
                    },
                    ["platform"] = PlatfromExtensions.GetPlatformName(r.GetPlatform())
                })),
                ["entrypoints"] = new JObject
                {
                    ["main"] = new JArray(entries.GetMain()),
                    ["client"] = new JArray(entries.GetClient()),
                    ["editor"] = new JArray(entries.GetEditor()),
                    ["instance"] = new JArray(entries.GetInstance())
                },
                ["side"] = new JArray(SideExtensions.GetSideTypeFromEnum(GetSide())),
                ["permissions"] = new JArray(GetPermissions()),
            };
            foreach (var custom in GetCustoms())
                json[custom.Key] = JToken.FromObject(custom.Value);
            return json;
        }

        public string ToJson() => ToObject().ToString();

        public bool Match(CCKMetadata req) => GetId() == req.GetId() || GetProvides().Contains(req.GetId());
        public bool Match(string id) => GetId() == id || GetProvides().Contains(id);
    }
}