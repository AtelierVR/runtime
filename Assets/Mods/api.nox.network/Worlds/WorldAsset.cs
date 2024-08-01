using System;
using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class WorldAsset : ShareObject
    {
        internal NetWorld netWorld;
        [ShareObjectExport] public uint id;
        [ShareObjectExport] public uint version;
        [ShareObjectExport] public string engine;
        [ShareObjectExport] public string platform;
        [ShareObjectExport] public bool is_empty;
        [ShareObjectExport] public string url;
        [ShareObjectExport] public string hash;
        [ShareObjectExport] public uint size;
        [ShareObjectExport] public string server;

        public bool IsEmpty() => is_empty || string.IsNullOrEmpty(url) || string.IsNullOrEmpty(hash) || size == 0;
        public string GetSID() => $"{id};v={version};e={engine};p={platform}@{server}";

        [ShareObjectExport] public Func<bool> SharedIsEmpty;
        [ShareObjectExport] public Func<string> SharedGetSID;

        public void BeforeExport()
        {
            SharedIsEmpty = () => IsEmpty();
            SharedGetSID = () => GetSID();
        }

        public void AfterExport()
        {
            SharedIsEmpty = null;
            SharedGetSID = null;
        }
    }
}