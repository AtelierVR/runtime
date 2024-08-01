using System;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyWorldAsset : ShareObject
    {
        [ShareObjectImport] public uint id;
        [ShareObjectImport] public uint version;
        [ShareObjectImport] public string engine;
        [ShareObjectImport] public string platform;
        [ShareObjectImport] public bool is_empty;
        [ShareObjectImport] public string url;
        [ShareObjectImport] public string hash;
        [ShareObjectImport] public uint size;
        [ShareObjectImport] public string server;

        [ShareObjectExport] public Func<bool> SharedIsEmpty;
        public bool IsEmpty() => SharedIsEmpty();
        [ShareObjectExport] public Func<string> SharedGetSID;
        public string GetSID() => SharedGetSID();
    }
}