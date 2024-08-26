using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyAuthAPI : ShareObject
    {
        [ShareObjectImport] public Func<string, UniTask<ShareObject>> SharedGetToken;
        public async UniTask<SimplyAuthToken> GetToken(string token)
            => (await SharedGetToken(token))?.Convert<SimplyAuthToken>();
    }
}