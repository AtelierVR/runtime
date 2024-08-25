using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyUserMe : SimplyUser
    {
        [ShareObjectImport, ShareObjectExport] public string email;
        [ShareObjectImport, ShareObjectExport] public uint createdAt;
        [ShareObjectImport, ShareObjectExport] public string home;

        [ShareObjectImport, ShareObjectExport] public Func<string, string, bool> SharedMatchRef;
        [ShareObjectImport, ShareObjectExport] public Func<UniTask<ShareObject>> SharedGetHome;

        public bool MatchRef(string reference, string default_server)
            => SharedMatchRef(reference, default_server);
            
        public async UniTask<SimplyWorld> GetHome()
            => (await SharedGetHome())?.Convert<SimplyWorld>();

        public override string ToString() => $"{GetType().Name}[username={username}, display={display}]";
    }
}