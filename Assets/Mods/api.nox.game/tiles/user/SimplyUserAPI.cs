using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyUserAPI : ShareObject
    {
        [ShareObjectImport] public Func<UniTask<SimplyUserMe>> GetMyUser { get; set; }
        [ShareObjectImport] public Func<string, string, uint, uint, UniTask<ShareObject>> SearchUsers { get; set; }
    }
}