using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyServer : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string id;
        [ShareObjectImport, ShareObjectExport] public string title;
        [ShareObjectImport, ShareObjectExport] public string description;
        [ShareObjectImport, ShareObjectExport] public string address;
        [ShareObjectImport, ShareObjectExport] public string version;
        [ShareObjectImport, ShareObjectExport] public long ready_at;
        [ShareObjectImport, ShareObjectExport] public string icon;
        [ShareObjectImport, ShareObjectExport] public string public_key;
        [ShareObjectImport, ShareObjectExport] public string[] features;

        [ShareObjectImport, ShareObjectExport] public Func<UniTask<ShareObject>> SharedGetToken;
        [ShareObjectImport, ShareObjectExport] public Func<UniTask<ShareObject>> SharedGetOrConnect;

        public async UniTask<SimplyAuthToken> GetToken() 
            => (await SharedGetToken())?.Convert<SimplyAuthToken>();
            
        public async UniTask<SimplyWebSocket> GetOrConnect()
            => (await SharedGetOrConnect())?.Convert<SimplyWebSocket>();
    }
}