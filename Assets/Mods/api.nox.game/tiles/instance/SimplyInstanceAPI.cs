using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyInstanceAPI : ShareObject
    {
        [ShareObjectImport] public Func<string, string, uint, uint, UniTask<ShareObject>> SearchInstances { get; set; }
    }
}