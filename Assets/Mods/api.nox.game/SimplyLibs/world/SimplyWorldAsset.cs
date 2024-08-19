using System;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyWorldAsset : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public uint id;
        [ShareObjectImport, ShareObjectExport] public uint version;
        [ShareObjectImport, ShareObjectExport] public string engine;
        [ShareObjectImport, ShareObjectExport] public string platform;
        [ShareObjectImport, ShareObjectExport] public bool is_empty;
        [ShareObjectImport, ShareObjectExport] public string url;
        [ShareObjectImport, ShareObjectExport] public string hash;
        [ShareObjectImport, ShareObjectExport] public uint size;
        [ShareObjectImport, ShareObjectExport] public string server;

        [ShareObjectImport, ShareObjectExport] public Func<bool> SharedIsEmpty;
        [ShareObjectImport, ShareObjectExport] public Func<string> SharedGetSID;
        public bool IsEmpty() => SharedIsEmpty();
        public string GetSID() => SharedGetSID();
    }
}