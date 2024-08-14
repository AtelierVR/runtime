using System;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyUser : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public uint id;
        [ShareObjectImport, ShareObjectExport] public string username;
        [ShareObjectImport, ShareObjectExport] public string display;
        [ShareObjectImport, ShareObjectExport] public string[] tags;
        [ShareObjectImport, ShareObjectExport] public string server;
        [ShareObjectImport, ShareObjectExport] public float rank;
        [ShareObjectImport, ShareObjectExport] public string[] links;
        [ShareObjectImport, ShareObjectExport] public string banner;
        [ShareObjectImport, ShareObjectExport] public string thumbnail;
        
        [ShareObjectImport, ShareObjectExport] public Func<string, string, bool> SharedMarch;
        public bool Match(string reference, string default_server)
            => SharedMarch(reference, default_server);

        public override string ToString() => $"{GetType().Name}[username={username}, display={display}]";
    }
}