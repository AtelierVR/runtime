using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Nox.CCK.Mods.Metadata
{
    public interface ModMetadata
    {
        public string GetDataType();
        public string GetId();
        public string[] GetProvides();
        public Version GetVersion();
        public SideFlags GetSide();
        public string[] GetPermissions();
        public bool IsKernel();
        public Entries GetEntryPoints();

        public string GetName();
        public string GetDescription();
        public string GetLicense();
        public string GetIcon(uint size = 0);

        public Contact GetContact();
        public Person[] GetAuthors();
        public Person[] GetContributors();

        public Relation[] GetRelations();
        public Relation[] GetDepends();
        public Relation[] GetBreaks();
        public Relation[] GetConflicts();
        public Relation[] GetRecommends();
        public Relation[] GetSuggests();
        public Reference[] GetReferences();

        public T GetCustom<T>(string key, T defaultValue = default);
        
        public bool HasCustom<T>(string key);
        public Dictionary<string, object> GetCustoms();

        public JObject ToObject();
        public string ToJson();

        public bool Match(ModMetadata req);
        public bool Match(string id);
        public bool Match(Relation relation);
        
        
#if UNITY_EDITOR
        public void SetCustom<T>(string key, T value);
        public bool Save(string path);
#endif
    }
}