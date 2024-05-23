using System;
using System.Collections.Generic;

namespace Nox.CCK.Mods
{
    public interface ModMetadata
    {
        public string GetDataType();
        public string GetId();
        public string[] GetProvides();
        public Version GetVersion();
        public Metadata.SideFlags GetSide();
        public string[] GetPermissions();
        public string[] GetRequired();
        public Metadata.Entries GetEntryPoints();

        public string GetName();
        public string GetDescription();
        public string GetLicense();
        public string GetIcon(uint size = 0);


        public Metadata.Contact GetContact();
        public Metadata.Person[] GetAuthors();
        public Metadata.Person[] GetContributors();


        public Metadata.Relation[] GetRelations();
        public Metadata.Relation[] GetDepends();
        public Metadata.Relation[] GetBreaks();
        public Metadata.Relation[] GetConflicts();
        public Metadata.Relation[] GetRecommends();
        public Metadata.Relation[] GetSuggests();

        public Metadata.Reference[] GetReferences();
        public Metadata.Engine[] GetEngines();
        public string[] GetPlatforms();

        public T GetCustom<T>(string key) where T : class;
        public bool HasCustom<T>(string key) where T : class;
        public Dictionary<string, object> GetCustoms();
    }
}