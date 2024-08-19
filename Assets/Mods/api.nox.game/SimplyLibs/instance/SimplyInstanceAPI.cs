using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyInstanceAPI : ShareObject
    {

        [ShareObjectExport, ShareObjectImport] public Func<string, string, uint, uint, UniTask<ShareObject>> SharedSearchInstances;
        [ShareObjectExport, ShareObjectImport] public Func<string, uint, UniTask<ShareObject>> SharedGetInstance;
        [ShareObjectExport, ShareObjectImport] public Func<ShareObject, UniTask<ShareObject>> SharedCreateInstance;
        public async UniTask<SimplyInstanceSearch> SearchInstances(string server, string query, uint offset = 0, uint limit = 10)
            => (await SharedSearchInstances(server, query, offset, limit)).Convert<SimplyInstanceSearch>();
        public async UniTask<SimplyInstance> GetInstance(string server, uint instanceId)
            => (await SharedGetInstance(server, instanceId))?.Convert<SimplyInstance>();
        public async UniTask<SimplyInstance> CreateInstance(SimplyCreateInstanceData data)
            => (await SharedCreateInstance(data))?.Convert<SimplyInstance>();
    }
}