using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyServerAPI : ShareObject
    {
        [ShareObjectImport] public Func<UniTask<SimplyServer>> GetMyServer { get; set; }
        [ShareObjectImport] public Func<string, string, uint, uint, UniTask<ShareObject>> SearchServers { get; set; }
    }
}